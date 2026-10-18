using HikerAlertApp.Core;

namespace HikerAlertApp.Tests;

[TestClass]
public class SosAlertFactoryTests
{
    [TestMethod]
    public async Task CreateAsync_ReturnsAlertWithInputAndCapturedLocation()
    {
        var location = new SosLocation(47.6062, -122.3321);
        var locationProvider = new TestLocationProvider(location);
        var factory = new SosAlertFactory(locationProvider);
        var input = new SosAlertInput(EmergencyLevel.High, "Injured ankle on the trail");
        var beforeCreation = DateTimeOffset.UtcNow;

        var alert = await factory.CreateAsync(input);

        Assert.AreEqual(input.EmergencyLevel, alert.EmergencyLevel);
        Assert.AreEqual(input.Notes, alert.Notes);
        Assert.AreEqual(location, alert.Location);
        Assert.AreEqual(1, locationProvider.CallCount);
        Assert.AreEqual(TimeSpan.Zero, alert.SentAtUtc.Offset);
        //Suggested to use IsGreaterThanOrEqualTo to avoid race conditions in the test
        Assert.IsGreaterThan(beforeCreation, alert.SentAtUtc);
        Assert.IsLessThan(DateTimeOffset.UtcNow, alert.SentAtUtc);
    }

    [TestMethod]
    public async Task CreateAsync_RejectsInvalidEmergencyLevelWithoutGettingLocation()
    {
        var locationProvider = new TestLocationProvider(new SosLocation(47.6062, -122.3321));
        var factory = new SosAlertFactory(locationProvider);
        var input = new SosAlertInput((EmergencyLevel)999, "Emergency notes");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => factory   .CreateAsync(input));

        Assert.AreEqual(0, locationProvider.CallCount);
    }

    [TestMethod]
    public async Task CreateAsync_CreatesAlertWhenNotesAreOmitted()
    {
        var location = new SosLocation(47.6062, -122.3321);
        var locationProvider = new TestLocationProvider(location);
        var factory = new SosAlertFactory(locationProvider);
        var input = new SosAlertInput(EmergencyLevel.High);

        var alert = await factory.CreateAsync(input);

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
