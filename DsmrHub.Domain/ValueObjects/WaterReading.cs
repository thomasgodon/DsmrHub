namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Water-related measurements taken from an M-Bus device on a DSMR/e-MUCS telegram
/// (M-Bus device type 007).
/// </summary>
public sealed record WaterReading(
    int? DeviceType,
    string? EquipmentId,
    WaterVolume? Delivered,
    DateTimeOffset? DeliveredTimestamp)
{
    public static readonly WaterReading Empty = new(null, null, null, null);
}
