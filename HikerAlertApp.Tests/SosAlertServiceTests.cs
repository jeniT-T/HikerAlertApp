using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HikerAlertApp.Tests;

[TestClass]
public class SosAlertServiceTests
{
    
    [TestMethod]
    public async Task PrepareAndSend_SavesLocationShowsDetailsAndSendsAfterCountdown()
    {
        var harness = CreateHarness(new ImmediateCountdownDelay());
        var alert = await harness.Service.PrepareAsync(
            EmergencyLevel.Injured,
            "Twisted ankle near the trail.");

        Assert.AreEqual(alert.Location, harness.LocationStore.LastSaved);
        Assert.AreEqual(EmergencyLevel.Injured, alert.Emergency);
        Assert.AreEqual("Twisted ankle near the trail.", alert.Notes);
        Assert.AreEqual("Cloudy", alert.Weather!.Description);

        var countdown = new RecordingProgress();
        await harness.Service.SendAfterCountdownAsync(alert, countdown);

        CollectionAssert.AreEqual(
            Enumerable.Range(1, 10).Reverse().ToArray(),
            countdown.Values.ToArray());
        Assert.AreEqual(1, harness.Sender.SentAlerts.Count);
        Assert.AreEqual(alert, harness.Sender.SentAlerts[0]);
    }

    [TestMethod]
    public async Task SendAfterCountdown_WhenUserCancels_DoesNotSendAlert()
    {
        var delay = new BlockingCountdownDelay();
        var harness = CreateHarness(delay);
        var alert = await harness.Service.PrepareAsync(
            EmergencyLevel.LifeThreatening,
            null);

        using var cancellation = new CancellationTokenSource();
        var sendTask = harness.Service.SendAfterCountdownAsync(
            alert,
            cancellationToken: cancellation.Token);

        // Wait until the countdown has started, then simulate tapping Cancel.
        await delay.DelayStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        cancellation.Cancel();

        // Use the async Assert helper for Task-returning code.
        await Assert.ThrowsAsync<TaskCanceledException>(() => sendTask);
        Assert.AreEqual(0, harness.Sender.SentAlerts.Count);
    }

    private static TestHarness CreateHarness(ISosCountdownDelay delay)
    {
        var location = new Location(
            47.6062,
            -122.3321,
            DateTimeOffset.Parse("2026-10-09T10:00:00Z"));
        var locationStore = new InMemoryLocationStore();
        var sender = new RecordingSender();

        var service = new SosAlertService(
            new StubLocationProvider(location),
            locationStore,
            new StubWeatherProvider(),
            sender,
            delay);

        return new TestHarness(service, locationStore, sender);
    }

    private sealed record TestHarness(
        SosAlertService Service,
        InMemoryLocationStore LocationStore,
        RecordingSender Sender);

    private sealed class StubLocationProvider(Location location) : ICurrentLocationProvider
    {
        public Task<Location> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(location);
    }

    private sealed class InMemoryLocationStore : ILastKnownLocationStore
    {
        public Location? LastSaved { get; private set; }

        public Task SaveAsync(Location location, CancellationToken cancellationToken = default)
        {
            LastSaved = location;
            return Task.CompletedTask;
        }

        public Task<Location?> GetLastKnownAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LastSaved);
        }
    }

    private sealed class StubWeatherProvider : IWeatherProvider
    {
        public Task<WeatherSummary?> GetCurrentAsync(
            Location location,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WeatherSummary?>(
                new WeatherSummary("Cloudy", 12.5));
    }

    private sealed class RecordingSender : ISosAlertSender
    {
        public List<SosAlert> SentAlerts { get; } = [];

        public Task SendAsync(SosAlert alert, CancellationToken cancellationToken = default)
        {
            SentAlerts.Add(alert);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];

        public void Report(int value) => Values.Add(value);
    }

    private sealed class ImmediateCountdownDelay : ISosCountdownDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class BlockingCountdownDelay : ISosCountdownDelay
    {
        public TaskCompletionSource DelayStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            DelayStarted.TrySetResult();
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    } 
}
