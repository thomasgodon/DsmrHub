using System.Globalization;
using DsmrHub.Domain;
using DsmrHub.Domain.Events;
using DsmrHub.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;

namespace DsmrHub.Infrastructure.Mqtt;

/// <summary>
/// Publishes a meter reading to the embedded MQTT broker under the <c>dsmr/</c> topic tree.
/// Unlike the original processor (which reflected on non-existent property names and produced
/// empty payloads), this maps explicit topics to real domain values.
/// </summary>
internal sealed class MqttMeterReadingHandler : INotificationHandler<MeterReadingReceived>
{
    private readonly IMqttBroker _mqttBroker;
    private readonly IMqttClient _mqttClient;
    private readonly MqttOptions _mqttOptions;
    private readonly ILogger<MqttMeterReadingHandler> _logger;

    public MqttMeterReadingHandler(ILogger<MqttMeterReadingHandler> logger, IMqttBroker mqttBroker, IOptions<MqttOptions> mqttOptions)
    {
        _mqttBroker = mqttBroker;
        _mqttOptions = mqttOptions.Value;
        _logger = logger;
        _mqttClient = new MqttFactory().CreateMqttClient();
    }

    public async Task Handle(MeterReadingReceived notification, CancellationToken cancellationToken)
    {
        if (!_mqttOptions.Enabled) return;

        await _mqttBroker.StartAsync(cancellationToken);
        await ConnectToBroker(cancellationToken);

        foreach (var (topic, payload) in BuildMessages(notification.Reading))
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic($"dsmr/{topic}")
                .WithPayload(payload)
                .Build();

            await _mqttClient.PublishAsync(message, cancellationToken);
        }
    }

    private async Task ConnectToBroker(CancellationToken cancellationToken)
    {
        if (_mqttClient.IsConnected) return;

        var connectOptions = new MqttClientOptionsBuilder()
            .WithCredentials(_mqttOptions.Username, _mqttOptions.Password)
            .WithClientId(typeof(MqttMeterReadingHandler).Assembly.GetName().Name)
            .WithTcpServer("127.0.0.1", _mqttOptions.Port)
            .Build();

        await _mqttClient.ConnectAsync(connectOptions, cancellationToken);
        _logger.LogInformation("MQTT Client connected...");
    }

    private static IEnumerable<(string Topic, string Payload)> BuildMessages(MeterReading reading)
    {
        var e = reading.Electricity;
        var g = reading.Gas;

        yield return ("identification", reading.Identification ?? string.Empty);
        if (reading.DsmrVersion is { } version) yield return ("dsmr-version", version.ToString(CultureInfo.InvariantCulture));
        if (reading.Timestamp is { } ts) yield return ("timestamp", ts.ToString("O", CultureInfo.InvariantCulture));
        yield return ("electricity/equipment-id", reading.ElectricityEquipmentId ?? string.Empty);

        yield return ("electricity/tariff", ((int)e.Tariff).ToString(CultureInfo.InvariantCulture));
        foreach (var m in Energy("electricity/energy-delivered-tariff1", e.EnergyDeliveredTariff1)) yield return m;
        foreach (var m in Energy("electricity/energy-delivered-tariff2", e.EnergyDeliveredTariff2)) yield return m;
        foreach (var m in Energy("electricity/energy-returned-tariff1", e.EnergyReturnedTariff1)) yield return m;
        foreach (var m in Energy("electricity/energy-returned-tariff2", e.EnergyReturnedTariff2)) yield return m;
        foreach (var m in Energy("electricity/energy-delivered-max-running-month", e.EnergyDeliveredMaxRunningMonth)) yield return m;

        if (e.PowerDelivered is { } pd) yield return ("electricity/power-delivered", Format(pd.Kilowatts));
        if (e.PowerReturned is { } pr) yield return ("electricity/power-returned", Format(pr.Kilowatts));
        if (e.PowerDeliveredCurrentAvg is { } pavg) yield return ("electricity/power-delivered-current-avg", Format(pavg.Kilowatts));

        foreach (var m in Phase("electricity/l1", e.PhaseL1)) yield return m;
        foreach (var m in Phase("electricity/l2", e.PhaseL2)) yield return m;
        foreach (var m in Phase("electricity/l3", e.PhaseL3)) yield return m;

        if (g.Delivered is { } gas) yield return ("gas/delivered", Format(gas.CubicMeters));
        if (g.ValvePosition is { } valve) yield return ("gas/valve-position", valve.ToString(CultureInfo.InvariantCulture));
        if (g.EquipmentId is { } gasId) yield return ("gas/equipment-id", gasId);
    }

    private static IEnumerable<(string, string)> Energy(string topic, Domain.ValueObjects.EnergyValue? value)
    {
        if (value is { } v) yield return (topic, Format(v.KilowattHours));
    }

    private static IEnumerable<(string, string)> Phase(string prefix, Domain.ValueObjects.ElectricityPhase phase)
    {
        if (phase.PowerDelivered is { } pd) yield return ($"{prefix}/power-delivered", Format(pd.Kilowatts));
        if (phase.PowerReturned is { } pr) yield return ($"{prefix}/power-returned", Format(pr.Kilowatts));
        if (phase.Voltage is { } v) yield return ($"{prefix}/voltage", Format(v.Volts));
        if (phase.Current is { } c) yield return ($"{prefix}/current", Format(c.Amperes));
    }

    private static string Format(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
