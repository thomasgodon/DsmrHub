using DsmrHub.Application.Abstractions;
using DsmrHub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Drives a <see cref="SyntheticTelegramGenerator"/>, forwarding one freshly generated, checksum-valid
/// telegram to the ingestion use case at the configured rate. Replaces the real serial source when
/// <see cref="DsmrOptions.UseSimulator"/> is enabled, so every sink and the dashboard receive live,
/// continuously-changing readings without any meter hardware.
/// </summary>
internal sealed class SimulatedMeterReadingSource : IMeterReadingSource
{
    private readonly ILogger<SimulatedMeterReadingSource> _logger;
    private readonly ITelegramIngestionService _ingestionService;
    private readonly DsmrOptions _options;
    private readonly SyntheticTelegramGenerator _generator = new();

    public SimulatedMeterReadingSource(
        ILogger<SimulatedMeterReadingSource> logger,
        ITelegramIngestionService ingestionService,
        IOptions<DsmrOptions> options)
    {
        _logger = logger;
        _ingestionService = ingestionService;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulation starting...");

        var interval = TimeSpan.FromSeconds(_options.SimulationRateInSeconds ?? 1);

        while (!cancellationToken.IsCancellationRequested)
        {
            await _ingestionService.IngestAsync(_generator.Next(), cancellationToken);
            await Task.Delay(interval, cancellationToken);
        }
    }
}
