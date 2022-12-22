using System.IO.Ports;
using DsmrIotDevice.Dsmr;
using DsmrOpcUa.Dsmr;
using DsmrParser.Dsmr;

namespace DsmrOpcUa.OpcUaServer.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpcUaServer(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            //serviceCollection.Configure<DsmrOptions>(configuration.GetSection(nameof(DsmrOptions)));
            serviceCollection.AddTransient<IDsmrProcessor, OpcUaServer>();

            return serviceCollection;
        }
    }
}
