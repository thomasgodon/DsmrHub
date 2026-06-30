using System.Globalization;

namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// Electrical current, expressed in amperes (A).
/// </summary>
public readonly record struct CurrentValue
{
    public decimal Amperes { get; }

    private CurrentValue(decimal amperes) => Amperes = amperes;

    public static CurrentValue FromAmperes(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Current cannot be negative.");
        }

        return new CurrentValue(value);
    }

    public override string ToString() => Amperes.ToString(CultureInfo.InvariantCulture);
}
