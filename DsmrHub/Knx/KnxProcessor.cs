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

            lock (_telegramsLock)
            {
                // GAS
                UpdateValue(nameof(telegram.PowerDelivered), BitConverter.GetBytes((float)telegram.PowerDelivered?.Value!));
            }

            await SendValuesAsync(cancellationToken);
        }

        private async Task SendValuesAsync(CancellationToken cancellationToken)
        {
            List<KnxTelegramValue> values;
            lock (_telegramsLock)
            {
                values = _telegrams.Values.ToList();
            }

            foreach (var knxTelegramValue in values)
            {
                var address = knxTelegramValue.Address;
                var value = knxTelegramValue.Value;

                _logger.LogWarning("logged: {value}", value);

                if (value is null)
                {
                    continue;
                }

                await _knxClient.WriteGroupValueAsync(
                    address,
                    new GroupValue(value),
                    MessagePriority.Low,
                    cancellationToken: cancellationToken);
            }
        }

        private void UpdateValue(string capability, byte[] value)
        {
            if (_telegrams.TryGetValue(capability, out var knxTelegram) is false)
            {
                return;
            }

            if (knxTelegram.Value == value)
            {
                return;
            }

            _telegrams[capability].Value = value;
        }

        private static Dictionary<string, KnxTelegramValue> BuildTelegrams(KnxOptions options)
        {
            var telegrams = new Dictionary<string, KnxTelegramValue> (options.GroupAddressMapping.Count);

            foreach (var groupAddressMapping in options.GroupAddressMapping)
            {
                if (string.IsNullOrEmpty(groupAddressMapping.Value.Address))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(groupAddressMapping.Value.DataPointType))
                {
                    continue;
                }

                telegrams.Add(groupAddressMapping.Key, new KnxTelegramValue(groupAddressMapping.Value.Address, groupAddressMapping.Value.DataPointType));
            }

            return telegrams;
        }
    }
}
