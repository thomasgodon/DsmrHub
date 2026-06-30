using DsmrHub.Application.Abstractions;
using DsmrHub.Domain;
using DsmrHub.Infrastructure.Dsmr.Parsing;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// In-house Fluvius e-MUCS / DSMR P1 telegram parser. Locates one complete telegram in the
/// input, validates its CRC16 checksum, tokenises every OBIS line and maps the result onto the
/// domain's <see cref="MeterReading"/>. No external parsing library is used.
/// </summary>
internal sealed class DsmrTelegramParser : ITelegramParser
{
    public bool TryParse(string rawTelegram, out MeterReading? reading)
    {
        reading = null;

        if (!P1Telegram.TryRead(rawTelegram, out var telegram) || telegram is null)
        {
            return false;
        }

        reading = MeterReadingFactory.ToMeterReading(telegram);
        return true;
    }
}
