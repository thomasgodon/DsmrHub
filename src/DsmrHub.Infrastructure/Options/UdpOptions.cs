namespace DsmrHub.Infrastructure.Options;

public sealed class UdpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = default!;
}
