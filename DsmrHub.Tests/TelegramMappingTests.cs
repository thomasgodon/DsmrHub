using DsmrHub.Domain.ValueObjects;
using DsmrHub.Infrastructure.Dsmr;
using DSMRParser;
using Xunit;

namespace DsmrHub.Tests;

public class TelegramMappingTests
{
    // A single complete telegram taken from the embedded example stream.
    private const string SampleTelegram =
        "/FLU5\\253770234_A\r\n" +
        "\r\n" +
        "0-0:96.1.4(50217)\r\n" +
        "0-0:96.1.1(3153414731313030303537373239)\r\n" +
        "0-0:1.0.0(221220204556W)\r\n" +
        "1-0:1.8.1(003457.591*kWh)\r\n" +
        "1-0:1.8.2(004282.456*kWh)\r\n" +
        "1-0:2.8.1(000447.891*kWh)\r\n" +
        "1-0:2.8.2(000203.818*kWh)\r\n" +
        "0-0:96.14.0(0001)\r\n" +
        "1-0:1.4.0(00.032*kW)\r\n" +
        "1-0:1.6.0(221203174500W)(03.716*kW)\r\n" +
        "0-0:98.1.0(0)(1-0:1.6.0)(1-0:1.6.0)()\r\n" +
        "1-0:1.7.0(00.524*kW)\r\n" +
        "1-0:2.7.0(00.000*kW)\r\n" +
        "1-0:21.7.0(00.524*kW)\r\n" +
        "1-0:22.7.0(00.000*kW)\r\n" +
        "1-0:32.7.0(231.6*V)\r\n" +
        "1-0:31.7.0(002.72*A)\r\n" +
        "0-0:96.3.10(1)\r\n" +
        "0-0:17.0.0(999.9*kW)\r\n" +
        "1-0:31.4.0(999*A)\r\n" +
        "0-0:96.13.0()\r\n" +
        "0-1:24.1.0(003)\r\n" +
        "0-1:96.1.1(37464C4F32313139303938343332)\r\n" +
        "0-1:24.4.0(1)\r\n" +
        "0-1:24.2.3(221220203958W)(04761.307*m3)\r\n" +
        "!FCCF\r\n";

    private static Domain.MeterReading Map()
    {
        // ignoreCrc: the focus is the mapping, not checksum verification.
        var parser = new DSMRTelegramParser();
        Assert.True(parser.TryParse(SampleTelegram, true, out var telegram));
        return telegram.ToMeterReading();
    }

    [Fact]
    public void Maps_EnergyRegisters()
    {
        var e = Map().Electricity;
        Assert.Equal(3457.591m, e.EnergyDeliveredTariff1!.Value.KilowattHours);
        Assert.Equal(4282.456m, e.EnergyDeliveredTariff2!.Value.KilowattHours);
        Assert.Equal(447.891m, e.EnergyReturnedTariff1!.Value.KilowattHours);
        Assert.Equal(203.818m, e.EnergyReturnedTariff2!.Value.KilowattHours);
    }

    [Fact]
    public void Maps_Tariff()
    {
        Assert.Equal(ElectricityTariff.Tariff1, Map().Electricity.Tariff);
    }

    [Fact]
    public void Maps_Power()
    {
        var e = Map().Electricity;
        Assert.Equal(0.524m, e.PowerDelivered!.Value.Kilowatts);
        Assert.Equal(0.000m, e.PowerReturned!.Value.Kilowatts);
    }

    [Fact]
    public void Maps_PhaseL1()
    {
        var l1 = Map().Electricity.PhaseL1;
        Assert.Equal(231.6m, l1.Voltage!.Value.Volts);
        Assert.Equal(2.72m, l1.Current!.Value.Amperes);
    }

    [Fact]
    public void Maps_Gas()
    {
        var g = Map().Gas;
        Assert.Equal(4761.307m, g.Delivered!.Value.CubicMeters);
        Assert.True(g.IsValveOpen);
    }
}
