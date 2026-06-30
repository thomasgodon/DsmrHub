using DsmrHub.Domain;

namespace DsmrHub.Infrastructure.IotHub;

internal interface IIotHubDevicesService
{
    Task Send(MeterReading reading, CancellationToken cancellationToken);
}
