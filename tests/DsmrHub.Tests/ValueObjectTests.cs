using DsmrHub.Domain.ValueObjects;
using Xunit;

namespace DsmrHub.Tests;

public class ValueObjectTests
{
    [Fact]
    public void EnergyValue_StoresKilowattHours()
    {
        var value = EnergyValue.FromKilowattHours(3457.591m);
        Assert.Equal(3457.591m, value.KilowattHours);
    }

    [Fact]
    public void EnergyValue_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EnergyValue.FromKilowattHours(-1m));
    }

    [Fact]
    public void PowerValue_ConvertsKilowattsToWatts()
    {
        var value = PowerValue.FromKilowatts(0.524m);
        Assert.Equal(524m, value.Watts);
    }

    [Fact]
    public void PowerValue_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PowerValue.FromKilowatts(-0.1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(231.6)]
    public void VoltageValue_AcceptsNonNegative(double volts)
    {
        var value = VoltageValue.FromVolts((decimal)volts);
        Assert.Equal((decimal)volts, value.Volts);
    }

    [Fact]
    public void GasVolume_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GasVolume.FromCubicMeters(-1m));
    }

    [Fact]
    public void CurrentValue_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CurrentValue.FromAmperes(-1m));
    }
}
