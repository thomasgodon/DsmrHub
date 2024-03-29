﻿using DSMRParser;
using DSMRParser.Models;

namespace DsmrHub.Dsmr;

internal class DsmrProcessorService : IDsmrProcessorService
{
    private readonly ILogger<DsmrProcessorService> _logger;
    private readonly DSMRTelegramParser _dsmrParser;
    private readonly IEnumerable<IDsmrProcessor> _dsmrProcessors;

    public DsmrProcessorService(ILogger<DsmrProcessorService> logger, IEnumerable<IDsmrProcessor> dsmrProcessors)
    {
        _dsmrParser = new DSMRTelegramParser();
        _logger = logger;
        _dsmrProcessors = dsmrProcessors;
    }

    public async Task ProcessMessage(string message, CancellationToken cancellationToken)
    {
        try
        {
            if (_dsmrParser.TryParse(message, out var telegram) is false)
            {
                return;
            }

            if (telegram.DSMRVersion == null)
            {
                return;
            }

            _logger.LogTrace(telegram.ToString());

            foreach (var dsmrProcessor in _dsmrProcessors)
            {
                await dsmrProcessor.ProcessTelegram(telegram, cancellationToken);
            }
        }
        catch (InvalidOBISIdException e)
        {
            _logger.LogWarning("{errorMessage} - {message}", e.Message, message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{error}", e.Message);
        }
    }
}