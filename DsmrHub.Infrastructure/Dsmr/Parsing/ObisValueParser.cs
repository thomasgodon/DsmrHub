using System.Globalization;

namespace DsmrHub.Infrastructure.Dsmr.Parsing;

/// <summary>
/// Low-level converters for the value formats found inside a P1 telegram's parenthesised groups:
/// decimal-with-unit (e.g. <c>003457.591*kWh</c>), integer counts, <c>YYMMDDhhmmssX</c> timestamps,
/// second durations and ASCII-hex encoded strings.
/// </summary>
internal static class ObisValueParser
{
    // Fluvius timestamps are Belgian local time: 'W' = winter (CET, +01:00), 'S' = summer (CEST, +02:00).
    private static readonly TimeSpan WinterOffset = TimeSpan.FromHours(1);
    private static readonly TimeSpan SummerOffset = TimeSpan.FromHours(2);

    /// <summary>Parses a "value*unit" group's numeric part (the unit is ignored).</summary>
    public static decimal? Decimal(string? group)
    {
        var number = NumericPart(group);
        return number is not null && decimal.TryParse(number, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    /// <summary>Parses a "value*unit" group into its numeric part and unit (unit null when absent).</summary>
    public static (decimal Value, string? Unit)? DecimalWithUnit(string? group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return null;
        }

        var star = group.IndexOf('*');
        var numberPart = star < 0 ? group : group[..star];
        var unit = star < 0 ? null : group[(star + 1)..];

        return decimal.TryParse(numberPart, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? (value, unit)
            : null;
    }

    /// <summary>Parses an integer count, tolerating leading zeros and an optional "*unit" suffix.</summary>
    public static int? Int(string? group)
    {
        var number = NumericPart(group);
        return number is not null && int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    /// <summary>Parses a "*s" duration group into a <see cref="TimeSpan"/>.</summary>
    public static TimeSpan? Duration(string? group)
    {
        var seconds = Int(group);
        return seconds is null ? null : TimeSpan.FromSeconds(seconds.Value);
    }

    /// <summary>Parses a YYMMDDhhmmssX timestamp (X = 'S' summer / 'W' winter) into a DateTimeOffset.</summary>
    public static DateTimeOffset? Timestamp(string? group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return null;
        }

        var digits = group;
        var offset = WinterOffset;

        var last = group[^1];
        if (last is 'S' or 'W' or 's' or 'w')
        {
            offset = last is 'S' or 's' ? SummerOffset : WinterOffset;
            digits = group[..^1];
        }

        if (digits.Length != 12 || !long.TryParse(digits, out _))
        {
            return null;
        }

        var year = 2000 + int.Parse(digits.AsSpan(0, 2), CultureInfo.InvariantCulture);
        var month = int.Parse(digits.AsSpan(2, 2), CultureInfo.InvariantCulture);
        var day = int.Parse(digits.AsSpan(4, 2), CultureInfo.InvariantCulture);
        var hour = int.Parse(digits.AsSpan(6, 2), CultureInfo.InvariantCulture);
        var minute = int.Parse(digits.AsSpan(8, 2), CultureInfo.InvariantCulture);
        var second = int.Parse(digits.AsSpan(10, 2), CultureInfo.InvariantCulture);

        try
        {
            return new DateTimeOffset(year, month, day, hour, minute, second, offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Decodes an ASCII-hex encoded group (e.g. "3153..." → "1SAG...") into text.</summary>
    public static string? HexToAscii(string? group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return null;
        }

        if (group.Length % 2 != 0)
        {
            return group; // not valid hex; return as-is rather than losing data
        }

        var chars = new char[group.Length / 2];
        for (var i = 0; i < chars.Length; i++)
        {
            if (!byte.TryParse(group.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                return group; // not valid hex; return as-is
            }

            chars[i] = (char)b;
        }

        return new string(chars);
    }

    private static string? NumericPart(string? group)
    {
        if (string.IsNullOrEmpty(group))
        {
            return null;
        }

        var star = group.IndexOf('*');
        return star < 0 ? group : group[..star];
    }
}
