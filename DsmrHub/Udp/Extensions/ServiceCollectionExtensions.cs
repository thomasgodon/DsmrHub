using DsmrHub.Dsmr;
using DsmrHub.Mqtt;
using System.Configuration;
using DsmrHub.IotCentral;

namespace DsmrHub.Udp.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUdpSender(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<IotCentralOptions>(configuration.GetSection(nameof(IotCentralOptions)));
            serviceCollection.AddTransient<IDsmrProcessor, IotCentralProcessor>();
            return serviceCollection;
        }
    }
}
