using DsmrOpcUa.Dsmr;
using DsmrOpcUa.Mqtt.Extensions;
using DsmrParser.Models;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace DsmrOpcUa.Mqtt;

internal class MqttProcessor : IDsmrProcessor
{
    private readonly IMqttBroker _mqttBroker;
    private readonly IMqttClient _mqttClient;
    private readonly MqttOptions _mqttOptions;
    private readonly ILogger<MqttProcessor> _logger;

    public MqttProcessor(ILogger<MqttProcessor> logger, IMqttBroker mqttBroker, IOptions<MqttOptions> mqttOptions)
    {
        _mqttBroker = mqttBroker;
        _mqttOptions = mqttOptions.Value;
        _logger = logger;

        _mqttClient = new MqttFactory().CreateMqttClient();
    }

    async Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
    {
        await StartBroker(cancellationToken);
        await ConnectToBroker(cancellationToken);
        await SendTelegram(telegram, cancellationToken);
    }

    private async Task StartBroker(CancellationToken cancellationToken)
    {
        await _mqttBroker.StartAsync(cancellationToken);
    }

    private async Task ConnectToBroker(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected) return;

        var connectOptions = new MqttClientOptionsBuilder()
            .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
            .WithClientId(typeof(Program).Assembly.GetName().Name)
            .WithTcpServer("127.0.0.1", _mqttOptions.Port)
            .Build();
        
        await _mqttClient.ConnectAsync(connectOptions, cancellationToken);
        _logger.LogInformation("MQTT Client connected...");
    }

    private async Task SendTelegram(Telegram telegram, CancellationToken cancellationToken)
    {
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("PowerConsumptionTariff1"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("PowerConsumptionTariff2"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("GasUsage"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("CurrentTariff"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("InstantaneousCurrent"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("InstantaneousElectricityDelivery"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("InstantaneousElectricityUsage"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("SerialNumberGasMeter"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("SerialNumberElectricityMeter"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("MessageHeader"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("MessageVersion"), cancellationToken);
        await _mqttClient.PublishAsync(telegram.ToApplicationMessage("Timestamp"), cancellationToken);
    }
}