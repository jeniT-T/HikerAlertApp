using HikerAlertApp.Core;

namespace HikerAlertApp.Tests;

[TestClass]
public sealed class NearbyTrailViewModelTests
{
    [TestMethod]
    public async Task LoadAsync_ExposesNearbyTrailImageLocationAndWeather()
    {
        var updatedAt = DateTimeOffset.UtcNow;
        var expectedTrail = new NearbyTrail(
            Name: "Pine Ridge Loop",
            ImageUrl: "https://example.test/pine-ridge.jpg",
            Location: "Pine Ridge Park",
            Weather: "16°C, light rain",
            UpdatedAtUtc: updatedAt);

        var viewModel = new NearbyTrailViewModel(
            new FakeNearbyTrailService(expectedTrail));

        await viewModel.LoadAsync();

        Assert.IsNotNull(viewModel.Trail);
        Assert.AreEqual(expectedTrail.Name, viewModel.Trail.Name);
        Assert.AreEqual(expectedTrail.ImageUrl, viewModel.Trail.ImageUrl);
        Assert.AreEqual(expectedTrail.Location, viewModel.Trail.Location);
        Assert.AreEqual(expectedTrail.Weather, viewModel.Trail.Weather);
        Assert.AreEqual(expectedTrail.UpdatedAtUtc, viewModel.Trail.UpdatedAtUtc);
    }

    private sealed class FakeNearbyTrailService(NearbyTrail trail)
        : INearbyTrailService
    {
        public Task<NearbyTrail?> GetNearbyTrailAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<NearbyTrail?>(trail);
    }
}