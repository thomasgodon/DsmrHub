using DsmrHub.Application.Abstractions;
using DsmrHub.Application.Dashboard;
using DsmrHub.Application.Telegrams;
using Microsoft.Extensions.DependencyInjection;

namespace DsmrHub.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application layer: MediatR (scanning this assembly), the telegram ingestion
    /// use case, and the dashboard snapshot broadcaster.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddSingleton<ITelegramIngestionService, TelegramIngestionService>();
        services.AddSingleton<IMeterSnapshotBroadcaster, MeterSnapshotBroadcaster>();

        return services;
    }
}
