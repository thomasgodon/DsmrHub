using DsmrHub.Dsmr;
using DSMRParser.Models;

namespace DsmrHub.OpcUaServer
{
    internal class OpcUaServer : IDsmrProcessor
    {
        private readonly ILogger<OpcUaServer> _logger;

        public OpcUaServer(ILogger<OpcUaServer> logger)
        {
            _logger = logger;
        }

        Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
