using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsmrHub.Knx
{
    internal class GroupAddressMapping
    {
        public GroupAddressMapping(string address, string dataPointType)
        {
            Address = address;
            DataPointType = dataPointType;
        }

        public string? Address { get; init; }
        public string? DataPointType { get; init; }
    }
}
