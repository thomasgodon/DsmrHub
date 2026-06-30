using System.Text.Json;
using DsmrHub.Application.Dashboard;
using DsmrHub.Application.Dashboard.Options;
using DsmrHub.Domain.Events;
using MediatR;
using Microsoft.Extensions.Options;

namespace DsmrHub.Infrastructure.Dashboard;

/// <summary>
/// Projects each parsed <see cref="MeterReadingReceived"/> to a <see cref="DashboardSnapshot"/> and
/// publishes it (as single-line JSON) to the live dashboard's SSE broadcaster. No-op when the
/// dashboard is disabled.
/// </summary>
internal sealed class MeterReadingDashboardHandler : INotificationHandler<MeterReadingReceived>
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IMeterSnapshotBroadcaster _broadcaster;
    private readonly DashboardOptions _options;

    public MeterReadingDashboardHandler(IMeterSnapshotBroadcaster broadcaster, IOptions<DashboardOptions> options)
    {
        _broadcaster = broadcaster;
        _options = options.Value;
    }

    public Task Handle(MeterReadingReceived notification, CancellationToken cancellationToken)
    {
        if (!_options.Enabled) return Task.CompletedTask;

        var snapshot = DashboardSnapshot.From(notification.Reading, DateTimeOffset.Now);
        _broadcaster.Publish(JsonSerializer.Serialize(snapshot, SerializerOptions));

        return Task.CompletedTask;
    }
}
