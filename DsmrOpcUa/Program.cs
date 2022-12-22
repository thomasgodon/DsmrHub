using DsmrOpcUa;
using DsmrOpcUa.Dsmr.Extensions;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        IConfiguration configuration = hostContext.Configuration;
        services.AddDsmrClient(configuration);
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
