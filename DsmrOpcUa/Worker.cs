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
        private readonly IEnumerable<IDsmrProcessor> _dsmrProcessors;

        public Worker(IDsmrClient dsmrClient, IDsmrSimulator dsmrSimulator, IOptions<DsmrOptions> dsmrOptions, IEnumerable<IDsmrProcessor> dsmrProcessors)
        {
            _dsmrClient = dsmrClient;
            _dsmrSimulator = dsmrSimulator;
            _dsmrOptions = dsmrOptions.Value;
            _dsmrProcessors = dsmrProcessors;
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