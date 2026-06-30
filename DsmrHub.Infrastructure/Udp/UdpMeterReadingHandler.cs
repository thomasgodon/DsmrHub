using System.Globalization;
using System.Text;
using DsmrHub.Domain;
using DsmrHub.Domain.Events;
using DsmrHub.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.Options;

namespace DsmrHub.Infrastructure.Udp;

/// <summary>
/// Broadcasts individual meter values to fixed UDP ports (10000-10033). The port assignments are
/// preserved from the original processor; values now come from the domain model instead of reflection.
/// Every port is sent on each reading (empty payload when the value is absent), matching the original.
/// </summary>
internal sealed class UdpMeterReadingHandler : INotificationHandler<MeterReadingReceived>
{
    private readonly UdpOptions _udpOptions;

    public UdpMeterReadingHandler(IOptions<UdpOptions> udpOptions)
    {
        _udpOptions = udpOptions.Value;
    }

    public async Task Handle(MeterReadingReceived notification, CancellationToken cancellationToken)
    {
        if (!_udpOptions.Enabled) return;

        foreach (var (port, payload) in BuildPackets(notification.Reading))
        {
            await Encoding.UTF8.GetBytes(payload).SendToAsync(_udpOptions.Host, port, cancellationToken);
        }
    }

    private static IEnumerable<(int Port, string Payload)> BuildPackets(MeterReading reading)
    {
        var e = reading.Electricity;
        var g = reading.Gas;

        yield return (10000, reading.Identification ?? string.Empty);
        yield return (10001, reading.DsmrVersion?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        yield return (10002, reading.ElectricityEquipmentId ?? string.Empty);
        yield return (10003, reading.Timestamp?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty);

        yield return (10010, Decimal(e.EnergyDeliveredTariff1?.KilowattHours));
        yield return (10011, Decimal(e.EnergyDeliveredTariff2?.KilowattHours));
        yield return (10012, Decimal(e.EnergyReturnedTariff1?.KilowattHours));
        yield return (10013, Decimal(e.EnergyReturnedTariff2?.KilowattHours));
        yield return (10014, ((int)e.Tariff).ToString(CultureInfo.InvariantCulture));
        yield return (10015, Decimal(e.PowerDelivered?.Kilowatts));
        yield return (10016, Decimal(e.PowerReturned?.Kilowatts));
        yield return (10017, Decimal(e.PhaseL1.PowerDelivered?.Kilowatts));
        yield return (10018, Decimal(e.PhaseL1.PowerReturned?.Kilowatts));
        yield return (10019, Decimal(e.PhaseL1.Voltage?.Volts));
        yield return (10020, Decimal(e.PhaseL1.Current?.Amperes));

        yield return (10030, g.DeviceType?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        yield return (10031, g.EquipmentId ?? string.Empty);
        yield return (10032, g.ValvePosition?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        yield return (10033, Decimal(g.Delivered?.CubicMeters));
    }

    private static string Decimal(decimal? value)
        => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
}
