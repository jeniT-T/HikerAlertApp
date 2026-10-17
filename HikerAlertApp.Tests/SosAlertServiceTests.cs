using HikerAlertApp.Core;

namespace HikerAlertApp.Tests;

[TestClass]
public class SosAlertServiceTests
{
    [TestMethod]
    public async Task CreateAsync_ReturnsAlertWithInputAndCapturedLocation()
    {
        var location = new SosLocation(47.6062, -122.3321);
        var locationProvider = new TestLocationProvider(location);
        var service = new SosAlertService(locationProvider);
        var input = new SosAlertInput(EmergencyLevel.High, "Injured ankle on the trail");
        var beforeCreation = DateTimeOffset.UtcNow;

        var alert = await service.CreateAsync(input);

        Assert.AreEqual(input.EmergencyLevel, alert.EmergencyLevel);
        Assert.AreEqual(input.Notes, alert.Notes);
        Assert.AreEqual(location, alert.Location);
        Assert.AreEqual(1, locationProvider.CallCount);
        Assert.AreEqual(TimeSpan.Zero, alert.SentAtUtc.Offset);
        Assert.IsTrue(alert.SentAtUtc >= beforeCreation);
        Assert.IsTrue(alert.SentAtUtc <= DateTimeOffset.UtcNow);
    }

    [TestMethod]
    public async Task CreateAsync_RejectsInvalidEmergencyLevelWithoutGettingLocation()
    {
        var locationProvider = new TestLocationProvider(new SosLocation(47.6062, -122.3321));
        var service = new SosAlertService(locationProvider);
        var input = new SosAlertInput((EmergencyLevel)999, "Emergency notes");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateAsync(input));

        Assert.AreEqual(0, locationProvider.CallCount);
    }

    [TestMethod]
    public async Task CreateAsync_CreatesAlertWhenNotesAreOmitted()
    {
        var location = new SosLocation(47.6062, -122.3321);
        var locationProvider = new TestLocationProvider(location);
        var service = new SosAlertService(locationProvider);
        var input = new SosAlertInput(EmergencyLevel.High);

        var alert = await service.CreateAsync(input);

        Assert.IsNull(alert.Notes);
        Assert.AreEqual(location, alert.Location);
        Assert.AreEqual(1, locationProvider.CallCount);
    }

    private sealed class TestLocationProvider(SosLocation location) : ILocationProvider
    {
        public int CallCount { get; private set; }

        public Task<SosLocation> GetCurrentLocationAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(location);
        }
    }
}
