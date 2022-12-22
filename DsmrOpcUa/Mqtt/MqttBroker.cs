using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmrOpcUa.Dsmr;
using DsmrParser.Models;
using MQTTnet.Server;
using MQTTnet;

namespace DsmrOpcUa.Mqtt
{
    internal class MqttBroker : IDsmrProcessor
    {
        private readonly ILogger<MqttBroker> _logger;

        public MqttBroker(ILogger<MqttBroker> logger)
        {
            _logger = logger;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            var mqttServerOptions = new MqttServerOptions();
            var mqttServer = new MqttFactory().CreateMqttServer(mqttServerOptions);
            await mqttServer.StartAsync();
        }

        Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
