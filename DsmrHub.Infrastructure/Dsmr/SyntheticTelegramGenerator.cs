using System.Globalization;
using System.Text;
using DsmrHub.Infrastructure.Dsmr.Parsing;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Produces synthetic, continuously-varying Fluvius e-MUCS / DSMR P1 telegrams for the
/// <see cref="SimulatedMeterReadingSource"/>. Each call to <see cref="Next"/> advances the simulated
/// meter state and emits one complete telegram terminated by a freshly computed CRC16.
///
/// The telegram is a full three-phase meter and carries <b>every</b> value the rest of the system can
/// surface: identity + text message, all energy/return registers, live total + per-phase
/// power/voltage/current with sag/swell counters, the running-month and monthly max-demand history,
/// power-failure counts + event log, breaker/limiter/fuse info, and both gas and water M-Bus devices.
///
/// The body is assembled with explicit CRLF line endings and the checksum is recomputed every time,
/// so output parses identically on Windows and Linux (the cross-platform fix for the old example
/// replay, which rebuilt telegrams with <c>Environment.NewLine</c> and broke CRC validation on Linux).
/// </summary>
internal sealed class SyntheticTelegramGenerator
{
    private const string Header = @"/FLU5\253770234_A";

    // Free-text message (OBIS 0-0:96.13.0), ASCII-hex encoded as the meter would transmit it.
    private static readonly string TextMessageHex = ToHex("DsmrHub synthetic meter");

    private readonly TimeProvider _timeProvider;

    // Cumulative registers (kWh / m3) seeded with realistic starting values from a typical meter.
    private decimal _energyDeliveredT1 = 3457.591m;
    private decimal _energyDeliveredT2 = 4282.456m;
    private decimal _energyReturnedT1 = 447.891m;
    private decimal _energyReturnedT2 = 203.818m;
    private decimal _gas = 4761.307m;
    private decimal _water = 123.456m;

    // Cumulative per-phase voltage sag/swell counters and power-failure counters.
    private readonly int[] _sags = [2, 1, 0];
    private readonly int[] _swells = [1, 0, 0];
    private int _powerFailures = 5;
    private int _longPowerFailures = 3;

    private long _tick;

