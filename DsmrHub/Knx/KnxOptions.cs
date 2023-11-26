namespace DsmrHub.Knx
{
    internal class KnxOptions
    {
        public bool Enabled { get; set; } = default!;
        public string Host { get; set; } = default!;
        public int Port { get; set; } = default!;
        public string KnxDeviceAddress { get; set; } = default!;
        public Dictionary<string, string> GroupAddressMapping { get; set; } = default!;
    }
}
