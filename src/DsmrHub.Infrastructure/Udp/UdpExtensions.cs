using System.Net.Sockets;

namespace DsmrHub.Infrastructure.Udp;

internal static class UdpExtensions
{
    public static async Task SendToAsync(this byte[] data, string host, int port, CancellationToken cancellationToken)
    {
        using var udpSender = new UdpClient();
        udpSender.Connect(host, port);
        await udpSender.SendAsync(data, cancellationToken);
    }
}
