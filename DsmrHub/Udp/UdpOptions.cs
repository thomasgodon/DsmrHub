using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsmrHub.Udp
{
    internal class UdpOptions
    {
        public bool Enabled { get; set; } = default!;
        public string Host { get; set; } = default!;
        public Dictionary<string, int> PortMapping { get; set; } = default!;
    }
}
