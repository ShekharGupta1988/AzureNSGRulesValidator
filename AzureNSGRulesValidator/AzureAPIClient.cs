using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AzureNSGRulesValidator
{
    public class AzureAPIClient : IDisposable
    {
        private string _frontDoorUrl = "https://management.azure.com";
        private string _hostingEnvironmentPath = "/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Web/hostingEnvironments/{2}?api-version=2015-08-01";
        private string _networkApiVersion;
        private HttpClient _httpClient;

        public AzureAPIClient(string authorizationHeader)
        {
            _networkApiVersion = "2017-03-01";

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_frontDoorUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", authorizationHeader);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        
        public async Task<List<NSGRule>> GetNSGRulesForHostingEnvironment(string subscriptionId, string resourceGroup, string hostingEnvironmentName)
        {
            var response = await _httpClient.GetAsync(string.Format(_hostingEnvironmentPath, subscriptionId, resourceGroup, hostingEnvironmentName));
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("Get Hosting Environment failed. Status Code : {0}", response.StatusCode));
            }

            var hostingEnv = await response.Content.ReadAsAsync<JToken>();
            string subNetResourcePath = ParseAssociatedSubnetPathFromResponse(hostingEnv);

            var subnetResponse = await _httpClient.GetAsync(subNetResourcePath);
            if (!subnetResponse.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("Get Subnet Details failed. Status Code : {0}", response.StatusCode));
            }

            var subnet = await subnetResponse.Content.ReadAsAsync<dynamic>();
            string networkSecurityGroupId = ParseNSGIdFromSubnetResponse(subnet);

            var nsgResponse = await _httpClient.GetAsync(networkSecurityGroupId);
            if (!nsgResponse.IsSuccessStatusCode)
            {
                throw new Exception(string.Format("Get NSG Call failed. Status Code : {0}", response.StatusCode));
            }

            var nsg = await nsgResponse.Content.ReadAsAsync<dynamic>();

            return ParseNSGRules(nsg);
        }

        private List<NSGRule> ParseNSGRules(dynamic nsg)
        {
            var nsgRuleList = new List<NSGRule>();

            if(nsg == null || nsg["properties"] == null)
            {
                throw new Exception("NSG Output cannot be empty");
            }

            var securityRules = nsg["properties"]["securityRules"];
            var defaultSecurityRules = nsg["properties"]["defaultSecurityRules"];

            if(securityRules != null)
            {
                foreach (var item in securityRules)
                {
                    nsgRuleList.Add(ParseNSGRule(item));
                }
            }

            if(defaultSecurityRules != null)
            {
                foreach(var item in defaultSecurityRules)
                {
                    nsgRuleList.Add(ParseNSGRule(item));
                }
            }

            return nsgRuleList.OrderBy(p => p.Priority).ToList();
        }

        private NSGRule ParseNSGRule(dynamic item)
        {
            return new NSGRule()
            {
                Name = item["name"].ToString(),
                AccessType = item["properties"]["access"].ToString(),
                DestinationAddressPerfix = item["properties"]["destinationAddressPrefix"].ToString(),
                DestinationPortRange = item["properties"]["destinationPortRange"].ToString(),
                Direction = item["properties"]["direction"].ToString(),
                Priority = item["properties"]["priority"].ToString(),
                Protocol = item["properties"]["protocol"].ToString(),
                SourceAddressPrefix = item["properties"]["sourceAddressPrefix"].ToString(),
                SourcePortRange = item["properties"]["sourcePortRange"].ToString()
            };
        }

        private string ParseNSGIdFromSubnetResponse(JToken subnet)
        {
            if (subnet == null || subnet["properties"] == null || subnet["properties"]["networkSecurityGroup"] == null)
            {
                throw new Exception("No NSG found for subnet");
            }

            string id = subnet["properties"]["networkSecurityGroup"]["id"].ToString();
            return string.Format("{0}?api-version={1}", id, _networkApiVersion);
        }

        private string ParseAssociatedSubnetPathFromResponse(JToken hostingEnv)
        {
            if (hostingEnv == null || hostingEnv["properties"] == null)
            {
                throw new Exception(string.Format("Could not find hosting Environment"));
            }

            var virtualNetwork = hostingEnv["properties"]["virtualNetwork"];

            if (virtualNetwork == null)
            {
                throw new Exception("Could not find any virtual network.");
            }

            string id = virtualNetwork["id"].ToString();
            string subnet = virtualNetwork["subnet"].ToString();

            return string.Format("{0}/subnets/{1}?api-version={2}", id, subnet, _networkApiVersion);
        }

        public void Dispose()
        {
            if(_httpClient != null)
            {
                _httpClient.Dispose();
            }
        }
    }
}
