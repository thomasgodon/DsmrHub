using DsmrHub.Domain;
using DsmrHub.Domain.Events;
using DsmrHub.Domain.ValueObjects;
using DsmrHub.Infrastructure.Options;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static System.Threading.Tasks.Task;

namespace DsmrHub.Infrastructure.Knx;

/// <summary>
/// Writes meter values onto a KNX bus over IP tunneling and answers KNX read requests.
/// Ported from the original <c>KnxProcessor</c>: the capability keys, byte encodings, scale
/// factors (kW->W, kWh, running-month *1000) and read-response behavior are preserved so existing
/// <c>GroupAddressMapping</c> configuration keeps working.
/// </summary>
internal sealed class KnxMeterReadingHandler : INotificationHandler<MeterReadingReceived>
{
    // Capability keys must match the keys used in KnxOptions.GroupAddressMapping (appsettings.json).
    private const string ElectricityTariffKey = "ElectricityTariff";
    private const string EnergyDeliveredTariff1Key = "EnergyDeliveredTariff1";
    private const string EnergyDeliveredTariff2Key = "EnergyDeliveredTariff2";
    private const string EnergyReturnedTariff1Key = "EnergyReturnedTariff1";
    private const string EnergyReturnedTariff2Key = "EnergyReturnedTariff2";
    private const string PowerDeliveredKey = "PowerDelivered";
    private const string PowerDeliveredL1Key = "PowerDeliveredL1";
    private const string PowerDeliveredL2Key = "PowerDeliveredL2";
    private const string PowerDeliveredL3Key = "PowerDeliveredL3";
    private const string PowerDeliveredCurrentAvgKey = "PowerDeliveredCurrentAvg";
    private const string PowerReturnedKey = "PowerReturned";
    private const string PowerReturnedL1Key = "PowerReturnedL1";
    private const string PowerReturnedL2Key = "PowerReturnedL2";
    private const string PowerReturnedL3Key = "PowerReturnedL3";
    private const string CurrentL1Key = "CurrentL1";
    private const string CurrentL2Key = "CurrentL2";
    private const string CurrentL3Key = "CurrentL3";
    private const string VoltageL1Key = "VoltageL1";
    private const string VoltageL2Key = "VoltageL2";
    private const string VoltageL3Key = "VoltageL3";
    private const string GasDeliveredKey = "GasDelivered";
    private const string GasValvePositionKey = "GasValvePosition";
    private const string EnergyDeliveredMaxRunningMonthKey = "EnergyDeliveredMaxRunningMonth";

    private readonly ILogger<KnxMeterReadingHandler> _logger;
    private readonly KnxOptions _knxOptions;
    private readonly Dictionary<string, KnxTelegramValue> _telegrams;
    private readonly Dictionary<GroupAddress, string> _capabilityAddressMapping;
    private readonly object _telegramsLock = new();
    private KnxBus? _knxBus;

    public KnxMeterReadingHandler(ILogger<KnxMeterReadingHandler> logger, IOptions<KnxOptions> knxOptions)
    {
        _logger = logger;
        _knxOptions = knxOptions.Value;
        _telegrams = BuildTelegrams(_knxOptions);
        _capabilityAddressMapping = BuildCapabilityAddressMapping(_knxOptions);
    }

