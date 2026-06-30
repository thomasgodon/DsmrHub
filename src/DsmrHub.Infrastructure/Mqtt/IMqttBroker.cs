namespace DsmrHub.Infrastructure.Mqtt;

internal interface IMqttBroker
{
    Task StartAsync(CancellationToken cancellationToken);
}
