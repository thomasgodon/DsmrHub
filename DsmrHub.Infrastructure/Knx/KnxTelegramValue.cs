using Knx.Falcon;

namespace DsmrHub.Infrastructure.Knx;

internal sealed class KnxTelegramValue
{
    public KnxTelegramValue(GroupAddress address)
    {
        Address = address;
    }

    public GroupAddress Address { get; }
    public byte[]? Value { get; internal set; }

    /// <summary>
    /// True for 1-bit datapoints (DPT 1.xxx). These must go on the bus as a single bit packed into the
    /// APCI (KNX "DPTBinary"), not as a separate data byte, or receivers such as Home Assistant reject them.
    /// </summary>
    public bool IsBit { get; internal set; }

    /// <summary>
    /// Builds the bus payload. Multi-byte values are stored little-endian (BitConverter) and reversed
    /// to KNX big-endian; 1-bit values become a bit-sized <see cref="GroupValue"/>.
    /// </summary>
    public GroupValue? ToGroupValue()
    {
        if (Value is null) return null;
        return IsBit
            ? new GroupValue(Value[0] != 0)
            : new GroupValue(Value.Reverse().ToArray());
    }

    public override string ToString()
    {
        var value = Value is not null ? string.Join(",", Value.ToList()) : string.Empty;
        return $"{Address} - {value}";
    }
}
