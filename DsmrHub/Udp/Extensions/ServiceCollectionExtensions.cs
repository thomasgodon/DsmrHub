using DsmrHub.Dsmr;
using DsmrHub.Mqtt;
using System.Configuration;

namespace DsmrHub.Udp.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUdpSender(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<UdpOptions>(configuration.GetSection(nameof(UdpOptions)));
            serviceCollection.AddTransient<IDsmrProcessor, UdpProcessor>();
            return serviceCollection;
        }
    }
}
