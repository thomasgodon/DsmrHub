using System.Globalization;
using System.Text;
using DSMRParser.Models;
using MQTTnet;
using Opc.Ua;

namespace DsmrHub.Dsmr.Extensions
{
    internal static class TelegramExtensions
    {
        public static MqttApplicationMessage? ToApplicationMessage(this Telegram telegram, string property)
        {
            var value = telegram.GetType().GetProperty(property)?.GetValue(telegram, null);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"dsmr/{property}")
                .WithPayload(value?.ToString() ?? string.Empty)
                .Build();

            return message;
        }
    }
}
