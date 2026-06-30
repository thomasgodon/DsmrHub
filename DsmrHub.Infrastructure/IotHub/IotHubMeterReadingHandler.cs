using DsmrHub.Domain.Events;
using MediatR;

namespace DsmrHub.Infrastructure.IotHub;

internal sealed class IotHubMeterReadingHandler : INotificationHandler<MeterReadingReceived>
{
    private readonly IIotHubDevicesService _iotHubDevicesService;

    public IotHubMeterReadingHandler(IIotHubDevicesService iotHubDevicesService)
    {
        _iotHubDevicesService = iotHubDevicesService;
    }

    public Task Handle(MeterReadingReceived notification, CancellationToken cancellationToken)
        => _iotHubDevicesService.Send(notification.Reading, cancellationToken);
}
