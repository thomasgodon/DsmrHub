using System.Globalization;
using System.Net.Sockets;

namespace DsmrHub.Udp.Extensions
{
    internal static class UdpExtensions
    {
        public static string GetSubValue(this object inputValue, string property)
        {
            var value = inputValue.GetType().GetProperty(property)?.GetValue(inputValue, null);
            var subValue = value?.GetType().GetProperty("Value")?.GetValue(value, null);
            var subSubValue = subValue?.GetType().GetProperty("Value")?.GetValue(subValue, null);

            return subSubValue?.ToInvariantString() ?? subValue?.ToInvariantString() ?? value?.ToInvariantString() ?? string.Empty;
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
