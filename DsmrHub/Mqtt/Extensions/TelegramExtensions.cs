using DsmrParser.Models;
using MQTTnet;

namespace DsmrHub.Mqtt.Extensions
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
