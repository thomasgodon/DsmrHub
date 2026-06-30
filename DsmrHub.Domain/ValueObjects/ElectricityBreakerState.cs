namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// State of the electricity breaker / disconnect switch (Fluvius OBIS 0-0:96.3.10).
/// </summary>
public enum ElectricityBreakerState
{
    Unknown = -1,
    Disconnected = 0,
    Connected = 1,
    ReadyForReconnection = 2,
}
