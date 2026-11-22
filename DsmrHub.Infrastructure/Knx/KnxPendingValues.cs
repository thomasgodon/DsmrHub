namespace DsmrHub.Infrastructure.Knx;

/// <summary>A value waiting to be written to the KNX bus.</summary>
internal sealed record KnxPendingValue(string Capability, byte[] Value, bool IsBit);

/// <summary>
/// Thread-safe "latest value wins" store between the meter readings and the KNX send loop.
/// Each capability holds at most one pending value: a newer value replaces an older one that has not
/// gone out yet, so outdated values are never sent and a slow bus cannot build up a backlog.
/// Values equal to the last one confirmed on the bus are not queued at all.
/// </summary>
internal sealed class KnxPendingValues
{
    private readonly object _lock = new();
    private readonly Dictionary<string, KnxPendingValue> _pending = new();
    private readonly Dictionary<string, KnxPendingValue> _lastSent = new();

    /// <summary>Queues a value. Returns false when it equals the last value sent (nothing to do).</summary>
    public bool Set(string capability, byte[] value, bool isBit)
    {
        lock (_lock)
        {
            if (_lastSent.TryGetValue(capability, out var sent) && sent.IsBit == isBit && sent.Value.AsSpan().SequenceEqual(value))
            {
                // Back to the value already on the bus: an older, different pending value is now outdated.
                _pending.Remove(capability);
                return false;
            }

            _pending[capability] = new KnxPendingValue(capability, value, isBit);
            return true;
        }
    }

    /// <summary>
    /// Removes and returns one pending value. Called right before each write, so a value updated while
    /// earlier writes were in flight is sent in its newest form.
    /// </summary>
    public bool TryTakeNext(out KnxPendingValue? value)
    {
        lock (_lock)
        {
            foreach (var entry in _pending)
            {
                _pending.Remove(entry.Key);
                value = entry.Value;
                return true;
            }

            value = null;
            return false;
        }
    }

    /// <summary>Records a value as being on the bus.</summary>
    public void MarkSent(KnxPendingValue value)
    {
        lock (_lock)
        {
            _lastSent[value.Capability] = value;
        }
    }

    /// <summary>Puts a value whose write failed back in the queue, unless a newer value was set meanwhile.</summary>
    public void Requeue(KnxPendingValue value)
    {
        lock (_lock)
        {
            _pending.TryAdd(value.Capability, value);
        }
    }

    /// <summary>The newest known value (pending, else last sent), used to answer KNX read requests.</summary>
    public KnxPendingValue? Latest(string capability)
    {
        lock (_lock)
        {
            if (_pending.TryGetValue(capability, out var pending)) return pending;
            return _lastSent.GetValueOrDefault(capability);
        }
    }

    public bool HasPending
    {
        get
        {
            lock (_lock)
            {
                return _pending.Count > 0;
            }
        }
    }
}
