using System.Diagnostics;
using System.Text;
using DsmrHub.IotHub.Models;
using DSMRParser.Models;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DsmrHub.IotHub
{
    internal class IotHubDevicesService : IIotHubDevicesService
    {
        private readonly ILogger<IotHubDevicesService> _logger;
        private readonly List<(DeviceClient Client, Stopwatch Interval, IotDevice DeviceOptions)> _deviceClients = new();
        private readonly IotHubOptions _options;

        public IotHubDevicesService(ILogger<IotHubDevicesService> logger, IOptions<IotHubOptions> iotCentralOptions)
        {
            _logger = logger;
            _options = iotCentralOptions.Value;
        }

        private async Task PopulateDeviceList(CancellationToken cancellationToken)
        {
            if (_deviceClients.Any())
            {
                return;
            }

            foreach (var optionsIotDevice in _options.IotDevices.Where(m => m.Enabled))
            {
                var client = await CreateDeviceClientAsync(optionsIotDevice, cancellationToken);
                if (client == null)
                {
                    continue;
                }

                var interval = new Stopwatch();
                interval.Start();
                _deviceClients.Add((client, interval, optionsIotDevice));
            }
        }

        public async Task Send(Telegram telegram, CancellationToken cancellationToken)
        {
            if (_options.IotDevices.Any(m => m.Enabled) is false)
            {
                return;
            }

            await PopulateDeviceList(cancellationToken);

            // process telegram
            var serializedResult = JsonConvert.SerializeObject(telegram);

            foreach (var (client, interval, deviceOptions) in _deviceClients)
            {
                if (deviceOptions.Enabled is false)
                {
                    return;
                }

                if (interval.Elapsed < deviceOptions.SendInterval)
                {
                    continue;
                }

                try
                {
                    var message = new Message(Encoding.UTF8.GetBytes(serializedResult))
                    {
                        ContentEncoding = Encoding.UTF8.WebName
                    };

                    await client.SendEventAsync(message, cancellationToken);
                    _logger.LogTrace("Send to device with id: {deviceId}", deviceOptions.DeviceId);
                    interval.Restart();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Could not send message to device {deviceId}", deviceOptions.DeviceId);
                }
            }
        }

        private async Task<DeviceClient?> CreateDeviceClientAsync(IotDevice options, CancellationToken cancellationToken)
        {
            var underlyingIotHub = await GetUnderlyingIotHub(options, cancellationToken);

            if (underlyingIotHub == null)
            {
                return null;
            }

            var authMethod = new DeviceAuthenticationWithRegistrySymmetricKey(options.DeviceId, options.PrimaryKey);
            var client = DeviceClient.Create(underlyingIotHub, authMethod, TransportType.Amqp);
            if (client == null)
            {
                _logger.LogError("Could not create device {deviceId}", options.DeviceId);
            }
            return client;
        }

        private async Task<string?> GetUnderlyingIotHub(IotDevice options, CancellationToken cancellationToken)
        {
            try
            {
                using var symmetricKeyProvider = new SecurityProviderSymmetricKey(options.DeviceId, options.PrimaryKey, options.SecondaryKey);
                var dps = ProvisioningDeviceClient.Create(options.ProvisioningUri, options.IdScope, symmetricKeyProvider, new ProvisioningTransportHandlerAmqp());
                var registerResult = await dps.RegisterAsync(cancellationToken);
                _logger.LogInformation("New registration succeeded for device {deviceId}", options.DeviceId);
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
