using DsmrHub.Infrastructure.Knx;
using Knx.Falcon;
using Xunit;

namespace DsmrHub.Tests;

public class KnxTelegramValueTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BitValue_IsSentAsSingleBit(bool value)
    {
        // DPT 1.xxx (tariff, gas valve) must be a 1-bit short value, not a data byte, or HA rejects it.
        var telegram = new KnxTelegramValue(GroupAddress.Parse("6/0/4"))
        {
            Value = BitConverter.GetBytes(value),
            IsBit = true
        };

        var groupValue = telegram.ToGroupValue()!;

        Assert.True(groupValue.IsShort);
        Assert.Equal(1, groupValue.SizeInBit);
        Assert.Equal(value, (bool)groupValue.TypedValue);
    }

    [Fact]
    public void FloatValue_IsSentAsFourBigEndianBytes()
    {
        var telegram = new KnxTelegramValue(GroupAddress.Parse("6/0/10"))
        {
            Value = BitConverter.GetBytes(4505f)
        };

        var groupValue = telegram.ToGroupValue()!;

        Assert.False(groupValue.IsShort);
        Assert.Equal(BitConverter.GetBytes(4505f).Reverse().ToArray(), groupValue.Value);
    }
}
