using MediatR;

namespace DsmrHub.Domain.Events;

/// <summary>
/// Domain event raised whenever a new meter reading has been parsed from the meter.
/// Dispatched to all registered sinks (MQTT, KNX, IoT Hub, UDP).
/// </summary>
public sealed record MeterReadingReceived(MeterReading Reading) : INotification;
