using DsmrHub.Domain;
using DsmrHub.Domain.ValueObjects;
using Xunit;

namespace DsmrHub.Tests;

public class MeterReadingTests
{
    [Fact]
    public void Constructor_RejectsNullElectricity()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MeterReading(null, 5, null, null!, GasReading.Empty));
    }

    [Fact]
    public void Constructor_RejectsNullGas()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MeterReading(null, 5, null, ElectricityReading.Empty, null!));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(null, null)]
    public void GasReading_IsValveOpen_ReflectsPosition(int? position, bool? expected)
    {
        var gas = new GasReading(null, null, position, null, null);
        Assert.Equal(expected, gas.IsValveOpen);
    }
}
