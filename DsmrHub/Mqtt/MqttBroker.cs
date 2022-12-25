using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace DsmrHub.Mqtt
{
    internal class MqttBroker : IMqttBroker
    {
        private readonly ILogger<MqttBroker> _logger;
        private readonly MqttServer _server;
        private readonly MqttServerOptions _serverOptions;
        private readonly MqttOptions _mqttOptions;

        public MqttBroker(ILogger<MqttBroker> logger, IOptions<MqttOptions> mqttOptions)
        {
            _logger = logger;
            _mqttOptions = mqttOptions.Value;

            _serverOptions = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(100)
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(_mqttOptions.Port)
                .Build();
            _server = new MqttFactory().CreateMqttServer(_serverOptions);
            _server.ValidatingConnectionAsync += ValidateConnectionAsyncHandler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_server.IsStarted)
            {
                _logger.LogInformation("MQTT Broker starting...");
                await _server.StartAsync().WaitAsync(cancellationToken);
                _logger.LogInformation($"{nameof(MqttBroker)} started on port: {_serverOptions.DefaultEndpointOptions.Port}");
            }
        }

        private Task ValidateConnectionAsyncHandler(ValidatingConnectionEventArgs args)
        {
            if (args.UserName != _mqttOptions.Username)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return Task.CompletedTask;
            }
            if (args.Password != _mqttOptions.Password)
            {
                args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                return Task.CompletedTask;
            }
            return Task.CompletedTask;
        }
    }
}
