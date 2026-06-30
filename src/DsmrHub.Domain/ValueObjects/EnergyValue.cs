using System.Globalization;

namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Amount of electrical energy, expressed in kilowatt-hours (kWh).
/// </summary>
public readonly record struct EnergyValue
{
    public decimal KilowattHours { get; }

    private EnergyValue(decimal kilowattHours) => KilowattHours = kilowattHours;

    public static EnergyValue FromKilowattHours(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Energy cannot be negative.");
        }

        return new EnergyValue(value);
    }

    public override string ToString() => KilowattHours.ToString(CultureInfo.InvariantCulture);
}
