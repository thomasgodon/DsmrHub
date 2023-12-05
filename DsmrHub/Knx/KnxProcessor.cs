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
        private readonly Dictionary<string, KnxTelegramValue> _telegrams;
        private readonly Dictionary<GroupAddress, string> _capabilityAddressMapping;
        private readonly object _telegramsLock = new();
        private KnxBus? _knxBus;

        public KnxProcessor(ILogger<KnxProcessor> logger, IOptions<KnxOptions> knxOptions)
        {
            _logger = logger;
            _knxOptions = knxOptions.Value;
            _telegrams = BuildTelegrams(_knxOptions);
            _capabilityAddressMapping = BuildCapabilityAddressMapping(_knxOptions);
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            if (_knxBus?.ConnectionState == BusConnectionState.Connected)
            {
                return;
            }

            _knxBus = new KnxBus(new IpTunnelingConnectorParameters(_knxOptions.Host, _knxOptions.Port));
            _knxBus.GroupMessageReceived += async (_, args) =>
            {
                await ProcessGroupMessageReceivedAsync(args, cancellationToken);
            };

            _logger.LogInformation("Connecting to {host}", _knxOptions.Host);
            await _knxBus.ConnectAsync(cancellationToken);
            await _knxBus.SetInterfaceConfigurationAsync(new BusInterfaceConfiguration(_knxOptions.IndividualAddress), cancellationToken);
            _logger.LogInformation("Connected to {host}", _knxOptions.Host);
        }

        public async Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (_knxOptions.Enabled is false) return;

            var processCancellationToken = new CancellationTokenSource();
            cancellationToken.Register(() =>
            {
                processCancellationToken.Cancel();
            });

            // get updated values
            var updatedValues = UpdateValues(telegram)
                .Where(m => m is not null)
                .ToList();

            // send updated values
            foreach (var updatedValue in updatedValues)
            {
                await WriteGroupValueAsync(updatedValue!, cancellationToken);
            }
        }

        private async Task ProcessGroupMessageReceivedAsync(GroupEventArgs e, CancellationToken cancellationToken)
        {
            if (e.EventType != GroupEventType.ValueRead)
            {
                return;
            }

            if (_capabilityAddressMapping.TryGetValue(e.DestinationAddress, out var capability) is false)
            {
                return;
            }

            KnxTelegramValue? knxTelegramValue;
            lock (_telegrams)
            {
                if (_telegrams.TryGetValue(capability, out knxTelegramValue) is false)
                {
                    return;
                }
            }

            await RespondGroupValueAsync(knxTelegramValue, cancellationToken);
        }

        private IEnumerable<KnxTelegramValue?> UpdateValues(Telegram telegram)
        {
            lock (_telegramsLock)
            {
                // Electricity tariff - 1.024
                yield return telegram.ElectricityTariff is not null
                    ? UpdateValue(nameof(Telegram.ElectricityTariff), BitConverter.GetBytes(telegram.ElectricityTariff.Value == 1))
                    : null;

                // Energy delivered - 14.*
                yield return telegram.EnergyDeliveredTariff1?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyDeliveredTariff1), BitConverter.GetBytes((float)(telegram.EnergyDeliveredTariff1.Value)))
                    : null;

                yield return telegram.EnergyDeliveredTariff2?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyDeliveredTariff2), BitConverter.GetBytes((float)(telegram.EnergyDeliveredTariff2.Value)))
                    : null;

                // Energy returned - 14.*
                yield return telegram.EnergyReturnedTariff1?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyReturnedTariff1), BitConverter.GetBytes((float)(telegram.EnergyReturnedTariff1.Value)))
                    : null;

                yield return telegram.EnergyReturnedTariff2?.Value is not null
                    ? UpdateValue(nameof(Telegram.EnergyReturnedTariff2), BitConverter.GetBytes((float)(telegram.EnergyReturnedTariff2.Value)))
                    : null;

                // Power delivered - 14.056
                yield return telegram.PowerDelivered?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDelivered), BitConverter.GetBytes((float)(telegram.PowerDelivered.Value * 1000)))
                    : null;

                yield return telegram.PowerDeliveredL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDeliveredL1), BitConverter.GetBytes((float)(telegram.PowerDeliveredL1.Value * 1000)))
                    : null;

                yield return telegram.PowerDeliveredL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDeliveredL2), BitConverter.GetBytes((float)(telegram.PowerDeliveredL2.Value * 1000)))
                    : null;

                yield return telegram.PowerDeliveredL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerDeliveredL3), BitConverter.GetBytes((float)(telegram.PowerDeliveredL3.Value * 1000)))
                    : null;

                // Power returned - 14.056
                yield return telegram.PowerReturned?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturned), BitConverter.GetBytes((float)(telegram.PowerReturned.Value * 1000)))
                    : null;

                yield return telegram.PowerReturnedL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturnedL1), BitConverter.GetBytes((float)(telegram.PowerReturnedL1.Value * 1000)))
                    : null;

                yield return telegram.PowerReturnedL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturnedL2), BitConverter.GetBytes((float)(telegram.PowerReturnedL2.Value * 1000)))
                    : null;

                yield return telegram.PowerReturnedL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.PowerReturnedL3), BitConverter.GetBytes((float)(telegram.PowerReturnedL3.Value * 1000)))
                    : null;

                // Current amperage - 14.019
                yield return telegram.CurrentL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.CurrentL1), BitConverter.GetBytes((float)telegram.CurrentL1.Value))
                    : null;

                yield return telegram.CurrentL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.CurrentL2), BitConverter.GetBytes((float)telegram.CurrentL2.Value))
                    : null;

                yield return telegram.CurrentL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.CurrentL3), BitConverter.GetBytes((float)telegram.CurrentL3.Value))
                    : null;

                // Current voltage - 14.027
                yield return telegram.VoltageL1?.Value is not null
                    ? UpdateValue(nameof(Telegram.VoltageL1), BitConverter.GetBytes((float)telegram.VoltageL1.Value))
                    : null;

                yield return telegram.VoltageL2?.Value is not null
                    ? UpdateValue(nameof(Telegram.VoltageL2), BitConverter.GetBytes((float)telegram.VoltageL2.Value))
                    : null;

                yield return telegram.VoltageL3?.Value is not null
                    ? UpdateValue(nameof(Telegram.VoltageL3), BitConverter.GetBytes((float)telegram.VoltageL3.Value))
                    : null;

                // Gas - 14.076
                yield return telegram.GasDelivered?.Value?.Value is not null
                    ? UpdateValue(nameof(Telegram.GasDelivered), BitConverter.GetBytes((float)telegram.GasDelivered.Value.Value))
                    : null;

                // Gas Valve position - 1.001
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

        private async Task RespondGroupValueAsync(KnxTelegramValue value, CancellationToken cancellationToken)
        {
            if (value.Value is null)
            {
                return;
            }

            if (_knxBus?.ConnectionState != BusConnectionState.Connected)
            {
                await ConnectAsync(cancellationToken);
            }

            if (_knxBus == null)
            {
                _logger.LogError("Something went wrong after connecting to knx client");
                return;
            }

            var groupValue = new GroupValue(value.Value.Reverse().ToArray());
            await _knxBus.RespondGroupValueAsync(value.Address, groupValue, MessagePriority.Low, cancellationToken);
        }

        private async Task WriteGroupValueAsync(KnxTelegramValue value, CancellationToken cancellationToken)
        {
            if (value.Value is null)
            {
                return;
            }

            if (_knxBus?.ConnectionState != BusConnectionState.Connected)
            {
                await ConnectAsync(cancellationToken);
            }

            if (_knxBus == null)
            {
                _logger.LogError("Something went wrong after connecting to knx client");
                return;
            }

            var groupValue = new GroupValue(value.Value.Reverse().ToArray());
            await _knxBus.WriteGroupValueAsync(value.Address, groupValue, MessagePriority.Low, cancellationToken);
        }

        private static Dictionary<string, KnxTelegramValue> BuildTelegrams(KnxOptions knxOptions)
        {
            var telegrams = new Dictionary<string, KnxTelegramValue> (knxOptions.GroupAddressMapping.Count);

            foreach (var groupAddressMapping in GroupAddressMappingsFromOptions(knxOptions))
            {
                telegrams.Add(groupAddressMapping.Key, new KnxTelegramValue(groupAddressMapping.Value));
            }

            return telegrams;
        }

        private static Dictionary<GroupAddress, string> BuildCapabilityAddressMapping(KnxOptions knxOptions)
            => GroupAddressMappingsFromOptions(knxOptions)
                .ToDictionary(
                    groupAddressMapping => GroupAddress.Parse(groupAddressMapping.Value), 
                    groupAddressMapping => groupAddressMapping.Key);

        private static IEnumerable<KeyValuePair<string, string>> GroupAddressMappingsFromOptions(KnxOptions options)
            => options.GroupAddressMapping
                .Where(
                    mapping => string.IsNullOrEmpty(mapping.Value) is false);
    }
}
