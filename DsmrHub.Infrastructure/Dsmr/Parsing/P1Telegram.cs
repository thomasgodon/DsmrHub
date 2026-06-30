using System.Text;

namespace DsmrHub.Infrastructure.Dsmr.Parsing;

/// <summary>
/// A tokenised P1 telegram: the identification header plus every data line keyed by its
/// OBIS code, with the raw contents of each parenthesised group preserved verbatim.
/// This layer is format-only and lossless; mapping OBIS codes to domain values happens in
/// <see cref="MeterReadingFactory"/>.
/// </summary>
internal sealed class P1Telegram
{
    private readonly Dictionary<string, IReadOnlyList<string>> _lines;

    private P1Telegram(string header, Dictionary<string, IReadOnlyList<string>> lines)
    {
        Header = header;
        _lines = lines;
    }

    /// <summary>The identification header line, e.g. <c>/FLU5\253770234_A</c>.</summary>
    public string Header { get; }

    /// <summary>Returns the parenthesised groups for an OBIS code, or an empty list if absent.</summary>
    public IReadOnlyList<string> Groups(string obis)
        => _lines.TryGetValue(obis, out var groups) ? groups : Array.Empty<string>();

    /// <summary>The single value of an OBIS code (first group), or null if the code is absent.</summary>
    public string? Value(string obis)
    {
        var groups = Groups(obis);
        return groups.Count > 0 ? groups[0] : null;
    }

    public bool Has(string obis) => _lines.ContainsKey(obis);

    /// <summary>
    /// Attempts to locate one complete telegram (<c>/</c> … <c>!CRC</c>) in <paramref name="raw"/>,
    /// validate its CRC16, and tokenise it. Returns false when no complete, checksum-valid
    /// telegram is present.
    /// </summary>
    public static bool TryRead(string? raw, out P1Telegram? telegram)
    {
        telegram = null;
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        var start = raw.IndexOf('/');
        if (start < 0)
        {
            return false;
        }

        var bang = raw.IndexOf('!', start);
        if (bang < 0)
        {
            return false;
        }

        if (!TryReadCrc(raw, bang, out var declaredCrc))
        {
            return false;
        }

        // CRC is computed over '/' … '!' inclusive.
        var checksummed = raw.AsSpan(start, bang - start + 1);
        var computedCrc = Crc16.Compute(Encoding.Latin1.GetBytes(checksummed.ToString()));
        if (computedCrc != declaredCrc)
        {
            return false;
        }

        var (header, lines) = Tokenise(raw, start, bang);
        if (header is null)
        {
            return false;
        }

        telegram = new P1Telegram(header, lines);
        return true;
    }

    private static bool TryReadCrc(string raw, int bang, out ushort crc)
    {
        crc = 0;

        // Exactly four hex characters follow '!'.
        if (bang + 4 >= raw.Length)
        {
            return false;
        }

        var hex = raw.AsSpan(bang + 1, 4);
        return ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out crc);
    }

    private static (string? Header, Dictionary<string, IReadOnlyList<string>> Lines) Tokenise(string raw, int start, int bang)
    {
        string? header = null;
        var lines = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        var body = raw.Substring(start, bang - start);
        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.Trim('\r', ' ', '\t');
            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] == '/')
            {
                header ??= line;
                continue;
            }

            var parenIndex = line.IndexOf('(');
            if (parenIndex < 0)
            {
                continue;
            }

            var obis = line[..parenIndex];
            lines[obis] = ExtractGroups(line, parenIndex);
        }

        return (header, lines);
    }

    private static IReadOnlyList<string> ExtractGroups(string line, int from)
    {
        var groups = new List<string>();

        for (var i = from; i < line.Length;)
        {
            if (line[i] != '(')
            {
                i++;
                continue;
            }

            var close = line.IndexOf(')', i + 1);
            if (close < 0)
            {
                break;
            }

            groups.Add(line.Substring(i + 1, close - i - 1));
            i = close + 1;
        }

        return groups;
    }
}
