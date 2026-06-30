using DsmrHub.Domain;
using DsmrHub.Infrastructure.Dsmr;
using Xunit;

namespace DsmrHub.Tests;

public class SyntheticTelegramGeneratorTests
{
    private static MeterReading Parse(string telegram)
    {
        var parser = new DsmrTelegramParser();
        // A false result here means the freshly computed CRC failed validation — exactly the
        // Windows-vs-Linux newline regression this generator is meant to prevent.
        Assert.True(parser.TryParse(telegram, out var reading));
        return reading!;
    }

    [Fact]
    public void Generated_telegram_passes_crc_and_parses()
    {
        var generator = new SyntheticTelegramGenerator();

        var reading = Parse(generator.Next());

        Assert.NotNull(reading.Electricity.EnergyDeliveredTariff1);
        Assert.NotNull(reading.Electricity.PowerDelivered);
    }

    [Fact]
    public void Simulates_every_available_value()
    {
        var reading = Parse(new SyntheticTelegramGenerator().Next());
        var e = reading.Electricity;

        // Identity + text message.
        Assert.NotNull(reading.Identification);
        Assert.NotNull(reading.DsmrVersion);
        Assert.NotNull(reading.Timestamp);
        Assert.NotNull(reading.ElectricityEquipmentId);
        Assert.False(string.IsNullOrEmpty(reading.TextMessage));

        // Energy registers + running-month peak.
        Assert.NotNull(e.EnergyDeliveredTariff1);
        Assert.NotNull(e.EnergyDeliveredTariff2);
        Assert.NotNull(e.EnergyReturnedTariff1);
        Assert.NotNull(e.EnergyReturnedTariff2);
        Assert.NotNull(e.EnergyDeliveredMaxRunningMonth);
        Assert.NotNull(e.EnergyDeliveredMaxRunningMonthTimestamp);
        Assert.NotNull(e.PowerDelivered);
        Assert.NotNull(e.PowerReturned);
        Assert.NotNull(e.PowerDeliveredCurrentAvg);

        // All three phases, including sag/swell counters.
        foreach (var phase in new[] { e.PhaseL1, e.PhaseL2, e.PhaseL3 })
        {
            Assert.NotNull(phase.PowerDelivered);
            Assert.NotNull(phase.PowerReturned);
            Assert.NotNull(phase.Voltage);
            Assert.NotNull(phase.Current);
            Assert.NotNull(phase.VoltageSags);
            Assert.NotNull(phase.VoltageSwells);
        }

        // Fluvius e-MUCS extras.
        Assert.NotNull(e.PowerFailuresCount);
        Assert.NotNull(e.LongPowerFailuresCount);
        Assert.NotNull(e.LimiterThreshold);
        Assert.NotNull(e.FuseThreshold);
        Assert.NotEmpty(e.PowerFailureLog!);
        Assert.NotEmpty(e.MaxDemandHistory!);

        // Gas and water M-Bus devices.
        Assert.NotNull(reading.Gas.Delivered);
        Assert.NotNull(reading.Gas.DeliveredTimestamp);
        Assert.NotNull(reading.Water.Delivered);
        Assert.NotNull(reading.Water.DeliveredTimestamp);
    }

    [Fact]
    public void Output_uses_crlf_line_endings_regardless_of_platform()
    {
        var telegram = new SyntheticTelegramGenerator().Next();

        Assert.Contains("\r\n", telegram);
        // Every '\n' must be preceded by a '\r' — no bare LF that would corrupt the CRC on Linux.
        for (var i = 0; i < telegram.Length; i++)
        {
            if (telegram[i] == '\n')
            {
                Assert.True(i > 0 && telegram[i - 1] == '\r', $"Bare LF at index {i}");
            }
        }
    }

    [Fact]
    public void Energy_registers_advance_between_successive_telegrams()
    {
        var generator = new SyntheticTelegramGenerator();

        var first = Parse(generator.Next()).Electricity.EnergyDeliveredTariff1!.Value.KilowattHours;
        var second = Parse(generator.Next()).Electricity.EnergyDeliveredTariff1!.Value.KilowattHours;

        Assert.True(second > first, $"Expected delivered energy to climb, got {first} then {second}");
    }
}
