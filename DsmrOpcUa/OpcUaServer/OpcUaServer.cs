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
    internal class OpcUaServer : IDsmrProcessor, IOpcUaServer
    {
        private readonly ILogger<OpcUaServer> _logger;

        public OpcUaServer(ILogger<OpcUaServer> logger)
        {
            _logger = logger;
            Start();
        }

        public void Start()
        {
            using var server = new OpcServer("opc.tcp://localhost:50000/");
            server.Start();
        }

        Task IDsmrProcessor.ProcessTelegram(Telegram telegram)
        {
            return Task.CompletedTask;
        }
    }
}
