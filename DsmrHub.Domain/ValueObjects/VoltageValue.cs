using System.Globalization;

namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Electrical potential, expressed in volts (V).
/// </summary>
public readonly record struct VoltageValue
{
    public decimal Volts { get; }

    private VoltageValue(decimal volts) => Volts = volts;

    public static VoltageValue FromVolts(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Voltage cannot be negative.");
        }

        return new VoltageValue(value);
    }

    public override string ToString() => Volts.ToString(CultureInfo.InvariantCulture);
}
