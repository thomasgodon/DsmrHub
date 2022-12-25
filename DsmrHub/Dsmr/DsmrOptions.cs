namespace DsmrHub.Dsmr
{
    public class DsmrOptions
    {
        public string ComPort { get; init; } = null!;
        public bool UseExampleTelegram { get; init; }
        public int? SimulationRateInSeconds { get; init; } = null!;
        public int BaudRate { get; init; }
        public int Parity { get; init; }
        public int DataBits { get; init; }
        public int StopBits { get; init; }
    }
}
