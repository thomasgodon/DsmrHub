namespace DsmrHub.Dsmr;

public interface IDsmrClient
{
    Task Start(CancellationToken cancellationToken);
}