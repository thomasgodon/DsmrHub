using System.Globalization;

namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Instantaneous electrical power, expressed in kilowatts (kW).
/// </summary>
public readonly record struct PowerValue
{
    public decimal Kilowatts { get; }

    private PowerValue(decimal kilowatts) => Kilowatts = kilowatts;

    public static PowerValue FromKilowatts(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Power cannot be negative.");
        }

        return new PowerValue(value);
    }

    public decimal Watts => Kilowatts * 1000m;

    public override string ToString() => Kilowatts.ToString(CultureInfo.InvariantCulture);
}
