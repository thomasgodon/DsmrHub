using DsmrHub.Domain.ValueObjects;

namespace DsmrHub.Domain;

/// <summary>
/// Aggregate root representing a single, parsed DSMR / Fluvius e-MUCS meter reading (one telegram).
/// This is the domain's own model, decoupled from any parsing library.
/// </summary>
public sealed class MeterReading
{
    public MeterReading(
        string? identification,
        int? dsmrVersion,
        DateTimeOffset? timestamp,
        ElectricityReading electricity,
        GasReading gas,
        string? electricityEquipmentId = null,
        string? textMessage = null,
        WaterReading? water = null,
        IReadOnlyList<MBusDevice>? mBusDevices = null)
    {
        Identification = identification;
        DsmrVersion = dsmrVersion;
        Timestamp = timestamp;
        Electricity = electricity ?? throw new ArgumentNullException(nameof(electricity));
        Gas = gas ?? throw new ArgumentNullException(nameof(gas));
        ElectricityEquipmentId = electricityEquipmentId;
        TextMessage = textMessage;
        Water = water ?? WaterReading.Empty;
        MBusDevices = mBusDevices ?? Array.Empty<MBusDevice>();
    }

    /// <summary>Meter identification header (e.g. "/FLU5\253770234_A").</summary>
    public string? Identification { get; }

    /// <summary>DSMR / e-MUCS protocol version reported by the meter (OBIS 0-0:96.1.4).</summary>
    public int? DsmrVersion { get; }

    /// <summary>Timestamp of the telegram, as reported by the meter.</summary>
    public DateTimeOffset? Timestamp { get; }

    /// <summary>Serial number of the electricity meter (OBIS 0-0:96.1.1), decoded to text.</summary>
    public string? ElectricityEquipmentId { get; }

    /// <summary>Free-text message from the meter (OBIS 0-0:96.13.0), decoded to text.</summary>
    public string? TextMessage { get; }

    public ElectricityReading Electricity { get; }

    public GasReading Gas { get; }

    /// <summary>Water measurements, when a water meter is present on an M-Bus channel.</summary>
    public WaterReading Water { get; }

    /// <summary>Every M-Bus channel reported on the telegram, device-type-agnostic.</summary>
    public IReadOnlyList<MBusDevice> MBusDevices { get; }
}
