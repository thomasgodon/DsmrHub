using DsmrHub.Dsmr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSMRParser.Models;

namespace DsmrHub.Knx
{
    internal class KnxProcessor : IDsmrProcessor
    {
        public Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (_knxOptions.Enabled is false) return;

            // connect to the KNXnet/IP gateway
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
