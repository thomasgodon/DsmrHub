using System.Reflection;
using System.Text;
using DsmrHub.Application.Abstractions;
using DsmrHub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Replays an embedded example telegram stream, forwarding each complete telegram to the
/// ingestion use case at the configured rate. Ported from the original <c>DsmrSimulator</c>.
/// </summary>
internal sealed class SimulatedMeterReadingSource : IMeterReadingSource
{
    private const string ExampleResourceSuffix = "example.dsmr";

    private readonly ILogger<SimulatedMeterReadingSource> _logger;
    private readonly ITelegramIngestionService _ingestionService;
    private readonly DsmrOptions _options;
    private readonly string _example;

    public SimulatedMeterReadingSource(
        ILogger<SimulatedMeterReadingSource> logger,
        ITelegramIngestionService ingestionService,
        IOptions<DsmrOptions> options)
    {
        _logger = logger;
        _ingestionService = ingestionService;
        _options = options.Value;
        _example = LoadExampleTelegram();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Simulation starting...");

        await Task.Delay(1000, cancellationToken);
        var buffer = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var rawLine in _example.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                buffer.AppendLine(line);

                if (!line.StartsWith('!')) continue;

                await _ingestionService.IngestAsync(buffer.ToString(), cancellationToken);
                buffer.Clear();
                await Task.Delay(TimeSpan.FromSeconds(_options.SimulationRateInSeconds ?? 1), cancellationToken);
            }
        }
    }

    private static string LoadExampleTelegram()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ExampleResourceSuffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Embedded example telegram '{ExampleResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
