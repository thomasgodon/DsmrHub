namespace DsmrHub.IotHub
{
    internal class IotHubOptions
    {
        public bool Enabled { get; set; } = default!;
        public string IdScope { get; set; } = default!;
        public string DeviceId { get; set; } = default!;
        public string PrimaryKey { get; set; } = default!;
        public string SecondaryKey { get; set; } = default!;
        public string ProvisioningUri { get; set; } = default!;
    }
}
