using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsmrHub.IotCentral
{
    internal class IotCentralOptions
    {
        public bool Enabled { get; set; } = default!;
        public string IdScope { get; set; } = default!;
        public string DeviceId { get; set; } = default!;
        public string PrimaryKey { get; set; } = default!;
        public string SecondaryKey { get; set; } = default!;
        public string ProvisioningUri { get; set; } = default!;
    }
}
