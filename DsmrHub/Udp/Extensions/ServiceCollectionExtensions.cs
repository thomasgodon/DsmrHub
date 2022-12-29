using DsmrHub.Dsmr;
using DsmrHub.Mqtt;
using System.Configuration;

namespace DsmrHub.Udp.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUdpSender(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.Configure<MqttOptions>(configuration.GetSection(nameof(UdpOptions)));
            return serviceCollection;
        }
    }
}
