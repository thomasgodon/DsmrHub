using DsmrHub.Dsmr;
using DsmrHub.Dsmr.Extensions;
using DsmrHub.Udp.Extensions;
using DSMRParser.Models;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace DsmrHub.Udp
{
    internal class UdpProcessor : IDsmrProcessor
    {
        private readonly ILogger<UdpProcessor> _logger;
        private readonly UdpOptions _udpOptions;

        public UdpProcessor(ILogger<UdpProcessor> logger, IOptions<UdpOptions> udpOptions)
        {
            _logger = logger;
            _udpOptions = udpOptions.Value;
        }

        async Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (!_udpOptions.Enabled) return;

            foreach (var (udpData, port) in GenerateUdpMessages(telegram, _udpOptions.PortMapping).ToList())
            {
                using var udpSender = new UdpClient();
                udpSender.Connect(_udpOptions.Host, port);
                await udpSender.SendAsync(udpData, cancellationToken);
            }
        }

        private static IEnumerable<(byte[] Data, int Port)> GenerateUdpMessages(Telegram telegram, IReadOnlyDictionary<string, int> portMapping)
        {
            foreach (var propertyInfo in telegram.GetType().GetProperties())
            {
                dynamic propertyValue;
                try
                {
                    propertyValue = Convert.ChangeType(propertyInfo.GetValue(telegram, null)?.GetSubValue(propertyInfo.Name), propertyInfo.PropertyType)!;
                }
                catch (Exception)
                {
                    yield break;
                }

                if (propertyValue == null)
                {
                    continue;
                }

                if (propertyValue is Enum)
                {
                    propertyValue = (int)propertyValue;
                }

                if (portMapping.TryGetValue(propertyInfo.Name, out var port) is false)
                {
                    continue;
                }

                yield return new ValueTuple<byte[], int>(Encoding.UTF8.GetBytes(propertyValue.ToString()), port);
            }
        }    }
}
