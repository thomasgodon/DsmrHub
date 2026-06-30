using DsmrHub.Domain;
using DsmrHub.Domain.ValueObjects;

namespace DsmrHub.Application.Dashboard;

/// <summary>
/// A flat, JSON-friendly projection of a <see cref="MeterReading"/> carrying every DSMR / e-MUCS
/// value for the live dashboard. Every measurement is nullable: a value the telegram did not contain
/// (or that could not be parsed) stays <c>null</c> here and renders blank ("—") on the page.
/// Units are normalised (V, A, kW, kWh, m³).
/// </summary>
public sealed record DashboardSnapshot(
    DateTimeOffset Timestamp,

    // ---- identity ----
    string? Identification,
    int? DsmrVersion,
    DateTimeOffset? MeterTimestamp,
    string? ElectricityEquipmentId,
    string? TextMessage,

    // ---- electricity totals ----
    int Tariff,
    decimal? EnergyDeliveredTariff1Kwh,
    decimal? EnergyDeliveredTariff2Kwh,
    decimal? EnergyReturnedTariff1Kwh,
    decimal? EnergyReturnedTariff2Kwh,
    decimal? EnergyDeliveredMaxRunningMonthKwh,
    DateTimeOffset? EnergyDeliveredMaxRunningMonthTimestamp,
    decimal? PowerDeliveredKw,
    decimal? PowerReturnedKw,
    decimal? PowerDeliveredCurrentAvgKw,

    // ---- per phase ----
    PhaseDto PhaseL1,
    PhaseDto PhaseL2,
    PhaseDto PhaseL3,

    // ---- Fluvius e-MUCS extras ----
    int? PowerFailuresCount,
    int? LongPowerFailuresCount,
    int BreakerState,
    decimal? LimiterThresholdKw,
    decimal? FuseThresholdA,
    IReadOnlyList<PowerFailureDto> PowerFailureLog,
    IReadOnlyList<MonthlyPeakDto> MaxDemandHistory,

    // ---- gas ----
    int? GasDeviceType,
    string? GasEquipmentId,
    bool? GasValveOpen,
    decimal? GasDeliveredM3,
    DateTimeOffset? GasDeliveredTimestamp,

    // ---- water ----
    int? WaterDeviceType,
    string? WaterEquipmentId,
    decimal? WaterDeliveredM3,
    DateTimeOffset? WaterDeliveredTimestamp,

    // ---- raw M-Bus channels ----
    IReadOnlyList<MBusDto> MBusDevices)
{
    public static DashboardSnapshot From(MeterReading r, DateTimeOffset timestamp)
    {
        var e = r.Electricity;
        var g = r.Gas;
        var w = r.Water;

        return new DashboardSnapshot(
            Timestamp: timestamp,

            Identification: r.Identification,
            DsmrVersion: r.DsmrVersion,
            MeterTimestamp: r.Timestamp,
            ElectricityEquipmentId: r.ElectricityEquipmentId,
            TextMessage: r.TextMessage,

            Tariff: (int)e.Tariff,
            EnergyDeliveredTariff1Kwh: e.EnergyDeliveredTariff1?.KilowattHours,
            EnergyDeliveredTariff2Kwh: e.EnergyDeliveredTariff2?.KilowattHours,
            EnergyReturnedTariff1Kwh: e.EnergyReturnedTariff1?.KilowattHours,
            EnergyReturnedTariff2Kwh: e.EnergyReturnedTariff2?.KilowattHours,
            EnergyDeliveredMaxRunningMonthKwh: e.EnergyDeliveredMaxRunningMonth?.KilowattHours,
            EnergyDeliveredMaxRunningMonthTimestamp: e.EnergyDeliveredMaxRunningMonthTimestamp,
            PowerDeliveredKw: e.PowerDelivered?.Kilowatts,
            PowerReturnedKw: e.PowerReturned?.Kilowatts,
            PowerDeliveredCurrentAvgKw: e.PowerDeliveredCurrentAvg?.Kilowatts,

            PhaseL1: PhaseDto.From(e.PhaseL1),
            PhaseL2: PhaseDto.From(e.PhaseL2),
            PhaseL3: PhaseDto.From(e.PhaseL3),

            PowerFailuresCount: e.PowerFailuresCount,
            LongPowerFailuresCount: e.LongPowerFailuresCount,
            BreakerState: (int)e.BreakerState,
            LimiterThresholdKw: e.LimiterThreshold?.Kilowatts,
            FuseThresholdA: e.FuseThreshold?.Amperes,
            PowerFailureLog: (e.PowerFailureLog ?? Array.Empty<PowerFailureEvent>())
                .Select(f => new PowerFailureDto(f.EndTime, (long)f.Duration.TotalSeconds))
                .ToList(),
            MaxDemandHistory: (e.MaxDemandHistory ?? Array.Empty<MonthlyPeakDemand>())
                .Select(m => new MonthlyPeakDto(m.PeriodStart, m.PeakOccurredAt, m.Peak.Kilowatts))
                .ToList(),

            GasDeviceType: g.DeviceType,
            GasEquipmentId: g.EquipmentId,
            GasValveOpen: g.IsValveOpen,
            GasDeliveredM3: g.Delivered?.CubicMeters,
            GasDeliveredTimestamp: g.DeliveredTimestamp,

            WaterDeviceType: w.DeviceType,
            WaterEquipmentId: w.EquipmentId,
            WaterDeliveredM3: w.Delivered?.CubicMeters,
            WaterDeliveredTimestamp: w.DeliveredTimestamp,

            MBusDevices: r.MBusDevices
                .Select(d => new MBusDto(d.Channel, d.DeviceType, d.EquipmentId, d.IsValveOpen, d.Value, d.Unit, d.CaptureTime))
                .ToList());
    }
}

public sealed record PhaseDto(
    decimal? PowerDeliveredKw,
    decimal? PowerReturnedKw,
    decimal? VoltageV,
    decimal? CurrentA,
    int? VoltageSags,
    int? VoltageSwells)
{
    public static PhaseDto From(ElectricityPhase p) => new(
        p.PowerDelivered?.Kilowatts,
        p.PowerReturned?.Kilowatts,
        p.Voltage?.Volts,
        p.Current?.Amperes,
        p.VoltageSags,
        p.VoltageSwells);
}

public sealed record PowerFailureDto(DateTimeOffset EndTime, long DurationSeconds);

public sealed record MonthlyPeakDto(DateTimeOffset PeriodStart, DateTimeOffset PeakOccurredAt, decimal PeakKw);

public sealed record MBusDto(
    int Channel,
    int? DeviceType,
    string? EquipmentId,
    bool? ValveOpen,
    decimal? Value,
    string? Unit,
    DateTimeOffset? CaptureTime);
