namespace DsmrHub.IotHub.Models
{
    internal class IotDevice
    {
        public bool Enabled { get; set; } = default!;
        public string IdScope { get; set; } = default!;
        public string DeviceId { get; set; } = default!;
        public string PrimaryKey { get; set; } = default!;
        public string SecondaryKey { get; set; } = default!;
        public string ProvisioningUri { get; set; } = default!;
        public TimeSpan SendInterval { get; set; } = TimeSpan.FromMinutes(1);
    }
}
