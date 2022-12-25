namespace DsmrHub.Mqtt;

internal interface IMqttBroker
{
    Task StartAsync(CancellationToken cancellationToken);
}