using System.Globalization;

namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Volume of delivered gas, expressed in cubic meters (m3).
/// </summary>
public readonly record struct GasVolume
{
    public decimal CubicMeters { get; }

    private GasVolume(decimal cubicMeters) => CubicMeters = cubicMeters;

    public static GasVolume FromCubicMeters(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Gas volume cannot be negative.");
        }

        return new GasVolume(value);
    }

    public override string ToString() => CubicMeters.ToString(CultureInfo.InvariantCulture);
}
