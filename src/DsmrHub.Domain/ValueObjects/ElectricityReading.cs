namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Electricity-related measurements taken from a DSMR telegram.
/// </summary>
public sealed record ElectricityReading(
    ElectricityTariff Tariff,
    EnergyValue? EnergyDeliveredTariff1,
    EnergyValue? EnergyDeliveredTariff2,
    EnergyValue? EnergyReturnedTariff1,
    EnergyValue? EnergyReturnedTariff2,
    EnergyValue? EnergyDeliveredMaxRunningMonth,
    DateTimeOffset? EnergyDeliveredMaxRunningMonthTimestamp,
    PowerValue? PowerDelivered,
    PowerValue? PowerReturned,
    PowerValue? PowerDeliveredCurrentAvg,
    ElectricityPhase PhaseL1,
    ElectricityPhase PhaseL2,
    ElectricityPhase PhaseL3)
{
    public static readonly ElectricityReading Empty = new(
        ElectricityTariff.Unknown,
        null, null, null, null, null, null, null, null, null,
        ElectricityPhase.Empty, ElectricityPhase.Empty, ElectricityPhase.Empty);
}
