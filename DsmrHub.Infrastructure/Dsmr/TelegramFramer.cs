using System.Text;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Reassembles complete P1 telegrams (<c>/</c> … <c>!CRC</c>) from the arbitrary chunks a serial
/// port delivers. A telegram regularly spans more than one read, so chunks are buffered until the
/// closing <c>!</c> and its four CRC characters have arrived; only then is the telegram released.
/// </summary>
internal sealed class TelegramFramer
{
    // A Fluvius telegram is ~1-2 kB. Anything far beyond that without a terminator is line noise.
    private const int MaxBufferLength = 16 * 1024;
    private const int CrcLength = 4;

    private readonly StringBuilder _buffer = new();

    /// <summary>
    /// Adds a chunk of received data and returns every telegram it completed, in arrival order.
    /// Returns an empty list while the current telegram is still incomplete.
    /// </summary>
    public IReadOnlyList<string> Append(string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
        {
            return Array.Empty<string>();
        }

        _buffer.Append(chunk);

        List<string>? telegrams = null;
        while (true)
        {
            var pending = _buffer.ToString();

            var start = pending.IndexOf('/');
            if (start < 0)
            {
                // No telegram start: whatever is buffered is the tail of one we joined halfway.
                _buffer.Clear();
                break;
            }

            var bang = pending.IndexOf('!', start);
            var end = bang + 1 + CrcLength;
            if (bang < 0 || end > pending.Length)
            {
                // Still waiting for the terminator or its CRC; drop anything before the start.
                _buffer.Remove(0, start);
                break;
            }

            (telegrams ??= []).Add(pending[start..end]);
            _buffer.Remove(0, end);
        }

        if (_buffer.Length > MaxBufferLength)
        {
            _buffer.Clear();
        }

        return telegrams ?? (IReadOnlyList<string>)Array.Empty<string>();
    }

    /// <summary>Discards any partially received telegram, e.g. after a reconnect.</summary>
    public void Reset() => _buffer.Clear();
}
