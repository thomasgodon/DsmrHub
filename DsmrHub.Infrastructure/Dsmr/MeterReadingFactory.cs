using DsmrHub.Domain;
using DsmrHub.Domain.ValueObjects;
using DsmrHub.Infrastructure.Dsmr.Parsing;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Maps a tokenised <see cref="P1Telegram"/> onto the domain's <see cref="MeterReading"/>.
/// This is the single place where Fluvius e-MUCS OBIS codes are interpreted, keeping the rest
/// of the codebase OBIS-agnostic. Every value is optional; missing codes map to null.
/// </summary>
internal static class MeterReadingFactory
{
    // M-Bus device-type codes (OBIS 0-n:24.1.0).
    private const int GasDeviceType = 3;
    private const int WaterDeviceType = 7;

    public static MeterReading ToMeterReading(P1Telegram t)
    {
        var mBusDevices = ReadMBusDevices(t);

        return new MeterReading(
            identification: t.Header,
            dsmrVersion: ObisValueParser.Int(t.Value("0-0:96.1.4")) ?? ObisValueParser.Int(t.Value("1-3:0.2.8")),
            timestamp: ObisValueParser.Timestamp(t.Value("0-0:1.0.0")),
            electricity: ReadElectricity(t),
            gas: ReadGas(t, mBusDevices),
            electricityEquipmentId: ObisValueParser.HexToAscii(t.Value("0-0:96.1.1")),
            textMessage: ObisValueParser.HexToAscii(t.Value("0-0:96.13.0")),
            water: ReadWater(t, mBusDevices),
            mBusDevices: mBusDevices);
    }

    private static ElectricityReading ReadElectricity(P1Telegram t)
    {
        var runningMonth = t.Groups("1-0:1.6.0");

        return new ElectricityReading(
            Tariff: MapTariff(ObisValueParser.Int(t.Value("0-0:96.14.0"))),
            EnergyDeliveredTariff1: Energy(t.Value("1-0:1.8.1")),
            EnergyDeliveredTariff2: Energy(t.Value("1-0:1.8.2")),
            EnergyReturnedTariff1: Energy(t.Value("1-0:2.8.1")),
            EnergyReturnedTariff2: Energy(t.Value("1-0:2.8.2")),
            // Preserves the existing field semantics: 1-0:1.6.0 reports a kW peak, surfaced here
            // as the running-month value (kept as-is so existing KNX/MQTT sinks are unaffected).
            EnergyDeliveredMaxRunningMonth: Energy(runningMonth.Count > 1 ? runningMonth[1] : null),
            EnergyDeliveredMaxRunningMonthTimestamp: ObisValueParser.Timestamp(runningMonth.Count > 0 ? runningMonth[0] : null),
            PowerDelivered: Power(t.Value("1-0:1.7.0")),
            PowerReturned: Power(t.Value("1-0:2.7.0")),
            PowerDeliveredCurrentAvg: Power(t.Value("1-0:1.4.0")),
            PhaseL1: ReadPhase(t, deliver: "1-0:21.7.0", ret: "1-0:22.7.0", voltage: "1-0:32.7.0", current: "1-0:31.7.0", sags: "1-0:32.32.0", swells: "1-0:32.36.0"),
            PhaseL2: ReadPhase(t, deliver: "1-0:41.7.0", ret: "1-0:42.7.0", voltage: "1-0:52.7.0", current: "1-0:51.7.0", sags: "1-0:52.32.0", swells: "1-0:52.36.0"),
            PhaseL3: ReadPhase(t, deliver: "1-0:61.7.0", ret: "1-0:62.7.0", voltage: "1-0:72.7.0", current: "1-0:71.7.0", sags: "1-0:72.32.0", swells: "1-0:72.36.0"),
            PowerFailuresCount: ObisValueParser.Int(t.Value("0-0:96.7.21")),
            LongPowerFailuresCount: ObisValueParser.Int(t.Value("0-0:96.7.9")),
            PowerFailureLog: ReadPowerFailureLog(t),
            BreakerState: MapBreakerState(ObisValueParser.Int(t.Value("0-0:96.3.10"))),
            LimiterThreshold: Power(t.Value("0-0:17.0.0")),
            FuseThreshold: Current(t.Value("1-0:31.4.0")),
            MaxDemandHistory: ReadMaxDemandHistory(t));
    }

    private static ElectricityPhase ReadPhase(P1Telegram t, string deliver, string ret, string voltage, string current, string sags, string swells)
        => new(
            PowerDelivered: Power(t.Value(deliver)),
            PowerReturned: Power(t.Value(ret)),
            Voltage: Voltage(t.Value(voltage)),
            Current: Current(t.Value(current)),
            VoltageSags: ObisValueParser.Int(t.Value(sags)),
            VoltageSwells: ObisValueParser.Int(t.Value(swells)));

    private static IReadOnlyList<PowerFailureEvent>? ReadPowerFailureLog(P1Telegram t)
    {
        var groups = t.Groups("1-0:99.97.0");
        if (groups.Count == 0)
        {
            return null;
        }

        // (count)(0-0:96.7.19) then repeating (endTimestamp)(duration*s) pairs.
        var events = new List<PowerFailureEvent>();
        for (var i = 2; i + 1 < groups.Count; i += 2)
        {
            var end = ObisValueParser.Timestamp(groups[i]);
            var duration = ObisValueParser.Duration(groups[i + 1]);
            if (end is not null && duration is not null)
            {
                events.Add(new PowerFailureEvent(end.Value, duration.Value));
            }
        }

        return events;
    }

