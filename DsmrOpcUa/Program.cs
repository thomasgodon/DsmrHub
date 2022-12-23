using DsmrOpcUa;
using DsmrOpcUa.Dsmr.Extensions;
using DsmrOpcUa.Mqtt.Extensions;
using DsmrOpcUa.OpcUaServer.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        services.AddDsmrClient(configuration);
        services.AddOpcUaServer(configuration);
        services.AddMqttBroker();
        services.AddMqttClient();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
