using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DsmrOpcUa.Dsmr;
using DsmrParser.Models;
using MQTTnet.Server;
using MQTTnet;

namespace DsmrOpcUa.Mqtt
{
    internal class MqttBroker : IMqttBroker
    {
        private readonly ILogger<MqttBroker> _logger;
        private MqttServer _server;
        private MqttServerOptions _serverOptions;

        public MqttBroker(ILogger<MqttBroker> logger)
        {
            _logger = logger;

            _serverOptions = BuildServerOptions();
            _server = new MqttFactory().CreateMqttServer(_serverOptions);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_server.IsStarted)
            {
                await _server.StartAsync().WaitAsync(cancellationToken);
                _logger.LogInformation($"{nameof(MqttBroker)} started on port: {_serverOptions.DefaultEndpointOptions.Port}");
            }
        }

        private static MqttServerOptions BuildServerOptions()
        {
            var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(100)
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(1883)
                .WithDefaultEndpointBoundIPAddress(IPAddress.Any);

            return optionsBuilder.Build();
        }
    }
}
