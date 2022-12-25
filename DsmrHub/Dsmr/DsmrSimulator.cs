using System.Text;
using Microsoft.Extensions.Options;

namespace DsmrHub.Dsmr
{
    internal class DsmrSimulator : IDsmrSimulator
    {
        private readonly ILogger<DsmrSimulator> _logger;
        private readonly IDsmrProcessorService _dsmrProcessorService;
        private readonly DsmrOptions _dsmrOptions;

        public DsmrSimulator(ILogger<DsmrSimulator> logger, IDsmrProcessorService dsmrProcessorService, IOptions<DsmrOptions> dsmrOptions)
        {
            _logger = logger;
            _dsmrProcessorService = dsmrProcessorService;
            _dsmrOptions = dsmrOptions.Value;
        }
        public async Task Start(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Simulation starting...");

            await Task.Delay(1000, cancellationToken);
            var buffer = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var line in Properties.Resources.example.Split("\r\n"))
                {
                    buffer.AppendLine(line);

                    if (!line.StartsWith('!')) continue;

                    await _dsmrProcessorService.ProcessMessage(buffer.ToString(), cancellationToken);
                    buffer.Clear();
                    await Task.Delay(TimeSpan.FromSeconds(_dsmrOptions.SimulationRateInSeconds ?? 1), cancellationToken);
                }
            }
        }
    }
}