    public SyntheticTelegramGenerator(TimeProvider? timeProvider = null)
        => _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>Advances the simulated meter and returns the next complete, checksum-valid telegram.</summary>
    public string Next()
    {
        var tick = _tick++;
        var now = _timeProvider.GetLocalNow();
        var timestamp = Timestamp(now);

        // Active tariff alternates over time; only the active register accumulates delivered energy.
        var tariff = tick / 30 % 2 == 0 ? 1 : 2;
        if (tariff == 1)
        {
            _energyDeliveredT1 += 0.001m;
        }
        else
        {
            _energyDeliveredT2 += 0.001m;
        }

        // Occasional solar return, and periodic gas / water consumption.
        var returning = Math.Sin(tick / 20.0) > 0.6;
        if (returning)
        {
            _energyReturnedT1 += 0.001m;
        }

        if (tick % 15 == 0)
        {
            _gas += 0.001m;
        }

        if (tick % 20 == 0)
        {
            _water += 0.001m;
        }

        // Rare grid disturbances keep the cumulative counters moving.
        if (tick % 90 == 0 && tick > 0)
        {
            _sags[tick / 90 % 3]++;
            _powerFailures++;
        }

        if (tick % 240 == 0 && tick > 0)
        {
            _swells[tick / 240 % 3]++;
            _longPowerFailures++;
        }

        // Per-phase live values fluctuate smoothly around a baseline (deterministic, no RNG state).
        var voltage = new decimal[3];
        var delivered = new decimal[3];
        var returned = new decimal[3];
        var current = new decimal[3];
        for (var i = 0; i < 3; i++)
        {
            voltage[i] = Math.Round(230m + 2.0m * (decimal)Math.Sin((tick + i * 7) / 6.0), 1);
            delivered[i] = returning
                ? 0m
                : Clamp(Math.Round(0.20m + 0.15m * (decimal)Math.Sin((tick + i * 5) / 8.0) + 0.05m * (decimal)Math.Sin((tick + i) / 3.0), 3));
            returned[i] = returning
                ? Clamp(Math.Round(0.10m + 0.08m * (decimal)Math.Sin((tick + i * 3) / 5.0), 3))
                : 0m;
            var amps = (delivered[i] > 0m ? delivered[i] : returned[i]) / voltage[i] * 1000m;
            current[i] = Math.Round(amps, 2);
        }

        var totalDelivered = Math.Round(delivered[0] + delivered[1] + delivered[2], 3);
        var totalReturned = Math.Round(returned[0] + returned[1] + returned[2], 3);
        var currentAvg = Math.Round(0.03m + 0.01m * (decimal)Math.Abs(Math.Sin(tick / 10.0)), 3);

        var body = new StringBuilder();
        AppendLine(body, Header);
        AppendLine(body, string.Empty);

        // ---- identity ----
        AppendLine(body, "0-0:96.1.4(50217)");
        AppendLine(body, "0-0:96.1.1(3153414731313030303537373239)");
        AppendLine(body, $"0-0:1.0.0({timestamp})");

        // ---- energy registers ----
        AppendLine(body, $"1-0:1.8.1({Energy(_energyDeliveredT1)}*kWh)");
        AppendLine(body, $"1-0:1.8.2({Energy(_energyDeliveredT2)}*kWh)");
        AppendLine(body, $"1-0:2.8.1({Energy(_energyReturnedT1)}*kWh)");
        AppendLine(body, $"1-0:2.8.2({Energy(_energyReturnedT2)}*kWh)");
        AppendLine(body, $"0-0:96.14.0({tariff:0000})");
        AppendLine(body, $"1-0:1.4.0({Power(currentAvg)}*kW)");
        AppendLine(body, $"1-0:1.6.0({MonthStart(now)})(02.480*kW)");
        AppendLine(body, MaxDemandHistory(now));

        // ---- live power ----
        AppendLine(body, $"1-0:1.7.0({Power(totalDelivered)}*kW)");
        AppendLine(body, $"1-0:2.7.0({Power(totalReturned)}*kW)");

        // ---- per phase (L1/L2/L3): power delivered/returned, voltage, current, sags, swells ----
        AppendLine(body, $"1-0:21.7.0({Power(delivered[0])}*kW)");
        AppendLine(body, $"1-0:41.7.0({Power(delivered[1])}*kW)");
        AppendLine(body, $"1-0:61.7.0({Power(delivered[2])}*kW)");
        AppendLine(body, $"1-0:22.7.0({Power(returned[0])}*kW)");
        AppendLine(body, $"1-0:42.7.0({Power(returned[1])}*kW)");
        AppendLine(body, $"1-0:62.7.0({Power(returned[2])}*kW)");
        AppendLine(body, $"1-0:32.7.0({Voltage(voltage[0])}*V)");
        AppendLine(body, $"1-0:52.7.0({Voltage(voltage[1])}*V)");
        AppendLine(body, $"1-0:72.7.0({Voltage(voltage[2])}*V)");
        AppendLine(body, $"1-0:31.7.0({Current(current[0])}*A)");
        AppendLine(body, $"1-0:51.7.0({Current(current[1])}*A)");
        AppendLine(body, $"1-0:71.7.0({Current(current[2])}*A)");
        AppendLine(body, $"1-0:32.32.0({Count(_sags[0])})");
        AppendLine(body, $"1-0:52.32.0({Count(_sags[1])})");
        AppendLine(body, $"1-0:72.32.0({Count(_sags[2])})");
        AppendLine(body, $"1-0:32.36.0({Count(_swells[0])})");
        AppendLine(body, $"1-0:52.36.0({Count(_swells[1])})");
        AppendLine(body, $"1-0:72.36.0({Count(_swells[2])})");

        // ---- Fluvius e-MUCS extras ----
        AppendLine(body, $"0-0:96.7.21({Count(_powerFailures)})");
        AppendLine(body, $"0-0:96.7.9({Count(_longPowerFailures)})");
        AppendLine(body, PowerFailureLog());
        AppendLine(body, "0-0:96.3.10(1)");
        AppendLine(body, "0-0:17.0.0(999.9*kW)");
        AppendLine(body, "1-0:31.4.0(999*A)");
        AppendLine(body, $"0-0:96.13.0({TextMessageHex})");

        // ---- M-Bus channel 1: gas (device type 3) ----
        AppendLine(body, "0-1:24.1.0(003)");
        AppendLine(body, "0-1:96.1.1(37464C4F32313139303938343332)");
        AppendLine(body, "0-1:24.4.0(1)");
        AppendLine(body, $"0-1:24.2.3({timestamp})({Gas(_gas)}*m3)");

        // ---- M-Bus channel 2: water (device type 7) ----
        AppendLine(body, "0-2:24.1.0(007)");
        AppendLine(body, "0-2:96.1.1(37464C4F32313139303938343333)");
        AppendLine(body, $"0-2:24.2.3({timestamp})({Gas(_water)}*m3)");

        body.Append('!');

        // CRC16 is computed over every byte from the leading '/' up to and including the '!'.
        var crc = Crc16.Compute(Encoding.Latin1.GetBytes(body.ToString()));
        body.Append(crc.ToString("X4", CultureInfo.InvariantCulture)).Append("\r\n");
        return body.ToString();
    }

