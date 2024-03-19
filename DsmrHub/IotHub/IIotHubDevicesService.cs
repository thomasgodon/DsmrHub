using DSMRParser.Models;

namespace DsmrHub.IotHub
{
    internal interface IIotHubDevicesService
    {
        Task Send(Telegram telegram, CancellationToken cancellationToken);
    }
}
