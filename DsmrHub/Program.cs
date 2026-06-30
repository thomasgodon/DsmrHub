using DsmrHub;
using DsmrHub.Application;
using DsmrHub.Application.Dashboard.Options;
using DsmrHub.Dashboard;
using DsmrHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<DashboardOptions>(builder.Configuration.GetSection(nameof(DashboardOptions)));
var dashboardOptions = builder.Configuration.GetSection(nameof(DashboardOptions)).Get<DashboardOptions>() ?? new DashboardOptions();

// Bind Kestrel to the dashboard port when enabled; otherwise bind no endpoints so the host behaves
// like the original worker service (no listening port). We configure Kestrel explicitly via
// ListenAnyIP so the app ignores any ambient HTTP_PORTS/URLS environment; when disabled an empty
// URLS keeps Kestrel from falling back to its default :5000.
if (dashboardOptions.Enabled)
{
    builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(dashboardOptions.Port));
}
else
{
    builder.WebHost.UseUrls(string.Empty);
}

var app = builder.Build();

if (dashboardOptions.Enabled)
{
    app.Logger.LogInformation("Dashboard listening on http://*:{Port}", dashboardOptions.Port);
    app.MapDashboard();
}
else
{
    app.Logger.LogInformation("Dashboard disabled; no HTTP port bound.");
}

await app.RunAsync();
