namespace DsmrHub.Infrastructure.Options;

public sealed class IotHubOptions
{
    public List<IotDevice> IotDevices { get; set; } = default!;
}

public sealed class IotDevice
{
    public bool Enabled { get; set; }
    public string IdScope { get; set; } = default!;
    public string DeviceId { get; set; } = default!;
    public string PrimaryKey { get; set; } = default!;
    public string SecondaryKey { get; set; } = default!;
    public string ProvisioningUri { get; set; } = default!;
    public TimeSpan SendInterval { get; set; } = TimeSpan.FromMinutes(1);
}
