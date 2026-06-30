namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Per-phase electrical measurements (L1, L2 or L3). Each measurement is optional
/// because not every meter reports every phase.
/// </summary>
public sealed record ElectricityPhase(
    PowerValue? PowerDelivered,
    PowerValue? PowerReturned,
    VoltageValue? Voltage,
    CurrentValue? Current,
    int? VoltageSags = null,
    int? VoltageSwells = null)
{
    public static readonly ElectricityPhase Empty = new(null, null, null, null);
}
