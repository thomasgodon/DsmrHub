using System.Text;
using DsmrHub.Domain;
using DsmrHub.Domain.ValueObjects;
using DsmrHub.Infrastructure.Dsmr;
using DsmrHub.Infrastructure.Dsmr.Parsing;
using Xunit;

namespace DsmrHub.Tests;

public class TelegramMappingTests
{
    // A single complete telegram taken from the embedded example stream (CRC !FCCF is genuine).
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

    private static MeterReading Parse(string telegram)
    {
        var parser = new DsmrTelegramParser();
        Assert.True(parser.TryParse(telegram, out var reading));
        return reading!;
    }

    private static MeterReading Map() => Parse(SampleTelegram);

    /// <summary>Appends a genuine CRC16 to a telegram body that ends with '!'.</summary>
    private static string WithCrc(string telegramEndingWithBang)
    {
        var crc = Crc16.Compute(Encoding.Latin1.GetBytes(telegramEndingWithBang));
        return telegramEndingWithBang + crc.ToString("X4") + "\r\n";
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
        Assert.Equal(0.032m, e.PowerDeliveredCurrentAvg!.Value.Kilowatts);
    }

    [Fact]
    public void Maps_PhaseL1()
    {
        var l1 = Map().Electricity.PhaseL1;
        Assert.Equal(231.6m, l1.Voltage!.Value.Volts);
        Assert.Equal(2.72m, l1.Current!.Value.Amperes);
        Assert.Equal(0.524m, l1.PowerDelivered!.Value.Kilowatts);
    }

    [Fact]
    public void Maps_Gas()
    {
        var g = Map().Gas;
        Assert.Equal(4761.307m, g.Delivered!.Value.CubicMeters);
        Assert.True(g.IsValveOpen);
        Assert.Equal("7FLO2119098432", g.EquipmentId);
    }

    [Fact]
    public void Maps_Meta()
    {
        var r = Map();
        Assert.Equal(50217, r.DsmrVersion);
        Assert.Equal("1SAG1100057729", r.ElectricityEquipmentId);
        Assert.Equal("/FLU5\\253770234_A", r.Identification);
        Assert.Equal(new DateTimeOffset(2022, 12, 20, 20, 45, 56, TimeSpan.FromHours(1)), r.Timestamp);
    }

    [Fact]
    public void Maps_BreakerLimiterAndFuse()
    {
        var e = Map().Electricity;
        Assert.Equal(ElectricityBreakerState.Connected, e.BreakerState);
        Assert.Equal(999.9m, e.LimiterThreshold!.Value.Kilowatts);
        Assert.Equal(999m, e.FuseThreshold!.Value.Amperes);
    }

    [Fact]
    public void Maps_RunningMonthPeak()
    {
        var e = Map().Electricity;
        // 1-0:1.6.0 surfaces the running-month peak value with its capture timestamp.
        Assert.Equal(3.716m, e.EnergyDeliveredMaxRunningMonth!.Value.KilowattHours);
        Assert.Equal(new DateTimeOffset(2022, 12, 3, 17, 45, 0, TimeSpan.FromHours(1)), e.EnergyDeliveredMaxRunningMonthTimestamp);
    }

    [Fact]
    public void EmptyMaxDemandHistory_IsEmptyNotNull()
    {
        // 0-0:98.1.0(0)... is present with a zero count.
        Assert.Empty(Map().Electricity.MaxDemandHistory!);
    }

    [Fact]
    public void RejectsTelegram_WithBadCrc()
    {
        var tampered = SampleTelegram.Replace("!FCCF", "!0000");
        var parser = new DsmrTelegramParser();
        Assert.False(parser.TryParse(tampered, out var reading));
        Assert.Null(reading);
    }

    [Fact]
    public void RejectsTelegram_WithMutatedPayload()
    {
        // Change a value but keep the old CRC: must fail the checksum.
        var mutated = SampleTelegram.Replace("1-0:1.8.1(003457.591*kWh)", "1-0:1.8.1(009999.999*kWh)");
        var parser = new DsmrTelegramParser();
        Assert.False(parser.TryParse(mutated, out _));
    }

    [Fact]
    public void Parses_PowerFailureLog()
    {
        var telegram = WithCrc(
            "/FLU5\\1_A\r\n\r\n" +
            "0-0:96.1.4(50223)\r\n" +
            "0-0:96.7.21(00007)\r\n" +
            "0-0:96.7.9(00003)\r\n" +
            "1-0:99.97.0(2)(0-0:96.7.19)(101208152415W)(0000000240*s)(101208151004W)(0000000301*s)\r\n" +
            "!");

        var e = Parse(telegram).Electricity;
        Assert.Equal(7, e.PowerFailuresCount);
        Assert.Equal(3, e.LongPowerFailuresCount);
        Assert.Equal(2, e.PowerFailureLog!.Count);
        Assert.Equal(TimeSpan.FromSeconds(240), e.PowerFailureLog[0].Duration);
        Assert.Equal(TimeSpan.FromSeconds(301), e.PowerFailureLog[1].Duration);
        Assert.Equal(new DateTimeOffset(2010, 12, 8, 15, 24, 15, TimeSpan.FromHours(1)), e.PowerFailureLog[0].EndTime);
    }

    [Fact]
    public void Parses_MaxDemandHistory()
    {
        var telegram = WithCrc(
            "/FLU5\\1_A\r\n\r\n" +
            "0-0:96.1.4(50223)\r\n" +
            "0-0:98.1.0(2)(1-0:1.6.0)(1-0:1.6.0)(200501000000S)(200423192538S)(03.695*kW)(200401000000S)(200305122139S)(05.980*kW)\r\n" +
            "!");

        var history = Parse(telegram).Electricity.MaxDemandHistory!;
        Assert.Equal(2, history.Count);
        Assert.Equal(3.695m, history[0].Peak.Kilowatts);
        Assert.Equal(5.980m, history[1].Peak.Kilowatts);
        Assert.Equal(new DateTimeOffset(2020, 4, 23, 19, 25, 38, TimeSpan.FromHours(2)), history[0].PeakOccurredAt);
    }

    [Fact]
    public void Parses_WaterChannel()
    {
        var telegram = WithCrc(
            "/FLU5\\1_A\r\n\r\n" +
            "0-0:96.1.4(50223)\r\n" +
            "0-2:24.1.0(007)\r\n" +
            "0-2:96.1.1(37464C4F32313139303938343332)\r\n" +
            "0-2:24.2.3(221220203958W)(00012.345*m3)\r\n" +
            "!");

        var reading = Parse(telegram);
        Assert.Equal(12.345m, reading.Water.Delivered!.Value.CubicMeters);
        Assert.Equal("7FLO2119098432", reading.Water.EquipmentId);
        Assert.Equal(7, reading.Water.DeviceType);
        Assert.Contains(reading.MBusDevices, d => d.Channel == 2 && d.DeviceType == 7);
    }

    [Fact]
    public void Parses_VoltageSagsAndSwells()
    {
        var telegram = WithCrc(
            "/FLU5\\1_A\r\n\r\n" +
            "0-0:96.1.4(50223)\r\n" +
            "1-0:32.32.0(00002)\r\n" +
            "1-0:32.36.0(00001)\r\n" +
            "!");

        var l1 = Parse(telegram).Electricity.PhaseL1;
        Assert.Equal(2, l1.VoltageSags);
        Assert.Equal(1, l1.VoltageSwells);
    }
}