    private static IReadOnlyList<MonthlyPeakDemand>? ReadMaxDemandHistory(P1Telegram t)
    {
        var groups = t.Groups("0-0:98.1.0");
        if (groups.Count == 0)
        {
            return null;
        }

        // (count)(1-0:1.6.0)(1-0:1.6.0) then repeating (periodStart)(peakOccurredAt)(peak*kW) triplets.
        var history = new List<MonthlyPeakDemand>();
        for (var i = 3; i + 2 < groups.Count; i += 3)
        {
            var periodStart = ObisValueParser.Timestamp(groups[i]);
            var peakAt = ObisValueParser.Timestamp(groups[i + 1]);
            var peak = Power(groups[i + 2]);
            if (periodStart is not null && peakAt is not null && peak is not null)
            {
                history.Add(new MonthlyPeakDemand(periodStart.Value, peakAt.Value, peak.Value));
            }
        }

        return history;
    }

    private static IReadOnlyList<MBusDevice> ReadMBusDevices(P1Telegram t)
    {
        var devices = new List<MBusDevice>();

        for (var channel = 1; channel <= 4; channel++)
        {
            var prefix = $"0-{channel}:";

            var deviceType = ObisValueParser.Int(t.Value($"{prefix}24.1.0"));
            var equipmentId = ObisValueParser.HexToAscii(t.Value($"{prefix}96.1.1") ?? t.Value($"{prefix}96.1.0"));
            var valve = ObisValueParser.Int(t.Value($"{prefix}24.4.0"));
            var (value, unit, capture) = ReadMBusMeasurement(t, prefix);

            // Skip channels with no data at all.
            if (deviceType is null && equipmentId is null && valve is null && value is null)
            {
                continue;
            }

            devices.Add(new MBusDevice(channel, deviceType, equipmentId, valve, value, unit, capture));
        }

        return devices;
    }

    private static (decimal? Value, string? Unit, DateTimeOffset? CaptureTime) ReadMBusMeasurement(P1Telegram t, string prefix)
    {
        // e-MUCS uses 24.2.3; older DSMR uses 24.2.1. Both are (timestamp)(value*unit).
        var groups = t.Groups($"{prefix}24.2.3");
        if (groups.Count == 0)
        {
            groups = t.Groups($"{prefix}24.2.1");
        }

        if (groups.Count == 0)
        {
            return (null, null, null);
        }

        var capture = groups.Count > 0 ? ObisValueParser.Timestamp(groups[0]) : null;
        var parsed = groups.Count > 1 ? ObisValueParser.DecimalWithUnit(groups[1]) : null;
        return (parsed?.Value, parsed?.Unit, capture);
    }

    private static GasReading ReadGas(P1Telegram t, IReadOnlyList<MBusDevice> devices)
    {
        var gas = FindByDeviceType(devices, GasDeviceType);
        if (gas is null)
        {
            return GasReading.Empty;
        }

        return new GasReading(
            DeviceType: gas.DeviceType,
            EquipmentId: gas.EquipmentId,
            ValvePosition: gas.ValvePosition,
            Delivered: gas.Value is { } v ? GasVolume.FromCubicMeters(v) : null,
            DeliveredTimestamp: gas.CaptureTime);
    }

    private static WaterReading ReadWater(P1Telegram t, IReadOnlyList<MBusDevice> devices)
    {
        var water = FindByDeviceType(devices, WaterDeviceType);
        if (water is null)
        {
            return WaterReading.Empty;
        }

        return new WaterReading(
            DeviceType: water.DeviceType,
            EquipmentId: water.EquipmentId,
            Delivered: water.Value is { } v ? WaterVolume.FromCubicMeters(v) : null,
            DeliveredTimestamp: water.CaptureTime);
    }

    private static MBusDevice? FindByDeviceType(IReadOnlyList<MBusDevice> devices, int deviceType)
    {
        foreach (var device in devices)
        {
            if (device.DeviceType == deviceType)
            {
                return device;
            }
        }

        return null;
    }

    private static ElectricityTariff MapTariff(int? tariff) => tariff switch
    {
        1 => ElectricityTariff.Tariff1,
        2 => ElectricityTariff.Tariff2,
        _ => ElectricityTariff.Unknown,
    };

    private static ElectricityBreakerState MapBreakerState(int? state) => state switch
    {
        0 => ElectricityBreakerState.Disconnected,
        1 => ElectricityBreakerState.Connected,
        2 => ElectricityBreakerState.ReadyForReconnection,
        _ => ElectricityBreakerState.Unknown,
    };

    private static EnergyValue? Energy(string? group)
        => ObisValueParser.Decimal(group) is { } v ? EnergyValue.FromKilowattHours(v) : null;

    private static PowerValue? Power(string? group)
        => ObisValueParser.Decimal(group) is { } v ? PowerValue.FromKilowatts(v) : null;

    private static VoltageValue? Voltage(string? group)
        => ObisValueParser.Decimal(group) is { } v ? VoltageValue.FromVolts(v) : null;

    private static CurrentValue? Current(string? group)
        => ObisValueParser.Decimal(group) is { } v ? CurrentValue.FromAmperes(v) : null;
}
