using DsmrHub;
using DsmrHub.Dsmr.Extensions;
using DsmrHub.Mqtt.Extensions;
using DsmrHub.OpcUaServer.Extensions;
using DsmrHub.Udp.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        services.AddDsmrClient(configuration);
        services.AddOpcUaServer(configuration);
        services.AddMqttBroker();
        services.AddMqttClient();
        services.AddMqttConfiguration(configuration);
        services.AddUdpSender(configuration);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
