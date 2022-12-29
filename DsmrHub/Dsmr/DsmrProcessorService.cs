using System.Text;
using DSMRParser;
using DSMRParser.Models;

namespace DsmrHub.Dsmr;

internal class DsmrProcessorService : IDsmrProcessorService
{
    private readonly ILogger<DsmrProcessorService> _logger;
    private readonly StringBuilder _buffer = new();
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
            if (!_dsmrParser.TryParse(message, out Telegram? telegram))
            {
                return;
            }

            _logger.LogTrace(telegram?.ToString());

            foreach (var dsmrProcessor in _dsmrProcessors)
            {
                await dsmrProcessor.ProcessTelegram(telegram, cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }
}