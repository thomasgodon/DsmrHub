using DsmrParser.Models;
using MQTTnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DsmrOpcUa.Mqtt.Extensions
{
    internal static class TelegramExtensions
    {
        public static MqttApplicationMessage? ToApplicationMessage(this Telegram telegram, string property)
        {
            var value = telegram.GetType().GetProperty(property)?.GetValue(telegram, null);
            if (value == null) return null;

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"dsmr/{property}")
                .WithPayload(value.ToString())
                .Build();

            return message;
        }
    }
}
