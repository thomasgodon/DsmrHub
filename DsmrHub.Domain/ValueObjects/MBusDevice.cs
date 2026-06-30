namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// A raw, device-type-agnostic view of a single M-Bus channel (1-4) on the P1 telegram.
/// Gas (<see cref="GasReading"/>) and water (<see cref="WaterReading"/>) are typed
/// conveniences derived from these; this list preserves <em>every</em> M-Bus channel
/// (including heat/thermal in GJ or unrecognised device types) so nothing is lost.
/// </summary>
public sealed record MBusDevice(
    int Channel,
    int? DeviceType,
    string? EquipmentId,
    int? ValvePosition,
    decimal? Value,
    string? Unit,
    DateTimeOffset? CaptureTime)
{
    /// <summary>True when the device reports its valve/breaker as open (position 1).</summary>
    public bool? IsValveOpen => ValvePosition is null ? null : ValvePosition == 1;
}
