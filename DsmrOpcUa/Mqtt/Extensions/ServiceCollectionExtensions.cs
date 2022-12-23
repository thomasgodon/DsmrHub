using DsmrOpcUa.Dsmr;

namespace DsmrOpcUa.Mqtt.Extensions
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
            serviceCollection.AddTransient<IDsmrProcessor, MqttClient>();

            return serviceCollection;
        }
    }
}
