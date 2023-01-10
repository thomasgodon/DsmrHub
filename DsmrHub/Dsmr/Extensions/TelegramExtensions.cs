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

        public static byte[] ToUdpPacket(this Telegram telegram, string property)
        {
            var value = telegram.GetType().GetProperty(property)?.GetValue(telegram, null);
            var subValue = value?.GetType().GetProperty("Value")?.GetValue(value, null);
            var subSubValue = subValue?.GetType().GetProperty("Value")?.GetValue(subValue, null);

            return Encoding.UTF8.GetBytes(subSubValue?.ToInvariantString() ?? subValue?.ToInvariantString() ?? value?.ToInvariantString() ?? string.Empty);
        }

        private static string? ToInvariantString(this object value)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString(CultureInfo.InvariantCulture);
            }
            return value.ToString();
        }
    }
}
