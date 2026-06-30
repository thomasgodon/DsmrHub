namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// A single entry of the long power-failure event log (OBIS 1-0:99.97.0):
/// the moment power was restored and how long the failure lasted.
/// </summary>
public sealed record PowerFailureEvent(
    DateTimeOffset EndTime,
    TimeSpan Duration);
