using DsmrHub.Dsmr;

namespace DsmrHub.Mqtt.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttBroker(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IMqttBroker, MqttBroker>();

            return serviceCollection;
        }

        public static IServiceCollection AddMqttClient(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient<IDsmrProcessor, MqttProcessor>();

            return serviceCollection;
        }

        public static IServiceCollection AddMqttConfiguration(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            return serviceCollection.Configure<MqttOptions>(configuration.GetSection(nameof(MqttOptions)));
        }
    }
}
