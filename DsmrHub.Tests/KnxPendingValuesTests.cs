using DsmrHub.Infrastructure.Knx;
using Xunit;

namespace DsmrHub.Tests;

public class KnxPendingValuesTests
{
    private static byte[] F(float value) => BitConverter.GetBytes(value);

    [Fact]
    public void NewerValue_ReplacesOutdatedPendingValue()
    {
        var store = new KnxPendingValues();

        store.Set("PowerDelivered", F(100), false);
        store.Set("PowerDelivered", F(200), false);

        Assert.True(store.TryTakeNext(out var value));
        Assert.Equal(F(200), value!.Value);
        Assert.False(store.TryTakeNext(out _));
    }

    [Fact]
    public void ValueEqualToLastSent_IsNotQueued()
    {
        var store = new KnxPendingValues();
        store.Set("PowerDelivered", F(100), false);
        store.TryTakeNext(out var value);
        store.MarkSent(value!);

        Assert.False(store.Set("PowerDelivered", F(100), false));
        Assert.False(store.HasPending);
    }

    [Fact]
    public void ReturningToLastSentValue_DropsOutdatedPendingValue()
    {
        var store = new KnxPendingValues();
        store.Set("PowerDelivered", F(100), false);
        store.TryTakeNext(out var value);
        store.MarkSent(value!);

        store.Set("PowerDelivered", F(200), false);
        store.Set("PowerDelivered", F(100), false);

        Assert.False(store.HasPending);
    }

    [Fact]
    public void ValueSetWhileAnotherIsBeingSent_IsTakenNextInNewestForm()
    {
        var store = new KnxPendingValues();
        store.Set("PowerDelivered", F(100), false);
        store.Set("CurrentL1", F(1), false);

        Assert.True(store.TryTakeNext(out var first));

        // While "first" is on its way, newer readings arrive for both capabilities.
        store.Set("PowerDelivered", F(300), false);
        store.Set("CurrentL1", F(3), false);

        var remaining = new Dictionary<string, byte[]>();
        while (store.TryTakeNext(out var next)) remaining[next!.Capability] = next.Value;

        Assert.Equal(F(3), remaining["CurrentL1"]);
        if (first!.Capability == "PowerDelivered") Assert.Equal(F(300), remaining["PowerDelivered"]);
        Assert.Equal(2, remaining.Count);
    }

    [Fact]
    public void Requeue_DoesNotOverrideNewerValue()
    {
        var store = new KnxPendingValues();
        store.Set("PowerDelivered", F(100), false);
        store.TryTakeNext(out var failed);

        store.Set("PowerDelivered", F(200), false);
        store.Requeue(failed!);

        Assert.True(store.TryTakeNext(out var value));
        Assert.Equal(F(200), value!.Value);
        Assert.False(store.TryTakeNext(out _));
    }

    [Fact]
    public void Requeue_WithoutNewerValue_MakesValuePendingAgain()
    {
        var store = new KnxPendingValues();
        store.Set("ElectricityTariff", BitConverter.GetBytes(true), true);
        store.TryTakeNext(out var failed);

        store.Requeue(failed!);

        Assert.True(store.TryTakeNext(out var value));
        Assert.Equal(failed, value);
    }

    [Fact]
    public void Latest_PrefersPendingOverLastSent()
    {
        var store = new KnxPendingValues();
        store.Set("PowerDelivered", F(100), false);
        store.TryTakeNext(out var sent);
        store.MarkSent(sent!);
        Assert.Equal(F(100), store.Latest("PowerDelivered")!.Value);

        store.Set("PowerDelivered", F(200), false);
        Assert.Equal(F(200), store.Latest("PowerDelivered")!.Value);
        Assert.Null(store.Latest("CurrentL1"));
    }
}
