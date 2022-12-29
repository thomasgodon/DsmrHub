using DsmrHub.Dsmr;
using DsmrHub.Dsmr.Extensions;
using DsmrHub.Udp.Extensions;
using DSMRParser.Models;
using Microsoft.Extensions.Options;

namespace DsmrHub.Udp
{
    internal class UdpProcessor : IDsmrProcessor
    {
        private readonly ILogger<UdpProcessor> _logger;
        private readonly UdpOptions _udpOptions;

        public UdpProcessor(ILogger<UdpProcessor> logger, IOptions<UdpOptions> udpOptions)
        {
            _logger = logger;
            _udpOptions = udpOptions.Value;
        }

        async Task IDsmrProcessor.ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (!_udpOptions.Enabled) return;

            await telegram.ToUdpPacket("PowerConsumptionTariff1").SendToAsync(_udpOptions.Host, 10000, cancellationToken);
            await telegram.ToUdpPacket("PowerConsumptionTariff2").SendToAsync(_udpOptions.Host, 10001, cancellationToken);
            await telegram.ToUdpPacket("InstantaneousElectricityDelivery").SendToAsync(_udpOptions.Host, 10005, cancellationToken);
            await telegram.ToUdpPacket("InstantaneousElectricityUsage").SendToAsync(_udpOptions.Host, 10006, cancellationToken);
            await telegram.ToUdpPacket("CurrentTariff").SendToAsync(_udpOptions.Host, 10007, cancellationToken);
            await telegram.ToUdpPacket("SerialNumberElectricityMeter").SendToAsync(_udpOptions.Host, 10008, cancellationToken);
            await telegram.ToUdpPacket("InstantaneousCurrent").SendToAsync(_udpOptions.Host, 10010, cancellationToken);
            await telegram.ToUdpPacket("GasUsage").SendToAsync(_udpOptions.Host, 10020, cancellationToken);
            await telegram.ToUdpPacket("SerialNumberGasMeter").SendToAsync(_udpOptions.Host, 10021, cancellationToken);
            await telegram.ToUdpPacket("MessageHeader").SendToAsync(_udpOptions.Host, 10030, cancellationToken);
            await telegram.ToUdpPacket("MessageVersion").SendToAsync(_udpOptions.Host, 10031, cancellationToken);
            await telegram.ToUdpPacket("Timestamp").SendToAsync(_udpOptions.Host, 10032, cancellationToken);
        }
    }
}
