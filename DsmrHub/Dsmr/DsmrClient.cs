using System.IO.Ports;
using Microsoft.Extensions.Options;

namespace DsmrHub.Dsmr
{
    internal class DsmrClient : IDsmrClient
    {
        private readonly ILogger<DsmrClient> _logger;
        private readonly SerialPort _serialPort;
        private readonly DsmrOptions _dsmrClientOptions;
        private readonly IDsmrProcessorService _dsmrProcessorService;

        public DsmrClient(ILogger<DsmrClient> logger, SerialPort serialPort, IDsmrProcessorService dsmrProcessorService, IOptions<DsmrOptions> dsmrClientOptions)
        {
            _logger = logger;
            _serialPort = serialPort;
            _dsmrClientOptions = dsmrClientOptions.Value;
            _dsmrProcessorService = dsmrProcessorService;

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
                    _logger.LogInformation($"connection to {_dsmrClientOptions.ComPort} initializing");
                    _serialPort.DataReceived += async (sender, args) =>
                    {
                        await ProcessReceivedData(_serialPort.ReadExisting(), cancellationToken);
                    };
                    _serialPort.Open();
                    await Task.Delay(Timeout.Infinite, cancellationToken);
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

        private async Task ProcessReceivedData(string receivedData, CancellationToken cancellationToken)
        {
            await _dsmrProcessorService.ProcessMessage(receivedData, cancellationToken);
            _logger.LogTrace(receivedData);
        }
    }
}
