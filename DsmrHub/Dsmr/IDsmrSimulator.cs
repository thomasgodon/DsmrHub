namespace DsmrHub.Dsmr;

public interface IDsmrSimulator
{
    Task Start(CancellationToken cancellationToken);
}