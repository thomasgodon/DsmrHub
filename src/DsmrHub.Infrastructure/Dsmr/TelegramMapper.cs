using DsmrHub.Domain;
using DsmrHub.Domain.ValueObjects;
using DSMRParser.Models;

namespace DsmrHub.Infrastructure.Dsmr;

/// <summary>
/// Maps the parsing library's <see cref="Telegram"/> onto the domain's <see cref="MeterReading"/>.
/// This is the single place where the external DSMR model is unwrapped (handling the nullable
/// <c>UnitValue</c>/<c>TimeStampedValue</c> nesting), keeping the rest of the codebase library-agnostic.
/// </summary>
internal static class TelegramMapper
{
    public static MeterReading ToMeterReading(this Telegram telegram)
    {
        var electricity = new ElectricityReading(
            Tariff: MapTariff(telegram.ElectricityTariff),
            EnergyDeliveredTariff1: Energy(telegram.EnergyDeliveredTariff1),
            EnergyDeliveredTariff2: Energy(telegram.EnergyDeliveredTariff2),
            EnergyReturnedTariff1: Energy(telegram.EnergyReturnedTariff1),
            EnergyReturnedTariff2: Energy(telegram.EnergyReturnedTariff2),
            EnergyDeliveredMaxRunningMonth: Energy(telegram.EnergyDeliveredMaxRunningMonth?.Value),
            EnergyDeliveredMaxRunningMonthTimestamp: telegram.EnergyDeliveredMaxRunningMonth?.DateTime,
            PowerDelivered: Power(telegram.PowerDelivered),
            PowerReturned: Power(telegram.PowerReturned),
            PowerDeliveredCurrentAvg: Power(telegram.PowerDeliveredCurrentAvg),
            PhaseL1: new ElectricityPhase(Power(telegram.PowerDeliveredL1), Power(telegram.PowerReturnedL1), Voltage(telegram.VoltageL1), Current(telegram.CurrentL1)),
            PhaseL2: new ElectricityPhase(Power(telegram.PowerDeliveredL2), Power(telegram.PowerReturnedL2), Voltage(telegram.VoltageL2), Current(telegram.CurrentL2)),
            PhaseL3: new ElectricityPhase(Power(telegram.PowerDeliveredL3), Power(telegram.PowerReturnedL3), Voltage(telegram.VoltageL3), Current(telegram.CurrentL3)));

        var gas = new GasReading(
            DeviceType: telegram.GasDeviceType,
            EquipmentId: telegram.GasEquipmentId,
            ValvePosition: telegram.GasValvePosition,
            Delivered: Gas(telegram.GasDelivered?.Value),
            DeliveredTimestamp: telegram.GasDelivered?.DateTime);

        return new MeterReading(
            identification: telegram.Identification,
            dsmrVersion: telegram.DSMRVersion,
            timestamp: telegram.TimeStamp,
            electricity: electricity,
            gas: gas,
            electricityEquipmentId: telegram.EquipmentId);
    }

    private static ElectricityTariff MapTariff(int? tariff) => tariff switch
    {
        1 => ElectricityTariff.Tariff1,
        2 => ElectricityTariff.Tariff2,
        _ => ElectricityTariff.Unknown,
    };

    private static EnergyValue? Energy(UnitValue<decimal>? value)
        => value is null ? null : EnergyValue.FromKilowattHours(value.Value);

    private static PowerValue? Power(UnitValue<decimal>? value)
        => value is null ? null : PowerValue.FromKilowatts(value.Value);

    private static VoltageValue? Voltage(UnitValue<decimal>? value)
        => value is null ? null : VoltageValue.FromVolts(value.Value);

    private static CurrentValue? Current(UnitValue<decimal>? value)
        => value is null ? null : CurrentValue.FromAmperes(value.Value);

    private static GasVolume? Gas(UnitValue<decimal>? value)
        => value is null ? null : GasVolume.FromCubicMeters(value.Value);
}
