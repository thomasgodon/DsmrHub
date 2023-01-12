﻿using System.Diagnostics;
using System.Text;
using DsmrHub.Dsmr;
using DsmrHub.Udp;
using DSMRParser.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static System.Formats.Asn1.AsnWriter;

namespace DsmrHub.IotCentral
{
    internal class IotCentralProcessor : IDsmrProcessor
    {
        private readonly ILogger<IotCentralProcessor> _logger;
        private readonly IotCentralOptions _iotCentralOptions;
        private readonly Stopwatch _registerInterval;
        private string? _underlyingIotHub;

        public IotCentralProcessor(ILogger<IotCentralProcessor> logger, IOptions<IotCentralOptions> iotCentralOptions)
        {
            _logger = logger;
            _iotCentralOptions = iotCentralOptions.Value;
            _registerInterval = new Stopwatch();
        }

        public async Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (!_iotCentralOptions.Enabled) return;

            await RegisterDevice();

            if (_underlyingIotHub == null)
            {
                return;
            }

            try
            {
                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telegram)))
                {
                    ContentEncoding = Encoding.UTF8.WebName
                };

                var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(_iotCentralOptions.DeviceId, _iotCentralOptions.PrimaryKey);
                await using var client = DeviceClient.Create(_underlyingIotHub, authMethod, TransportType.Amqp);
                await client.SendEventAsync(message, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"could not send message to device {_iotCentralOptions.DeviceId}");
            }
        }

        private async Task RegisterDevice()
        {
            if (_registerInterval.IsRunning && _registerInterval.Elapsed < TimeSpan.FromHours(1))
            {
                return;
            }

            _underlyingIotHub = await GetUnderlyingIotHub();

            if (_underlyingIotHub == null)
            {
                return;
            }
            
            _registerInterval.Restart();
        }

        private async Task<string?> GetUnderlyingIotHub()
        {
            try
            {
                using var symmetricKeyProvider = new SecurityProviderSymmetricKey(_iotCentralOptions.DeviceId, _iotCentralOptions.PrimaryKey, _iotCentralOptions.SecondaryKey);
                var dps = ProvisioningDeviceClient.Create(_iotCentralOptions.ProvisioningUri, _iotCentralOptions.IdScope, symmetricKeyProvider, new ProvisioningTransportHandlerAmqp());
                var registerResult = await dps.RegisterAsync(TimeSpan.FromMinutes(1));
                _logger.LogInformation($"New registration succeeded for device {_iotCentralOptions.DeviceId}");
                return registerResult.AssignedHub;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not get underlying iot hub");
                return null;
            }
        }
    }
}
