using System.IO.Ports;
using System.Text;
using DsmrIotDevice.Dsmr;
using DsmrOpcUa.Dsmr;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DsmrOpcUa
{
    public class Worker : BackgroundService
    {
        private readonly IDsmrClient _dsmrClient;
        private readonly IDsmrSimulator _dsmrSimulator;
        private readonly DsmrOptions _dsmrOptions;

        public Worker(IDsmrClient dsmrClient, IDsmrSimulator dsmrSimulator, IOptions<DsmrOptions> dsmrOptions)
        {
            _dsmrClient = dsmrClient;
            _dsmrSimulator = dsmrSimulator;
            _dsmrOptions = dsmrOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_dsmrOptions.UseExampleTelegram)
            {
                await _dsmrSimulator.Start(cancellationToken);
            }
            else
            {
                await _dsmrClient.Start(cancellationToken);
            }
        }
    }
}