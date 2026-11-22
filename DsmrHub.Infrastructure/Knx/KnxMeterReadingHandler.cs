using DsmrHub.Domain.Events;
using DsmrHub.Domain.ValueObjects;
using MediatR;

namespace DsmrHub.Infrastructure.Knx;

/// <summary>
/// Maps a meter reading onto KNX capability values and hands them to <see cref="KnxBusPublisher"/>,
/// which writes them on its own background loop. This handler never touches the bus, so a slow or
/// stuck KNX tunnel cannot delay the serial ingestion or the other sinks.
/// Ported from the original <c>KnxProcessor</c>: the capability keys, byte encodings and scale
/// factors (kW->W, kWh) are preserved so existing <c>GroupAddressMapping</c> configuration keeps
/// working. The running-month peak is a power and is sent as a W float under the
/// <c>PowerDeliveredMaxRunningMonth</c> key.
/// </summary>
internal sealed class KnxMeterReadingHandler : INotificationHandler<MeterReadingReceived>
{
    // Capability keys must match the keys used in KnxOptions.GroupAddressMapping (appsettings.json).
    private const string ElectricityTariffKey = "ElectricityTariff";
    private const string EnergyDeliveredTariff1Key = "EnergyDeliveredTariff1";
    private const string EnergyDeliveredTariff2Key = "EnergyDeliveredTariff2";
    private const string EnergyReturnedTariff1Key = "EnergyReturnedTariff1";
    private const string EnergyReturnedTariff2Key = "EnergyReturnedTariff2";
    private const string PowerDeliveredKey = "PowerDelivered";
    private const string PowerDeliveredL1Key = "PowerDeliveredL1";
    private const string PowerDeliveredL2Key = "PowerDeliveredL2";
    private const string PowerDeliveredL3Key = "PowerDeliveredL3";
    private const string PowerDeliveredCurrentAvgKey = "PowerDeliveredCurrentAvg";
    private const string PowerReturnedKey = "PowerReturned";
    private const string PowerReturnedL1Key = "PowerReturnedL1";
    private const string PowerReturnedL2Key = "PowerReturnedL2";
    private const string PowerReturnedL3Key = "PowerReturnedL3";
    private const string CurrentL1Key = "CurrentL1";
    private const string CurrentL2Key = "CurrentL2";
    private const string CurrentL3Key = "CurrentL3";
    private const string VoltageL1Key = "VoltageL1";
    private const string VoltageL2Key = "VoltageL2";
    private const string VoltageL3Key = "VoltageL3";
    private const string GasDeliveredKey = "GasDelivered";
    private const string GasValvePositionKey = "GasValvePosition";
    private const string PowerDeliveredMaxRunningMonthKey = "PowerDeliveredMaxRunningMonth";

    private readonly KnxBusPublisher _publisher;

    public KnxMeterReadingHandler(KnxBusPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task Handle(MeterReadingReceived notification, CancellationToken cancellationToken)
    {
        var e = notification.Reading.Electricity;
        var g = notification.Reading.Gas;

        // Electricity tariff - 1.024
        if (e.Tariff != ElectricityTariff.Unknown) Bit(ElectricityTariffKey, e.Tariff == ElectricityTariff.Tariff1);

        // Energy delivered - 14.*
        Float(EnergyDeliveredTariff1Key, e.EnergyDeliveredTariff1?.KilowattHours);
        Float(EnergyDeliveredTariff2Key, e.EnergyDeliveredTariff2?.KilowattHours);

        // Energy returned - 14.*
        Float(EnergyReturnedTariff1Key, e.EnergyReturnedTariff1?.KilowattHours);
        Float(EnergyReturnedTariff2Key, e.EnergyReturnedTariff2?.KilowattHours);

        // Power delivered - 14.056 (W)
        Float(PowerDeliveredKey, e.PowerDelivered?.Watts);
        Float(PowerDeliveredL1Key, e.PhaseL1.PowerDelivered?.Watts);
        Float(PowerDeliveredL2Key, e.PhaseL2.PowerDelivered?.Watts);
        Float(PowerDeliveredL3Key, e.PhaseL3.PowerDelivered?.Watts);
        Float(PowerDeliveredCurrentAvgKey, e.PowerDeliveredCurrentAvg?.Watts);

        // Power returned - 14.056 (W)
        Float(PowerReturnedKey, e.PowerReturned?.Watts);
        Float(PowerReturnedL1Key, e.PhaseL1.PowerReturned?.Watts);
        Float(PowerReturnedL2Key, e.PhaseL2.PowerReturned?.Watts);
        Float(PowerReturnedL3Key, e.PhaseL3.PowerReturned?.Watts);

        // Current amperage - 14.019
        Float(CurrentL1Key, e.PhaseL1.Current?.Amperes);
        Float(CurrentL2Key, e.PhaseL2.Current?.Amperes);
        Float(CurrentL3Key, e.PhaseL3.Current?.Amperes);

        // Current voltage - 14.027
        Float(VoltageL1Key, e.PhaseL1.Voltage?.Volts);
        Float(VoltageL2Key, e.PhaseL2.Voltage?.Volts);
        Float(VoltageL3Key, e.PhaseL3.Voltage?.Volts);

        // Gas - 14.076
        Float(GasDeliveredKey, g.Delivered?.CubicMeters);

        // Gas valve position - 1.001
        if (g.ValvePosition is not null) Bit(GasValvePositionKey, g.ValvePosition == 1);

        // Power delivered max running month - 14.056 (W, like the other power values)
        Float(PowerDeliveredMaxRunningMonthKey, e.PowerDeliveredMaxRunningMonth?.Watts);

        return Task.CompletedTask;
    }

    private void Float(string capability, decimal? value)
    {
        if (value is null) return;
        _publisher.Enqueue(capability, BitConverter.GetBytes((float)value.Value), isBit: false);
    }

    // 1-bit datapoint (DPT 1.xxx), sent as DPTBinary.
    private void Bit(string capability, bool value)
        => _publisher.Enqueue(capability, BitConverter.GetBytes(value), isBit: true);
}
