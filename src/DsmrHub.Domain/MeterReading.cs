using DsmrHub.Domain.ValueObjects;

namespace DsmrHub.Domain;

/// <summary>
/// Aggregate root representing a single, parsed DSMR meter reading (one telegram).
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
        string? electricityEquipmentId = null)
    {
        Identification = identification;
        DsmrVersion = dsmrVersion;
        Timestamp = timestamp;
        Electricity = electricity ?? throw new ArgumentNullException(nameof(electricity));
        Gas = gas ?? throw new ArgumentNullException(nameof(gas));
        ElectricityEquipmentId = electricityEquipmentId;
    }

    /// <summary>Meter identification header (e.g. "/KFM5KAIFA-METER").</summary>
    public string? Identification { get; }

    /// <summary>DSMR protocol version reported by the meter.</summary>
    public int? DsmrVersion { get; }

    /// <summary>Timestamp of the telegram, as reported by the meter.</summary>
    public DateTimeOffset? Timestamp { get; }

    /// <summary>Serial number of the electricity meter.</summary>
    public string? ElectricityEquipmentId { get; }

    public ElectricityReading Electricity { get; }

    public GasReading Gas { get; }
}
