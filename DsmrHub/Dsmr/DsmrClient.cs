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
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("connection to {port} initializing",  _dsmrClientOptions.ComPort);
                    _serialPort.DataReceived += (_, _) =>
                    {
                        lock (_queueLock)
                        {
                            _queue.Enqueue(_serialPort.ReadExisting());
                        }
                    };
                    _serialPort.Open();
                    await ProcessReceivedData(cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }

                if (cancellationToken.IsCancellationRequested) continue;

                _logger.LogInformation("Retry in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }

        private async Task ProcessReceivedData(CancellationToken cancellationToken)
        {
            var buffer = new StringBuilder();
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);

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

                await _dsmrProcessorService.ProcessMessage(buffer.ToString(), cancellationToken);
                _logger.LogTrace(buffer.ToString());
                buffer.Clear();
            }
        }
    }
}
