using DsmrHub.Domain;

namespace DsmrHub.Application.Abstractions;

/// <summary>
/// Port that turns a raw DSMR telegram string into the domain's <see cref="MeterReading"/>.
/// Implemented in the infrastructure layer against a concrete parsing library.
/// </summary>
public interface ITelegramParser
{
    /// <summary>
    /// Attempts to parse and map a raw telegram. Returns <c>false</c> (and a null reading)
    /// when the input is not a valid, complete telegram.
    /// </summary>
    bool TryParse(string rawTelegram, out MeterReading? reading);
}
