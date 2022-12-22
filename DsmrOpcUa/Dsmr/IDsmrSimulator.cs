namespace DsmrOpcUa.Dsmr;

public interface IDsmrSimulator
{
    Task Start(CancellationToken cancellationToken);
}