using DsmrHub.Application.Abstractions;
using DsmrHub.Domain;
using DSMRParser;
using DSMRParser.Models;
using Microsoft.Extensions.Logging;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Infrastructure adapter that parses raw DSMR telegrams with <see cref="DSMRTelegramParser"/>
/// and maps the result onto the domain's <see cref="MeterReading"/>.
/// </summary>
internal sealed class DsmrTelegramParser : ITelegramParser
{
    private readonly ILogger<DsmrTelegramParser> _logger;
    private readonly DSMRTelegramParser _parser = new();

    public DsmrTelegramParser(ILogger<DsmrTelegramParser> logger)
    {
        _logger = logger;
    }

    public bool TryParse(string rawTelegram, out MeterReading? reading)
    {
        reading = null;

        try
        {
            if (!_parser.TryParse(rawTelegram, out var telegram) || telegram is null)
            {
                return false;
            }

            // A telegram without a DSMR version is incomplete / noise.
            if (telegram.DSMRVersion is null)
            {
                return false;
            }

            _logger.LogTrace("{telegram}", telegram.ToString());

            reading = telegram.ToMeterReading();
            return true;
        }
        catch (InvalidOBISIdException e)
        {
            _logger.LogWarning("{errorMessage} - {message}", e.Message, rawTelegram);
            return false;
        }
    }
}
