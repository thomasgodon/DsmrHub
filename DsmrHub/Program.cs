using DsmrHub;
using DsmrHub.Dsmr.Extensions;
using DsmrHub.IotHub.Extensions;
using DsmrHub.Knx.Extensions;
using DsmrHub.Mqtt.Extensions;
using DsmrHub.Udp.Extensions;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        services.AddDsmrClient(configuration);
        services.AddMqttBroker();
        services.AddMqttClient();
        services.AddMqttConfiguration(configuration);
        services.AddUdpSender(configuration);
        services.AddIotHub(configuration);
        services.AddKnx(configuration);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
