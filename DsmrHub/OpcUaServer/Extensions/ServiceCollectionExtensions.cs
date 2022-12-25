using DsmrHub.Dsmr;

namespace DsmrHub.OpcUaServer.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpcUaServer(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            //serviceCollection.Configure<DsmrOptions>(configuration.GetSection(nameof(DsmrOptions)));
            //serviceCollection.AddTransient<IDsmrProcessor, OpcUaServer>();

            return serviceCollection;
        }
    }
}
