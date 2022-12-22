using DsmrParser.Models;

namespace DsmrOpcUa.Dsmr;

internal interface IDsmrProcessorService
{
    Task ProcessMessage(string message, CancellationToken cancellationToken);
}