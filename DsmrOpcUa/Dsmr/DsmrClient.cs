using DsmrIotDevice.Dsmr;
using DsmrParser.Dsmr;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DsmrOpcUa.Dsmr
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

        public Task Start(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _serialPort.DataReceived += async (sender, args) =>
                    {
                        await ProcessReceivedData(_serialPort.ReadExisting(), cancellationToken);
                    };
                    _serialPort.Open();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }

                _logger.LogInformation($"{_dsmrClientOptions.ComPort} connection opened...");

                Console.ReadLine();
            }

            return Task.CompletedTask;
        }

        private async Task ProcessReceivedData(string receivedData, CancellationToken cancellationToken)
        {
            await _dsmrProcessorService.ProcessMessage(receivedData, cancellationToken);
            _logger.LogTrace(receivedData);
        }
    }
}
