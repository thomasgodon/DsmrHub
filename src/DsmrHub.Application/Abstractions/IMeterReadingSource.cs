namespace DsmrHub.Application.Abstractions;

/// <summary>
/// Port representing a source of raw DSMR telegrams (e.g. a serial port or a simulator).
/// The host drives the selected source for the lifetime of the application.
/// </summary>
public interface IMeterReadingSource
{
    /// <summary>Starts reading telegrams until cancellation is requested.</summary>
    Task StartAsync(CancellationToken cancellationToken);
}
