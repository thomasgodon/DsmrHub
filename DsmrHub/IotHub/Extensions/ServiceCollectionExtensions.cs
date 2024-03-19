using DsmrHub.Dsmr;

namespace DsmrHub.IotHub.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIotHub(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<IotHubOptions>(configuration.GetSection(nameof(IotHubOptions)));
            serviceCollection.AddSingleton<IDsmrProcessor, IotHubProcessor>();
            return serviceCollection;
        }
    }
}
