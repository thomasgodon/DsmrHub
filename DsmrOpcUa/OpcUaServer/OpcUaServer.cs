using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmrOpcUa.Dsmr;
using DsmrParser.Models;
using Opc.UaFx.Server;

namespace DsmrOpcUa.OpcUaServer
{
    internal class OpcUaServer : IDsmrProcessor
    {
        private readonly ILogger<OpcUaServer> _logger;

        public OpcUaServer(ILogger<OpcUaServer> logger)
        {
            _logger = logger;
        }

        Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
