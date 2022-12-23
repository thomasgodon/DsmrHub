namespace DsmrOpcUa.Mqtt;

internal interface IMqttBroker
{
    int Port { get; }

    Task StartAsync(CancellationToken cancellationToken);
}