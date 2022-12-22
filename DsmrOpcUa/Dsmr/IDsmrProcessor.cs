using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmrParser.Models;

namespace DsmrOpcUa.Dsmr
{
    public interface IDsmrProcessor
    {
        Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken);
    }
}
