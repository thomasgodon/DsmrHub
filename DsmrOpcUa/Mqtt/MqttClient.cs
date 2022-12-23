using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmrOpcUa.Dsmr;
using DsmrParser.Models;

namespace DsmrOpcUa.Mqtt
{
    internal class MqttClient : IDsmrProcessor
    {
        private readonly ILogger<MqttClient> _logger;

        public MqttClient(ILogger<MqttClient> logger)
        {
            _logger = logger;
        }

        Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
