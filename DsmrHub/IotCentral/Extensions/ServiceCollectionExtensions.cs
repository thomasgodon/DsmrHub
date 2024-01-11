using DsmrHub.Dsmr;
using DsmrHub.Udp;

namespace DsmrHub.IotCentral.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIotCentral(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<IotCentralOptions>(configuration.GetSection(nameof(IotCentralOptions)));
            serviceCollection.AddSingleton<IDsmrProcessor, IotCentralProcessor>();
            return serviceCollection;
        }
    }
}
