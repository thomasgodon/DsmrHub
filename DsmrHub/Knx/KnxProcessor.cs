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
                // Electricity general
                yield return telegram.ElectricityTariff is not null
                    ? UpdateValue(nameof(Telegram.ElectricityTariff), BitConverter.GetBytes(telegram.ElectricityTariff.Value == 1))
                    : null;

                // Energy delivered
                yield return telegram.EnergyDeliveredTariff1?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyDeliveredTariff1), BitConverter.GetBytes((float)telegram.EnergyDeliveredTariff1.Value))
                    : null;

                yield return telegram.EnergyDeliveredTariff2?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyDeliveredTariff2), BitConverter.GetBytes((float)telegram.EnergyDeliveredTariff2.Value))
                    : null;

                // Energy returned
                yield return telegram.EnergyReturnedTariff1?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyReturnedTariff1), BitConverter.GetBytes((float)telegram.EnergyReturnedTariff1.Value))
                    : null;

                yield return telegram.EnergyReturnedTariff2?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyReturnedTariff2), BitConverter.GetBytes((float)telegram.EnergyReturnedTariff2.Value))
                    : null;

                // Power delivered
                yield return telegram.PowerDelivered?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDelivered), BitConverter.GetBytes((float)telegram.PowerDelivered.Value))
                    : null;

                yield return telegram.PowerDeliveredL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDeliveredL1), BitConverter.GetBytes((float)telegram.PowerDeliveredL1.Value))
                    : null;

                yield return telegram.PowerDeliveredL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDeliveredL2), BitConverter.GetBytes((float)telegram.PowerDeliveredL2.Value))
                    : null;

                yield return telegram.PowerDeliveredL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDeliveredL3), BitConverter.GetBytes((float)telegram.PowerDeliveredL3.Value))
                    : null;

                // Power returned
                yield return telegram.PowerReturned?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturned), BitConverter.GetBytes((float)telegram.PowerReturned.Value))
                    : null;

                yield return telegram.PowerReturnedL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturnedL1), BitConverter.GetBytes((float)telegram.PowerReturnedL1.Value))
                    : null;

                yield return telegram.PowerReturnedL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturnedL2), BitConverter.GetBytes((float)telegram.PowerReturnedL2.Value))
                    : null;

                yield return telegram.PowerReturnedL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturnedL3), BitConverter.GetBytes((float)telegram.PowerReturnedL3.Value))
                    : null;

                // Current amperage
                yield return telegram.CurrentL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.CurrentL1), BitConverter.GetBytes((float)telegram.CurrentL1.Value))
                    : null;

                yield return telegram.CurrentL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.CurrentL2), BitConverter.GetBytes((float)telegram.CurrentL2.Value))
                    : null;

                yield return telegram.CurrentL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.CurrentL3), BitConverter.GetBytes((float)telegram.CurrentL3.Value))
                    : null;

                // Current voltage
                yield return telegram.VoltageL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.VoltageL1), BitConverter.GetBytes((float)telegram.VoltageL1.Value))
                    : null;

                yield return telegram.VoltageL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.VoltageL2), BitConverter.GetBytes((float)telegram.VoltageL2.Value))
                    : null;

                yield return telegram.VoltageL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.VoltageL3), BitConverter.GetBytes((float)telegram.VoltageL3.Value))
                    : null;

                // Gas
                yield return telegram.GasDelivered?.Value?.Value is not null
                    ? UpdateValue(nameof(Telegram.GasDelivered), BitConverter.GetBytes((float)telegram.GasDelivered.Value.Value))
                    : null;

                yield return telegram.GasValvePosition is not null
                    ? UpdateValue(nameof(Telegram.GasValvePosition), BitConverter.GetBytes(telegram.GasValvePosition.Value == 1))
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
            if (value.Value is null)
            {
                return;
            }

            var groupValue = new GroupValue(value.Value.Reverse().ToArray());
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
