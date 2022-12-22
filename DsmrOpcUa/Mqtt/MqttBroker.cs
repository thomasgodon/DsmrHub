using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmrOpcUa.Dsmr;
using DsmrParser.Models;

namespace DsmrOpcUa.Mqtt
{
    internal class MqttBroker : IDsmrProcessor, IMqttBroker
    {
        private readonly ILogger<MqttBroker> _logger;

        public MqttBroker(ILogger<MqttBroker> logger)
        {
            _logger = logger;
        }

        public void Start()
        {

        }

        Task IDsmrProcessor.ProcessTelegram(Telegram telegram)
        {
            throw new NotImplementedException();
        }
    }
}
