using DsmrHub.Application.Abstractions;
using Microsoft.Extensions.Hosting;

namespace DsmrHub.Host;

/// <summary>
/// Background service that drives the configured meter-reading source for the application's lifetime.
/// The source (serial port or simulator) is selected during DI composition based on configuration.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly IMeterReadingSource _source;

    public Worker(IMeterReadingSource source)
    {
        _source = source;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _source.StartAsync(stoppingToken);
}