    // Two historical short-failure events: (count)(marker) then repeating (endTimestamp)(duration*s).
    private static string PowerFailureLog()
        => "1-0:99.97.0(2)(0-0:96.7.19)(231220114023W)(0000003540*s)(240105183012W)(0000000180*s)";

    // Three monthly peaks: (count)(marker)(marker) then repeating (periodStart)(peakAt)(peak*kW).
    private static string MaxDemandHistory(DateTimeOffset now)
    {
        var m1 = now.AddMonths(-3);
        var m2 = now.AddMonths(-2);
        var m3 = now.AddMonths(-1);
        return "0-0:98.1.0(3)(1-0:1.6.0)(1-0:1.6.0)"
            + $"({MonthStart(m1)})({Timestamp(m1)})(02.345*kW)"
            + $"({MonthStart(m2)})({Timestamp(m2)})(03.456*kW)"
            + $"({MonthStart(m3)})({Timestamp(m3)})(02.890*kW)";
    }

    private static decimal Clamp(decimal v) => v < 0m ? 0m : v;

    private static void AppendLine(StringBuilder sb, string line) => sb.Append(line).Append("\r\n");

    // Fluvius timestamps are local time with 'S' = summer (CEST, +02:00) / 'W' = winter (CET, +01:00).
    private static string Timestamp(DateTimeOffset now)
        => now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture)
           + (now.Offset >= TimeSpan.FromHours(2) ? 'S' : 'W');

    private static string MonthStart(DateTimeOffset now)
    {
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset);
        return Timestamp(start);
    }

    private static string ToHex(string text)
    {
        var bytes = Encoding.ASCII.GetBytes(text);
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }

    private static string Energy(decimal v) => v.ToString("000000.000", CultureInfo.InvariantCulture);
    private static string Power(decimal v) => v.ToString("00.000", CultureInfo.InvariantCulture);
    private static string Voltage(decimal v) => v.ToString("000.0", CultureInfo.InvariantCulture);
    private static string Current(decimal v) => v.ToString("000.00", CultureInfo.InvariantCulture);
    private static string Gas(decimal v) => v.ToString("00000.000", CultureInfo.InvariantCulture);
    private static string Count(int v) => v.ToString("00000000", CultureInfo.InvariantCulture);
}
