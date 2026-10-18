using HikerAlertApp.Core;

namespace HikerAlertApp.Tests;

[TestClass]
public class SosDispatchServiceTests
{
    private static readonly SosLocation TestLocation = new(47.6062, -122.3321);

    [TestMethod]
    public async Task SubmitAsync_DeliversAndClearsQueue_WhenOnline()
    {
        var queue = new InMemorySosAlertQueue();
        var relay = new StubRelay(delivers: true);
        var service = CreateService(queue, relay, online: true);

        var result = await service.SubmitAsync(new SosAlertInput(EmergencyLevel.High, "Injured"));

        Assert.IsTrue(result.DeliveredImmediately);
        Assert.IsFalse(result.QueuedForRetry);
        Assert.AreEqual("stub", result.Channel);
        Assert.AreEqual(0, await queue.CountAsync());
    }

    [TestMethod]
    public async Task SubmitAsync_QueuesAlert_WhenOffline()
    {
        var queue = new InMemorySosAlertQueue();
        var relay = new StubRelay(delivers: true);
        var service = CreateService(queue, relay, online: false);

        var result = await service.SubmitAsync(new SosAlertInput(EmergencyLevel.High, "Injured"));

        Assert.IsFalse(result.DeliveredImmediately);
        Assert.IsTrue(result.QueuedForRetry);
        Assert.AreEqual("queued-on-device", result.Channel);
        Assert.AreEqual(1, await queue.CountAsync());
        Assert.AreEqual(0, relay.CallCount);
    }

    [TestMethod]
    public async Task SubmitAsync_QueuesAlert_WhenRelayFails()
    {
        var queue = new InMemorySosAlertQueue();
        var relay = new StubRelay(delivers: false);
        var service = CreateService(queue, relay, online: true);

        var result = await service.SubmitAsync(new SosAlertInput(EmergencyLevel.Critical, "Lost"));

        Assert.IsFalse(result.DeliveredImmediately);
        Assert.IsTrue(result.QueuedForRetry);
        Assert.AreEqual(1, await queue.CountAsync());
    }

    [TestMethod]
    public async Task FlushPendingAsync_DeliversQueuedAlerts_AndEmptiesQueue()
    {
        var queue = new InMemorySosAlertQueue();
        var relay = new StubRelay(delivers: false);
        var service = CreateService(queue, relay, online: true);

        await service.SubmitAsync(new SosAlertInput(EmergencyLevel.High, "First"));
        await service.SubmitAsync(new SosAlertInput(EmergencyLevel.High, "Second"));
        Assert.AreEqual(2, await queue.CountAsync());

        relay.Delivers = true;
        var delivered = await service.FlushPendingAsync();

        Assert.AreEqual(2, delivered);
        Assert.AreEqual(0, await queue.CountAsync());
    }

    [TestMethod]
    public async Task FlushPendingAsync_DoesNothing_WhenOffline()
    {
        var queue = new InMemorySosAlertQueue();
        var relay = new StubRelay(delivers: true);
        var service = CreateService(queue, relay, online: false);

        await service.SubmitAsync(new SosAlertInput(EmergencyLevel.High, "Offline"));
        var delivered = await service.FlushPendingAsync();

        Assert.AreEqual(0, delivered);
        Assert.AreEqual(1, await queue.CountAsync());
    }

    private static SosDispatchService CreateService(
        ISosAlertQueue queue,
        IAlertRelay relay,
        bool online)
        => new(
            new SosAlertFactory(new StubLocationProvider()),
            queue,
            relay,
            new StubConnectivityProvider(online));

    private sealed class StubLocationProvider : ILocationProvider
    {
        public Task<SosLocation> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(TestLocation);
    }

    private sealed class StubConnectivityProvider(bool online) : IConnectivityProvider
    {
        public bool HasInternetConnection { get; set; } = online;
    }

    private sealed class StubRelay(bool delivers) : IAlertRelay
    {
        public bool Delivers { get; set; } = delivers;
        public int CallCount { get; private set; }
        public string ChannelName => "stub";

        public Task<bool> RelayAsync(SosAlert alert, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Delivers);
        }
    }
}
