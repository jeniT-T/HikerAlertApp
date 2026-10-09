using System;
using System.Collections.Generic;
using System.Text;
using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HikerAlertApp.Tests;

[TestClass]
public class LocationTrackingServiceTests
{
    [TestMethod]
    public async Task SaveLocationAsync_WhileOffline_PersistsLatestLocation()
    {
        // The fake represents local storage; this test does not need a network.
        var store = new InMemoryLocationStore();
        var service = new LocationTrackingService(store);

        var firstLocation = new Location(
            47.6062, -122.3321, DateTimeOffset.Parse("2026-10-09T10:00:00Z"));
        var latestLocation = new Location(
            47.6205, -122.3493, DateTimeOffset.Parse("2026-10-09T10:05:00Z"));

        // Simulate two GPS updates during a hike.
        await service.SaveLocationAsync(firstLocation);
        await service.SaveLocationAsync(latestLocation);

        Assert.AreEqual(2, store.SavedLocations.Count);
        Assert.AreEqual(latestLocation, await store.GetLastKnownAsync());
    }

    private sealed class InMemoryLocationStore : ILastKnownLocationStore
    {
        public List<Location> SavedLocations { get; } = [];

        public Task SaveAsync(
            Location location,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SavedLocations.Add(location);
            return Task.CompletedTask;
        }

        public Task<Location?> GetLastKnownAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(SavedLocations.LastOrDefault());
        }
    }
}
