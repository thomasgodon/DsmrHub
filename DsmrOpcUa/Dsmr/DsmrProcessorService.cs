using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmrParser.Models;
using DsmrParser.Dsmr;
using DotNetty.Codecs.Mqtt.Packets;

namespace DsmrOpcUa.Dsmr;

internal class DsmrProcessorService : IDsmrProcessorService
{
    private readonly ILogger<DsmrProcessorService> _logger;
    private readonly StringBuilder _buffer = new();
    private readonly Parser _dsmrParser;

    public DsmrProcessorService(Parser dsmrParser, ILogger<DsmrProcessorService> logger)
    {
        _dsmrParser = dsmrParser;
        _logger = logger;
    }

    public async Task ProcessMessage(string message, CancellationToken cancellationToken)
    {
        try
        {
            var telegrams = await _dsmrParser.Parse(message).WaitAsync(cancellationToken);

            foreach (var telegram in telegrams)
            {
                var parsedTelegram = JsonConvert.SerializeObject(telegram);
                _logger.LogInformation(parsedTelegram);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }
}