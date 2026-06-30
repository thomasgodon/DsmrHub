using DsmrHub.Application.Abstractions;
using DsmrHub.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DsmrHub.Application.Telegrams;

/// <summary>
/// Default <see cref="ITelegramIngestionService"/>: parses a raw telegram and, on success,
/// publishes a <see cref="MeterReadingReceived"/> event so every registered sink can react.
/// Replaces the old <c>DsmrProcessorService</c> fan-out loop.
/// </summary>
internal sealed class TelegramIngestionService : ITelegramIngestionService
{
    private readonly ILogger<TelegramIngestionService> _logger;
    private readonly ITelegramParser _parser;
    private readonly IPublisher _publisher;

    public TelegramIngestionService(
        ILogger<TelegramIngestionService> logger,
        ITelegramParser parser,
        IPublisher publisher)
    {
        _logger = logger;
        _parser = parser;
        _publisher = publisher;
    }

    public async Task IngestAsync(string rawTelegram, CancellationToken cancellationToken)
    {
        try
        {
            if (!_parser.TryParse(rawTelegram, out var reading) || reading is null)
            {
                return;
            }

            await _publisher.Publish(new MeterReadingReceived(reading), cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{error}", e.Message);
        }
    }
}
