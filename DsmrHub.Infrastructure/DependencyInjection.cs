using DsmrHub.Application.Abstractions;
using DsmrHub.Application.Dashboard.Options;
using DsmrHub.Domain.Events;
using DsmrHub.Infrastructure.Dashboard;
using DsmrHub.Infrastructure.Dsmr;
using DsmrHub.Infrastructure.IotHub;
using DsmrHub.Infrastructure.Knx;
using DsmrHub.Infrastructure.Mqtt;
using DsmrHub.Infrastructure.Options;
using DsmrHub.Infrastructure.Udp;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DsmrHub.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the infrastructure layer: options, the DSMR parser, the selected telegram source,
    /// and the sink handlers (MQTT, KNX, IoT Hub, UDP) that react to <see cref="MeterReadingReceived"/>.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.Configure<DsmrOptions>(configuration.GetSection(nameof(DsmrOptions)));
        services.Configure<MqttOptions>(configuration.GetSection(nameof(MqttOptions)));
        services.Configure<UdpOptions>(configuration.GetSection(nameof(UdpOptions)));
        services.Configure<IotHubOptions>(configuration.GetSection(nameof(IotHubOptions)));
        services.Configure<KnxOptions>(configuration.GetSection(nameof(KnxOptions)));
        services.Configure<DashboardOptions>(configuration.GetSection(nameof(DashboardOptions)));

        // DSMR parsing + telegram source (serial port or simulator, chosen by configuration)
        services.AddSingleton<ITelegramParser, DsmrTelegramParser>();
        services.AddSingleton<IMeterReadingSource>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DsmrOptions>>().Value;
            return options.UseSimulator
                ? ActivatorUtilities.CreateInstance<SimulatedMeterReadingSource>(sp)
                : ActivatorUtilities.CreateInstance<SerialMeterReadingSource>(sp);
        });

        // Sinks
        services.AddSingleton<IMqttBroker, MqttBroker>();
        services.AddSingleton<IIotHubDevicesService, IotHubDevicesService>();

        services.AddSingleton<INotificationHandler<MeterReadingReceived>, MqttMeterReadingHandler>();
        services.AddSingleton<INotificationHandler<MeterReadingReceived>, KnxMeterReadingHandler>();
        services.AddSingleton<INotificationHandler<MeterReadingReceived>, IotHubMeterReadingHandler>();
        services.AddTransient<INotificationHandler<MeterReadingReceived>, UdpMeterReadingHandler>();
        services.AddSingleton<INotificationHandler<MeterReadingReceived>, MeterReadingDashboardHandler>();

        return services;
    }
}
