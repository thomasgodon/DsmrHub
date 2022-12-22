namespace DsmrOpcUa.Dsmr;

public interface IDsmrClient
{
    Task Start(CancellationToken cancellationToken);
}