using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using DsmrHub.Application.Abstractions;
using DsmrHub.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Reads raw DSMR telegrams from a serial port and forwards them to the ingestion use case.
/// Ported verbatim from the original <c>DsmrClient</c> to preserve hardware behavior
/// (event-based receive, buffering queue, receive-timeout reconnect loop).
/// </summary>
internal sealed class SerialMeterReadingSource : IMeterReadingSource
{
    private readonly ILogger<SerialMeterReadingSource> _logger;
    private readonly SerialPort _serialPort;
    private readonly DsmrOptions _options;
    private readonly ITelegramIngestionService _ingestionService;
    private readonly Queue<string> _queue = new();
    private readonly object _queueLock = new();
    private readonly Stopwatch _receiveTimeoutTimer = new();
    private readonly Stopwatch _lastReceivedTimeTimer = new();
    private DateTime? _lastReceivedTime;

    public SerialMeterReadingSource(
        ILogger<SerialMeterReadingSource> logger,
        ITelegramIngestionService ingestionService,
        IOptions<DsmrOptions> options)
    {
        _logger = logger;
        _ingestionService = ingestionService;
        _options = options.Value;

        _serialPort = new SerialPort
        {
            PortName = _options.ComPort,
            BaudRate = _options.BaudRate,
            StopBits = (StopBits)_options.StopBits,
            DataBits = _options.DataBits,
            Parity = (Parity)_options.Parity,
        };

        _lastReceivedTimeTimer.Start();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            try
            {
                _serialPort.DataReceived += (_, _) =>
                {
                    lock (_queueLock)
                    {
                        _queue.Enqueue(_serialPort.ReadExisting());
                    }
                };
                _serialPort.Open();
                _logger.LogInformation("Connected on {port}", _options.ComPort);
                await ProcessReceivedData(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{message}", e.Message);
            }

            if (cancellationToken.IsCancellationRequested) break;

            _logger.LogInformation("Reconnecting in 5 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private async Task ProcessReceivedData(CancellationToken cancellationToken)
    {
        var buffer = new StringBuilder();
        _receiveTimeoutTimer.Restart();
        while (cancellationToken.IsCancellationRequested is false && _serialPort.IsOpen)
        {
            await Task.Delay(100, cancellationToken);

            // print last received every minute
            if (_lastReceivedTimeTimer.Elapsed > TimeSpan.FromMinutes(1) && _lastReceivedTime is not null)
            {
                _logger.LogInformation("Last telegram received at: {time}", _lastReceivedTime);
                _lastReceivedTimeTimer.Restart();
            }

            // stop loop, disconnect to trigger reconnect
            if (_receiveTimeoutTimer.Elapsed > _options.ReceiveTimeout)
            {
                _logger.LogWarning("Nothing received after {timeout}. Closing connection...", _options.ReceiveTimeout);
                _serialPort.Close();
                break;
            }

            lock (_queueLock)
            {
                if (_queue.Count == 0)
                {
                    continue;
                }

                while (_queue.Count > 0)
                {
                    buffer.Append(_queue.Dequeue());
                }
            }

            _lastReceivedTime = DateTime.Now;
            _receiveTimeoutTimer.Restart();
            _logger.LogTrace("{buffer}", buffer.ToString());
            await _ingestionService.IngestAsync(buffer.ToString(), cancellationToken);
            buffer.Clear();
        }

        if (_serialPort.IsOpen is false)
        {
            _logger.LogInformation("Connection to {port} closed", _options.ComPort);
        }
    }
}
