using DsmrHub.Dsmr;
using DSMRParser.Models;

namespace DsmrHub.IotHub
{
    internal class IotHubProcessor : IDsmrProcessor
    {
        private readonly IIotHubDevicesService _iotHubDevicesService;

        public IotHubProcessor(IIotHubDevicesService iotHubDevicesService)
        {
            _iotHubDevicesService = iotHubDevicesService;
        }

        public async Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            await _iotHubDevicesService.Send(telegram, cancellationToken);
        }
    }
}
