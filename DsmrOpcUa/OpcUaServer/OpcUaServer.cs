using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmrOpcUa.Dsmr;
using DsmrParser.Models;

namespace DsmrOpcUa.OpcUaServer
{
    internal class OpcUaServer : IDsmrProcessor
    {
        public Task ProcessTelegram(Telegram telegram)
        {
            throw new NotImplementedException();
        }
    }
}
