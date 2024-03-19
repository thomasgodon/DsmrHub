using System.Diagnostics;
using System.Text;
using DsmrHub.Dsmr;
using DSMRParser.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DsmrHub.IotHub
{
    internal class IotHubProcessor : IDsmrProcessor
    {
        private readonly ILogger<IotHubProcessor> _logger;
        private readonly IotHubOptions _iotHubOptions;
        private readonly Stopwatch _registerInterval;
        private DeviceClient _deviceClient = default!;

        public IotHubProcessor(ILogger<IotHubProcessor> logger, IOptions<IotHubOptions> iotHubOptions)
        {
            _logger = logger;
            _iotHubOptions = iotHubOptions.Value;
            _registerInterval = new Stopwatch();
        }

        public async Task ProcessTelegram(Telegram telegram, CancellationToken cancellationToken)
        {
            if (!_iotHubOptions.Enabled) return;

            try
            {
                var message = new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(telegram)))
                {
                    ContentEncoding = Encoding.UTF8.WebName
                };

                if (_registerInterval.IsRunning is false)
                {
                    await CreateDeviceClientAsync(cancellationToken);
                }
                
                await _deviceClient.SendEventAsync(message, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not send message to device {deviceId}", _iotHubOptions.DeviceId);
                await CreateDeviceClientAsync(cancellationToken);
            }
        }

        private async Task CreateDeviceClientAsync(CancellationToken cancellationToken)
        {
            if (_registerInterval.IsRunning && _registerInterval.Elapsed < TimeSpan.FromHours(1))
            {
                return;
            }

            var underlyingIotHub = await GetUnderlyingIotHub(cancellationToken);

            if (underlyingIotHub == null)
            {
                return;
            }

            _deviceClient = CreateDeviceClient(underlyingIotHub);
            _registerInterval.Restart();
        }

        private async Task<string?> GetUnderlyingIotHub(CancellationToken cancellationToken)
        {
            try
            {
                using var symmetricKeyProvider = new SecurityProviderSymmetricKey(_iotHubOptions.DeviceId, _iotHubOptions.PrimaryKey, _iotHubOptions.SecondaryKey);
                var dps = ProvisioningDeviceClient.Create(_iotHubOptions.ProvisioningUri, _iotHubOptions.IdScope, symmetricKeyProvider, new ProvisioningTransportHandlerAmqp());
                var registerResult = await dps.RegisterAsync(cancellationToken);
                _logger.LogInformation("New registration succeeded for device {deviceId}", _iotHubOptions.DeviceId);
                return registerResult.AssignedHub;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not get underlying iot hub");
                return null;
            }
        }

        private DeviceClient CreateDeviceClient(string assignedIotHub)
        {
            var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(_iotHubOptions.DeviceId, _iotHubOptions.PrimaryKey);
            var client = DeviceClient.Create(assignedIotHub, authMethod, TransportType.Amqp);
            return client;
        }
    }
}
