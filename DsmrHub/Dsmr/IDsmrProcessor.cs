using DSMRParser.Models;

namespace DsmrHub.Dsmr
{
    public interface IDsmrProcessor
    {
        Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken);
    }
}