    public async Task Handle(MeterReadingReceived notification, CancellationToken cancellationToken)
    {
        if (_knxOptions.Enabled is false) return;

        var updatedValues = UpdateValues(notification.Reading)
            .Where(m => m is not null)
            .ToList();

        foreach (var updatedValue in updatedValues)
        {
            await WriteGroupValueAsync(updatedValue!, cancellationToken);
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
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
        await _knxBus.SetInterfaceConfigurationAsync(new BusInterfaceConfiguration(IndividualAddress.Parse(_knxOptions.IndividualAddress)), cancellationToken);
        _logger.LogInformation("Connected to {host}", _knxOptions.Host);
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

    private IEnumerable<KnxTelegramValue?> UpdateValues(MeterReading reading)
    {
        var e = reading.Electricity;
        var g = reading.Gas;

        lock (_telegramsLock)
        {
            // Electricity tariff - 1.024
            yield return e.Tariff != ElectricityTariff.Unknown
                ? UpdateValue(ElectricityTariffKey, BitConverter.GetBytes(e.Tariff == ElectricityTariff.Tariff1))
                : null;

            // Energy delivered - 14.*
            yield return Float(EnergyDeliveredTariff1Key, e.EnergyDeliveredTariff1?.KilowattHours);
            yield return Float(EnergyDeliveredTariff2Key, e.EnergyDeliveredTariff2?.KilowattHours);

            // Energy returned - 14.*
            yield return Float(EnergyReturnedTariff1Key, e.EnergyReturnedTariff1?.KilowattHours);
            yield return Float(EnergyReturnedTariff2Key, e.EnergyReturnedTariff2?.KilowattHours);

            // Power delivered - 14.056 (W)
            yield return Float(PowerDeliveredKey, e.PowerDelivered?.Watts);
            yield return Float(PowerDeliveredL1Key, e.PhaseL1.PowerDelivered?.Watts);
            yield return Float(PowerDeliveredL2Key, e.PhaseL2.PowerDelivered?.Watts);
            yield return Float(PowerDeliveredL3Key, e.PhaseL3.PowerDelivered?.Watts);
            yield return Float(PowerDeliveredCurrentAvgKey, e.PowerDeliveredCurrentAvg?.Watts);

            // Power returned - 14.056 (W)
            yield return Float(PowerReturnedKey, e.PowerReturned?.Watts);
            yield return Float(PowerReturnedL1Key, e.PhaseL1.PowerReturned?.Watts);
            yield return Float(PowerReturnedL2Key, e.PhaseL2.PowerReturned?.Watts);
            yield return Float(PowerReturnedL3Key, e.PhaseL3.PowerReturned?.Watts);

            // Current amperage - 14.019
            yield return Float(CurrentL1Key, e.PhaseL1.Current?.Amperes);
            yield return Float(CurrentL2Key, e.PhaseL2.Current?.Amperes);
            yield return Float(CurrentL3Key, e.PhaseL3.Current?.Amperes);

            // Current voltage - 14.027
            yield return Float(VoltageL1Key, e.PhaseL1.Voltage?.Volts);
            yield return Float(VoltageL2Key, e.PhaseL2.Voltage?.Volts);
            yield return Float(VoltageL3Key, e.PhaseL3.Voltage?.Volts);

            // Gas - 14.076
            yield return Float(GasDeliveredKey, g.Delivered?.CubicMeters);

            // Gas valve position - 1.001
            yield return g.ValvePosition is not null
                ? UpdateValue(GasValvePositionKey, BitConverter.GetBytes(g.ValvePosition == 1))
                : null;

            // Energy delivered max running month - 13.010 (Wh as uint)
            yield return e.EnergyDeliveredMaxRunningMonth is { } maxMonth
                ? UpdateValue(EnergyDeliveredMaxRunningMonthKey, BitConverter.GetBytes((uint)(maxMonth.KilowattHours * 1000)))
                : null;
        }
    }

    private KnxTelegramValue? Float(string capability, decimal? value)
        => value is null ? null : UpdateValue(capability, BitConverter.GetBytes((float)value.Value));

    private KnxTelegramValue? UpdateValue(string capability, byte[] value)
    {
        if (_telegrams.TryGetValue(capability, out var knxTelegram) is false)
        {
            return null;
        }

        if (knxTelegram.Value is not null && knxTelegram.Value.SequenceEqual(value))
        {
            return null;
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
        var writeCancellationToken = new CancellationTokenSource();

        // TODO: retry
        await WhenAny(
            _knxBus.WriteGroupValueAsync(value.Address, groupValue, MessagePriority.Low, writeCancellationToken.Token),
            Delay(TimeSpan.FromMilliseconds(100), cancellationToken));

        writeCancellationToken.Cancel();
    }

    private static Dictionary<string, KnxTelegramValue> BuildTelegrams(KnxOptions knxOptions)
    {
        var telegrams = new Dictionary<string, KnxTelegramValue>();

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
        => (options.GroupAddressMapping ?? new Dictionary<string, string>())
            .Where(mapping => string.IsNullOrEmpty(mapping.Value) is false);
}
