using DsmrHub.Dsmr;
using DsmrHub.IotHub.Models;

namespace DsmrHub.IotHub.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIotHub(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<IotHubOptions>(configuration.GetSection(nameof(IotHubOptions)));
            serviceCollection.AddSingleton<IIotHubDevicesService, IotHubDevicesService>();
            serviceCollection.AddSingleton<IDsmrProcessor, IotHubProcessor>();
            return serviceCollection;
        }
    }
}
