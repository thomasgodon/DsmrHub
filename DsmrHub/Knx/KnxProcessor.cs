using DsmrHub.Dsmr;
using DSMRParser.Models;
using Knx;
using Knx.KnxNetIp;
using Microsoft.Extensions.Options;
using System.Net;

namespace DsmrHub.Knx
{
    internal class KnxProcessor : IDsmrProcessor
    {
        private readonly KnxOptions _knxOptions;
        private readonly KnxNetIpTunnelingClient _client;
        private readonly ILogger<KnxProcessor> _logger;

        public KnxProcessor(ILogger<KnxProcessor> logger, IOptions<KnxOptions> knxOptions)
        {
            _logger = logger;
            _knxOptions = knxOptions.Value;
            var knxClientEndpoint = new IPEndPoint(IPAddress.Parse(_knxOptions.Host), _knxOptions.Port);
            var knxDeviceAddress = KnxAddress.ParseDevice(_knxOptions.KnxDeviceAddress);
            _client = new KnxNetIpTunnelingClient(knxClientEndpoint, knxDeviceAddress);
        }

        public async Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (_knxOptions.Enabled is false) return;

            if (_client.IsConnected is false)
            {
                try
                {
                    await _client.Connect();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Couldn't connect to '{address}'", _client.RemoteEndPoint.Address);
                    await _client.Disconnect();
                }
            }
        }
    }
}
