using DsmrHub.Dsmr;
using DSMRParser.Models;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using Microsoft.Extensions.Options;

namespace DsmrHub.Knx
{
    internal class KnxProcessor : IDsmrProcessor
    {
        private readonly ILogger<KnxProcessor> _logger;
        private readonly KnxOptions _knxOptions;
        private readonly KnxBus _knxClient;
        private readonly Dictionary<string, KnxTelegramValue> _telegrams;
        private readonly object _telegramsLock = new();

        public KnxProcessor(ILogger<KnxProcessor> logger, IOptions<KnxOptions> knxOptions)
        {
            _logger = logger;
            _knxOptions = knxOptions.Value;
            _telegrams = BuildTelegrams(_knxOptions);
            _knxClient = new KnxBus(new IpTunnelingConnectorParameters(_knxOptions.Host, _knxOptions.Port));
        }

        public async Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (_knxOptions.Enabled is false) return;

            // connect to the KNXnet/IP gateway
            if (_knxClient.ConnectionState != BusConnectionState.Connected)
            {
                try
                {
                    await _knxClient.ConnectAsync(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Couldn't connect to '{address}'", _knxOptions.Host);
                    await _knxClient.DisposeAsync();
                }
            }

            // get updated values
            var updatedValues = UpdateValues(telegram)
                .Where(m => m is not null)
                .ToList();

            // send updated values
            foreach (var updatedValue in updatedValues)
            {
                await SendValueAsync(updatedValue!, cancellationToken);
            }
        }

        private IEnumerable<KnxTelegramValue?> UpdateValues(Telegram telegram)
        {
            lock (_telegramsLock)
            {
                yield return telegram.GasDelivered?.Value?.Value is not null
                    ? UpdateValue(nameof(Telegram.GasDelivered), BitConverter.GetBytes((float)telegram.GasDelivered.Value.Value))
                    : null;
            }
        }

        private KnxTelegramValue? UpdateValue(string capability, byte[] value)
        {
            if (_telegrams.TryGetValue(capability, out var knxTelegram) is false)
            {
                return null;
            }

            if (knxTelegram.Value is not null)
            {
                if (knxTelegram.Value.SequenceEqual(value))
                {
                    return null;
                }
            }

            _telegrams[capability].Value = value;
            return _telegrams[capability];
        }

        private async Task SendValueAsync(KnxTelegramValue value, CancellationToken cancellationToken)
        {
            var groupValue = new GroupValue(value.Value);
            await _knxClient.WriteGroupValueAsync(value.Address, groupValue, MessagePriority.Low, cancellationToken);
        }

        private static Dictionary<string, KnxTelegramValue> BuildTelegrams(KnxOptions options)
        {
            var telegrams = new Dictionary<string, KnxTelegramValue> (options.GroupAddressMapping.Count);

            var groupAddressMappings = options.GroupAddressMapping
                .Where(groupAddressMapping => string.IsNullOrEmpty(groupAddressMapping.Value) is false);

            foreach (var groupAddressMapping in groupAddressMappings)
            {
                telegrams.Add(groupAddressMapping.Key, new KnxTelegramValue(groupAddressMapping.Value));
            }

            return telegrams;
        }
    }
}
