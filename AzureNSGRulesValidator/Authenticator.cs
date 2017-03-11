using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace AzureNSGRulesValidator
{
    public class Authenticator
    {
        public static AuthenticationResult Authenticate()
        {
            InitializeServicePointManager();

            var tokenCache = new TokenCache();
            var authResult = GetAuthorizationResult(tokenCache: tokenCache).Result;

            return authResult;
        }
        
        private static void InitializeServicePointManager()
        {
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 48;
            ServicePointManager.MaxServicePointIdleTime = 90000;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
        }
        
        private static string AppendApiVersion(string url, string apiVersion)
        {
            if (!string.IsNullOrWhiteSpace(apiVersion))
            {
                return string.Format("{0}?api-version={1}", url, apiVersion);
            }

            return url;
        }
        
        private static async Task<AuthenticationResult> GetAuthorizationResult(string tenantId = "common", TokenCache tokenCache = null, string userId = null)
        {
            AuthenticationResult result = null;
            try
            {
                var context = new AuthenticationContext(Constants.AADUrl + tenantId, true, tokenCache);
                if (!string.IsNullOrEmpty(userId))
                {
                    result = await context.AcquireTokenAsync(
                        resource: "https://management.core.windows.net/",
                        clientId: "1950a258-227b-4e31-a9cf-717495945fc2",
                        redirectUri: new Uri("urn:ietf:wg:oauth:2.0:oob"),
                        parameters: new PlatformParameters(PromptBehavior.Auto),
                        userId: new UserIdentifier(userId, UserIdentifierType.UniqueId)).ConfigureAwait(false);
                }
                else
                {
                    result = await context.AcquireTokenAsync(
                        resource: "https://management.core.windows.net/",
                        clientId: "1950a258-227b-4e31-a9cf-717495945fc2",
                        redirectUri: new Uri("urn:ietf:wg:oauth:2.0:oob"),
                        parameters: new PlatformParameters(PromptBehavior.Always)
                        ).ConfigureAwait(false);
                }
            }
            catch (Exception threadEx)
            {
                Console.WriteLine(threadEx.Message);
            }

            return result;
        }
    }
}
