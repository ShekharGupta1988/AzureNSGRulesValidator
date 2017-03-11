using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AzureNSGRulesValidator
{
    class Program
    {
        static void Main(string[] args)
        {
            var token = Authenticator.Authenticate();
            AzureAPIClient _apiClient = new AzureAPIClient(string.Format("{0} {1}", token.AccessTokenType, token.AccessToken));
        }
    }
}
