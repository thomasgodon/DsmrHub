namespace DsmrHub.Domain.ValueObjects;

/// <summary>
/// The active electricity tariff as reported by the meter (DSMR OBIS 0-0:96.14.0).
/// </summary>
public enum ElectricityTariff
{
    Unknown = 0,
    Tariff1 = 1,
    Tariff2 = 2,
}
