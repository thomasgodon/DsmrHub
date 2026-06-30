namespace DsmrHub.Application.Abstractions;

/// <summary>
/// Application use case: ingest a single raw telegram, parse it, and publish the
/// resulting <see cref="Domain.Events.MeterReadingReceived"/> domain event.
/// </summary>
public interface ITelegramIngestionService
{
    Task IngestAsync(string rawTelegram, CancellationToken cancellationToken);
}
