using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DsmrOpcUa.Dsmr;
using DsmrParser.Models;
using Microsoft.Extensions.Options;
using MQTTnet.Server;
using MQTTnet;
using MQTTnet.Protocol;
using Opc.Ua;

namespace DsmrOpcUa.Mqtt
{
    internal class MqttBroker : IMqttBroker
    {
        private readonly ILogger<MqttBroker> _logger;
        private readonly MqttServer _server;
        private readonly MqttServerOptions _serverOptions;
        private readonly MqttOptions _mqttOptions;

        public int Port => _serverOptions.DefaultEndpointOptions.Port;

        public MqttBroker(ILogger<MqttBroker> logger, IOptions<MqttOptions> mqttOptions)
        {
            _logger = logger;
            _mqttOptions = mqttOptions.Value;

            _serverOptions = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(100)
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(_mqttOptions.Port)
                .Build();
            _server = new MqttFactory().CreateMqttServer(_serverOptions);
            _server.ValidatingConnectionAsync += ValidateConnectionAsyncHandler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_server.IsStarted)
            {
                _logger.LogInformation("MQTT Broker starting...");
                await _server.StartAsync().WaitAsync(cancellationToken);
                _logger.LogInformation($"{nameof(MqttBroker)} started on port: {_serverOptions.DefaultEndpointOptions.Port}");
            }
        }

        private Task ValidateConnectionAsyncHandler(ValidatingConnectionEventArgs args)
        {
            if (args.UserName != _mqttOptions.Username)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return Task.CompletedTask;
            }
            if (args.Password != _mqttOptions.Password)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }
    }
}
