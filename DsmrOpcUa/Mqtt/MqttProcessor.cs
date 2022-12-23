using DsmrOpcUa.Dsmr;
using DsmrOpcUa.Mqtt.Extensions;
using DsmrParser.Models;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace DsmrOpcUa.Mqtt;

internal class MqttProcessor : IDsmrProcessor
{
    private readonly IMqttBroker _mqttBroker;
    private readonly MqttClient _mqttClient;
    private readonly MqttOptions _mqttOptions;

    public MqttProcessor(IMqttBroker mqttBroker, IOptions<MqttOptions> mqttOptions)
    {
        _mqttBroker = mqttBroker;
        _mqttOptions = mqttOptions.Value;
    }

    async Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
    {
        await StartBroker(cancellationToken);
        //await ConnectToBroker(cancellationToken);
        //await SendTelegram(telegram, cancellationToken);
    }

    private async Task StartBroker(CancellationToken cancellationToken)
    {
        await _mqttBroker.StartAsync(cancellationToken);
    }

    private async Task ConnectToBroker(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected) return;

        var connectOptions = new MqttClientOptionsBuilder()
            .WithCredentials("username", "password")
            .Build();
        await _mqttClient.ConnectAsync(connectOptions, cancellationToken);
    }

    private async Task SendTelegram(Telegram telegram, CancellationToken cancellationToken)
    {
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("PowerConsumptionTariff1"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("PowerConsumptionTariff2"), cancellationToken);
    }
}