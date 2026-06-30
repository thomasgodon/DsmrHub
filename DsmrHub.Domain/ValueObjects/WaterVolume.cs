using System.Globalization;

namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Volume of delivered water, expressed in cubic meters (m3).
/// </summary>
public readonly record struct WaterVolume
{
    public decimal CubicMeters { get; }

    private WaterVolume(decimal cubicMeters) => CubicMeters = cubicMeters;

    public static WaterVolume FromCubicMeters(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Water volume cannot be negative.");
        }

        return new WaterVolume(value);
    }

    public override string ToString() => CubicMeters.ToString(CultureInfo.InvariantCulture);
}
