using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Options;

namespace DsmrHub.Dsmr
{
    internal class DsmrClient : IDsmrClient
    {
        private readonly ILogger<DsmrClient> _logger;
        private readonly SerialPort _serialPort;
        private readonly DsmrOptions _dsmrClientOptions;
        private readonly IDsmrProcessorService _dsmrProcessorService;
        private readonly Queue<string> _queue;
        private readonly object _queueLock = new();
        private readonly Stopwatch _receiveTimeout = new();

        public DsmrClient(ILogger<DsmrClient> logger, SerialPort serialPort, IDsmrProcessorService dsmrProcessorService, IOptions<DsmrOptions> dsmrClientOptions)
        {
            _logger = logger;
            _serialPort = serialPort;
            _dsmrClientOptions = dsmrClientOptions.Value;
            _dsmrProcessorService = dsmrProcessorService;
            _queue = new Queue<string>();

            _serialPort.PortName = _dsmrClientOptions.ComPort;
            _serialPort.BaudRate = _dsmrClientOptions.BaudRate;
            _serialPort.StopBits = (StopBits)_dsmrClientOptions.StopBits;
            _serialPort.DataBits = _dsmrClientOptions.DataBits;
            _serialPort.Parity = (Parity)_dsmrClientOptions.Parity;
        }

        public async Task Start(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested is false)
            {
                try
                {
                    _logger.LogInformation("Connection to {port} initializing",  _dsmrClientOptions.ComPort);
                    _serialPort.DataReceived += (_, _) =>
                    {
                        lock (_queueLock)
                        {
                            _queue.Enqueue(_serialPort.ReadExisting());
                        }
                    };
                    _serialPort.Open();
                    _logger.LogInformation("Connection to {port} initialized", _dsmrClientOptions.ComPort);
                    await ProcessReceivedData(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{message}", e.Message);
                }

                if (cancellationToken.IsCancellationRequested) break;

                _logger.LogInformation("Reconnect in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        private async Task ProcessReceivedData(CancellationToken cancellationToken)
        {
            var buffer = new StringBuilder();
            _receiveTimeout.Start();
            while (cancellationToken.IsCancellationRequested is false && _serialPort.IsOpen)
            {
                await Task.Delay(100, cancellationToken);

                // stop loop, disconnect to trigger reconnect
                if (_receiveTimeout.Elapsed > _dsmrClientOptions.ReceiveTimeout)
                {
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

                _logger.LogTrace(buffer.ToString());
                try
                {
                    await _dsmrProcessorService.ProcessMessage(buffer.ToString(), cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "{message}", e.Message);
                }
                buffer.Clear();
            }

            if (_serialPort.IsOpen is false)
            {
                _logger.LogInformation("Connection to {port} closed", _dsmrClientOptions.ComPort);
            }
        }
    }
}
