namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Gas-related measurements taken from a DSMR telegram.
/// </summary>
public sealed record GasReading(
    int? DeviceType,
    string? EquipmentId,
    int? ValvePosition,
    GasVolume? Delivered,
    DateTimeOffset? DeliveredTimestamp)
{
    public static readonly GasReading Empty = new(null, null, null, null, null);

    /// <summary>True when the meter reports the gas valve as open (position 1).</summary>
    public bool? IsValveOpen => ValvePosition is null ? null : ValvePosition == 1;
}
