using System.Threading.Channels;
using DsmrHub.Infrastructure.Options;
using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Sdk;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DsmrHub.Infrastructure.Knx;

/// <summary>
/// Owns the KNX IP tunnel and writes queued meter values on a background loop, so the serial
/// ingestion never waits on the bus. Values are coalesced per capability in <see cref="KnxPendingValues"/>
/// (newest wins), every write is awaited with a timeout, failed writes are retried, and a tunnel that
/// stops confirming writes (while still reporting "connected") is torn down and reconnected.
/// Also answers KNX read requests for the mapped group addresses.
/// </summary>
internal sealed class KnxBusPublisher : BackgroundService
{
    // Our own guard: a write completes when the interface confirms the TP send (L_Data.con). A stuck
    // tunnel never confirms, so without a timeout the loop would wait forever.
    private static readonly TimeSpan WriteTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RetryBackoff = TimeSpan.FromSeconds(5);
    private const int MaxConsecutiveFailures = 3;

    private readonly ILogger<KnxBusPublisher> _logger;
    private readonly KnxOptions _options;
    private readonly KnxPendingValues _pending = new();
    private readonly Dictionary<string, GroupAddress> _addressByCapability;
    private readonly Dictionary<GroupAddress, string> _capabilityByAddress;
    private readonly HashSet<string> _negativeConfirmationLogged = new();
    private readonly Channel<bool> _signal = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleReader = true,
    });

    private KnxBus? _knxBus;
    private int _consecutiveFailures;

    public KnxBusPublisher(ILogger<KnxBusPublisher> logger, IOptions<KnxOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        _addressByCapability = (_options.GroupAddressMapping ?? new Dictionary<string, string>())
            .Where(mapping => string.IsNullOrEmpty(mapping.Value) is false)
            .ToDictionary(mapping => mapping.Key, mapping => GroupAddress.Parse(mapping.Value));
        _capabilityByAddress = _addressByCapability.ToDictionary(pair => pair.Value, pair => pair.Key);
    }

    /// <summary>Queues a value for the bus. Never blocks; unmapped capabilities are ignored.</summary>
    public void Enqueue(string capability, byte[] value, bool isBit)
    {
        if (_options.Enabled is false || _addressByCapability.ContainsKey(capability) is false) return;

        if (_pending.Set(capability, value, isBit))
        {
            _signal.Writer.TryWrite(true);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Enabled is false) return;

        try
        {
            while (stoppingToken.IsCancellationRequested is false)
            {
                await _signal.Reader.ReadAsync(stoppingToken);

                if (await SendPendingAsync(stoppingToken)) continue;

                // Failed pass: wait, then retry the requeued values without waiting for the next telegram.
                await Task.Delay(RetryBackoff, stoppingToken);
                _signal.Writer.TryWrite(true);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host is stopping.
        }
        finally
        {
            // Close the tunnel explicitly: otherwise the IP interface keeps the slot until it times out
            // and a freshly started container is refused with "no more connections available".
            await DisconnectAsync();
        }
    }

    /// <summary>Writes every pending value. Returns false when the pass stopped on a failure.</summary>
    private async Task<bool> SendPendingAsync(CancellationToken stoppingToken)
    {
        KnxBus bus;
        try
        {
            bus = await EnsureConnectedAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || stoppingToken.IsCancellationRequested is false)
        {
            _logger.LogWarning("Could not connect to {host}: {message}. Retrying in {backoff}s", _options.Host, ex.Message, RetryBackoff.TotalSeconds);
            await DisconnectAsync();
            return false;
        }

        while (_pending.TryTakeNext(out var value))
        {
            var address = _addressByCapability[value!.Capability];
            var groupValue = new KnxTelegramValue(address) { Value = value.Value, IsBit = value.IsBit }.ToGroupValue()!;

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeout.CancelAfter(WriteTimeout);

                var confirmed = await bus.WriteGroupValueAsync(address, groupValue, MessagePriority.Low, timeout.Token);

                // Either way the telegram went out and the tunnel is alive. A negative confirmation only
                // means no TP device acknowledged it; IP listeners such as Home Assistant still get it.
                _pending.MarkSent(value);
                _consecutiveFailures = 0;

                if (confirmed is false && _negativeConfirmationLogged.Add(value.Capability))
                {
                    _logger.LogWarning("KNX write to {address} ({capability}) was not acknowledged on the bus (no TP device listens to it?)", address, value.Capability);
                }
            }
            catch (Exception ex) when (stoppingToken.IsCancellationRequested is false)
            {
                _pending.Requeue(value);
                _consecutiveFailures++;

                var reason = ex is OperationCanceledException ? $"no confirmation within {WriteTimeout.TotalSeconds}s" : ex.Message;
                _logger.LogWarning("KNX write to {address} ({capability}) failed: {reason}", address, value.Capability, reason);

                if (_consecutiveFailures >= MaxConsecutiveFailures)
                {
                    _logger.LogWarning("KNX tunnel unresponsive after {count} failed writes, reconnecting", _consecutiveFailures);
                    _consecutiveFailures = 0;
                    await DisconnectAsync();
                }

                return false;
            }
        }

        return true;
    }

    private async Task<KnxBus> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_knxBus is { ConnectionState: BusConnectionState.Connected })
        {
            return _knxBus;
        }

        await DisconnectAsync();

        var bus = new KnxBus(new IpTunnelingConnectorParameters(_options.Host, _options.Port));
        bus.GroupMessageReceived += OnGroupMessageReceived;
        _knxBus = bus;

        _logger.LogInformation("Connecting to {host}", _options.Host);
        await bus.ConnectAsync(cancellationToken);

        // Over IP tunneling the interface assigns each tunnel its own individual address, so overriding
        // it is optional. Requesting one that another tunnel already holds fails; that must not stop
        // the bridge, the tunnel's assigned address works fine.
        if (string.IsNullOrWhiteSpace(_options.IndividualAddress) is false)
        {
            try
            {
                await bus.SetInterfaceConfigurationAsync(new BusInterfaceConfiguration(IndividualAddress.Parse(_options.IndividualAddress)), cancellationToken);
            }
            catch (ConnectorException ex)
            {
                _logger.LogWarning("Could not set individual address {address}, using the tunnel-assigned address instead: {message}", _options.IndividualAddress, ex.Message);
            }
        }

        _logger.LogInformation("Connected to {host} as {address}", _options.Host, bus.InterfaceConfiguration.IndividualAddress);
        return bus;
    }

    private async Task DisconnectAsync()
    {
        var bus = _knxBus;
        if (bus is null) return;
        _knxBus = null;

        bus.GroupMessageReceived -= OnGroupMessageReceived;
        var wasConnected = bus.ConnectionState == BusConnectionState.Connected;
        try
        {
            await bus.DisposeAsync();
            if (wasConnected) _logger.LogInformation("Disconnected from {host}", _options.Host);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while disconnecting from {host}", _options.Host);
        }
    }

    private async void OnGroupMessageReceived(object? sender, GroupEventArgs e)
    {
        try
        {
            if (e.EventType != GroupEventType.ValueRead) return;
            if (_capabilityByAddress.TryGetValue(e.DestinationAddress, out var capability) is false) return;
            if (_pending.Latest(capability) is not { } value) return;
            if (sender is not KnxBus { ConnectionState: BusConnectionState.Connected } bus) return;

            var groupValue = new KnxTelegramValue(e.DestinationAddress) { Value = value.Value, IsBit = value.IsBit }.ToGroupValue()!;

            using var timeout = new CancellationTokenSource(WriteTimeout);
            await bus.RespondGroupValueAsync(e.DestinationAddress, groupValue, MessagePriority.Low, timeout.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("KNX read response to {address} failed: {message}", e.DestinationAddress, ex.Message);
        }
    }
}
