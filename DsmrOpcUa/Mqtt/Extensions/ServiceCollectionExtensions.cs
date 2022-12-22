using DsmrOpcUa.Dsmr;

namespace DsmrOpcUa.Mqtt.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMqttBroker(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            //serviceCollection.Configure<DsmrOptions>(configuration.GetSection(nameof(DsmrOptions)));
            serviceCollection.AddTransient<IDsmrProcessor, MqttBroker>().AddSingleton<IMqttBroker, MqttBroker>();

            return serviceCollection;
        }
    }
}
