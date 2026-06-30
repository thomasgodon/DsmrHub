namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// One month's peak quarter-hourly demand from the maximum-demand history
/// (OBIS 0-0:98.1.0): the billing period it belongs to, when the peak occurred,
/// and the peak average power.
/// </summary>
public sealed record MonthlyPeakDemand(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeakOccurredAt,
    PowerValue Peak);
