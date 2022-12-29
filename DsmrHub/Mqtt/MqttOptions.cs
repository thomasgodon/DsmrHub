namespace DsmrHub.Mqtt
{
    internal class MqttOptions
    {
        public bool Enabled { get; set; }
        public int Port { get; set; }
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
