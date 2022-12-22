﻿using Microsoft.Azure.Amqp.Framing;
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
    private readonly IEnumerable<IDsmrProcessor> _dsmrProcessors;

    public DsmrProcessorService(Parser dsmrParser, ILogger<DsmrProcessorService> logger, IEnumerable<IDsmrProcessor> dsmrProcessors)
    {
        _dsmrParser = dsmrParser;
        _logger = logger;
        _dsmrProcessors = dsmrProcessors;
    }

    public async Task ProcessMessage(string message, CancellationToken cancellationToken)
    {
        try
        {
            var telegrams = await _dsmrParser.Parse(message).WaitAsync(cancellationToken);

            foreach (var telegram in telegrams)
            {
                _logger.LogTrace(telegram.ToString());

                foreach (var dsmrProcessor in _dsmrProcessors)
                {
                    await dsmrProcessor.ProcessTelegram(telegram, cancellationToken);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }
}