namespace DsmrOpcUa.Mqtt;

internal interface IMqttBroker
{
    Task StartAsync(CancellationToken cancellationToken);
}