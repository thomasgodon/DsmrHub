using DsmrIotDevice.Dsmr;
using DsmrParser.Dsmr;
using System.IO.Ports;

namespace DsmrOpcUa.Dsmr.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDsmrClient(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<DsmrOptions>(configuration.GetSection(nameof(DsmrOptions)));
            serviceCollection.AddSingleton<IDsmrClient, DsmrClient>();
            serviceCollection.AddSingleton<IDsmrSimulator, DsmrSimulator>();
            serviceCollection.AddSingleton<IDsmrProcessorService, DsmrProcessorService>();
            serviceCollection.AddSingleton<Parser>();
            serviceCollection.AddSingleton<SerialPort>();

            return serviceCollection;
        }
    }
}
