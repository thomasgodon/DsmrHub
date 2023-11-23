using DsmrHub.Dsmr;
using DsmrHub.Dsmr.Extensions;
using DsmrHub.Udp.Extensions;
using DSMRParser.Models;
using Microsoft.Extensions.Options;

namespace DsmrHub.Udp
{
    internal class UdpProcessor : IDsmrProcessor
    {
        private readonly UdpOptions _udpOptions;

        public UdpProcessor(IOptions<UdpOptions> udpOptions)
        {
            _udpOptions = udpOptions.Value;
        }

        async Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (!_udpOptions.Enabled) return;

            await telegram.ToUdpPacket(nameof(telegram.Identification)).SendToAsync(_udpOptions.Host, 10000, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.DSMRVersion)).SendToAsync(_udpOptions.Host, 10001, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.EquipmentId)).SendToAsync(_udpOptions.Host, 10002, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.TimeStamp)).SendToAsync(_udpOptions.Host, 10003, cancellationToken);

            await telegram.ToUdpPacket(nameof(telegram.EnergyDeliveredTariff1)).SendToAsync(_udpOptions.Host, 10010, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.EnergyDeliveredTariff2)).SendToAsync(_udpOptions.Host, 10011, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.EnergyReturnedTariff1)).SendToAsync(_udpOptions.Host, 10012, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.EnergyReturnedTariff2)).SendToAsync(_udpOptions.Host, 10013, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.ElectricityTariff)).SendToAsync(_udpOptions.Host, 10014, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.PowerDelivered)).SendToAsync(_udpOptions.Host, 10015, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.PowerReturned)).SendToAsync(_udpOptions.Host, 10016, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.PowerDeliveredL1)).SendToAsync(_udpOptions.Host, 10017, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.PowerReturnedL1)).SendToAsync(_udpOptions.Host, 10018, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.VoltageL1)).SendToAsync(_udpOptions.Host, 10019, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.CurrentL1)).SendToAsync(_udpOptions.Host, 10020, cancellationToken);

            await telegram.ToUdpPacket(nameof(telegram.GasDeviceType)).SendToAsync(_udpOptions.Host, 10030, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.GasEquipmentId)).SendToAsync(_udpOptions.Host, 10031, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.GasValvePosition)).SendToAsync(_udpOptions.Host, 10032, cancellationToken);
            await telegram.ToUdpPacket(nameof(telegram.GasDelivered)).SendToAsync(_udpOptions.Host, 10033, cancellationToken);
        }
    }
}
