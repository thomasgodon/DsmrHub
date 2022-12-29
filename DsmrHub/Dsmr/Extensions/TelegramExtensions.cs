using System.Text;
using DSMRParser.Models;
using MQTTnet;

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

        public static byte[] ToUdpPacket(this Telegram telegram, string property)
        {
            var value = telegram.GetType().GetProperty(property)?.GetValue(telegram, null);
            var message = Encoding.UTF8.GetBytes(value?.ToString() ?? string.Empty);

            return message;
        }
    }
}
