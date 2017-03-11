using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureNSGRulesValidator
{
    public class NSGRule
    {
        public string Name { get; set; }

        public string Protocol { get; set; }

        public string SourcePortRange { get; set; }

        public string DestinationPortRange { get; set; }

        public string SourceAddressPrefix { get; set; }

        public string DestinationAddressPerfix { get; set; }

        public string AccessType { get; set; }

        public string Priority { get; set; }

        public string Direction { get; set; }
    }
}
