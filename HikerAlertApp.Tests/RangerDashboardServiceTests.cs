using System;
using System.Collections.Generic;
using System.Text;
using HikerAlertApp.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HikerAlertApp.Tests;

[TestClass]
public class RangerDashboardServiceTests
{
    
    [TestMethod]
    public async Task GetDashboardAsync_LoadsProfileLocationsAndAlertsForSignedInRanger()
    {
        var rangerId = "ranger-42";
        var profile = new RangerProfile(rangerId, "Taylor Ranger", "B-104", "North District");
        var location = new Location(
            47.6062, -122.3321, DateTimeOffset.Parse("2026-10-09T10:00:00Z"));
        var hikerLocations = new[]
        {
            new RangerHikerLocation("hiker-7", "Alex Hiker", location)
        };
        var alerts = new[]
        {
            new RangerAlertHistoryItem(
                "alert-9", "hiker-7", EmergencyLevel.Injured,
                DateTimeOffset.Parse("2026-10-09T10:05:00Z"), location, "Ankle injury")
        };

        var repository = new FakeDashboardRepository(profile, hikerLocations, alerts);
        var service = new RangerDashboardService(
            new FakeRangerIdentity(rangerId),
            repository);

        var dashboard = await service.GetDashboardAsync();

        Assert.IsNotNull(dashboard);
        Assert.AreEqual(profile, dashboard.Profile);
        Assert.AreEqual(hikerLocations[0], dashboard.HikerLocations[0]);
        Assert.AreEqual(alerts[0], dashboard.AlertHistory[0]);
        CollectionAssert.AreEqual(
            new[] { rangerId, rangerId, rangerId },
            repository.QueriedRangerIds.ToArray());
    }

    [TestMethod]
    public async Task GetDashboardAsync_WithoutRangerSession_RejectsAccess()
    {
        var repository = new FakeDashboardRepository(
            null, Array.Empty<RangerHikerLocation>(), Array.Empty<RangerAlertHistoryItem>());
        var service = new RangerDashboardService(
            new FakeRangerIdentity(null),
            repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDashboardAsync());

        Assert.AreEqual(0, repository.QueriedRangerIds.Count);
    }

    private sealed class FakeRangerIdentity(string? rangerId) : ICurrentRangerIdentity
    {
        public string? RangerId => rangerId;
    }

    private sealed class FakeDashboardRepository(
        RangerProfile? profile,
        IReadOnlyList<RangerHikerLocation> locations,
        IReadOnlyList<RangerAlertHistoryItem> alerts) : IRangerDashboardRepository
    {
        public List<string> QueriedRangerIds { get; } = [];

        public Task<RangerProfile?> GetProfileAsync(
            string rangerId,
            CancellationToken cancellationToken = default)
        {
            QueriedRangerIds.Add(rangerId);
            return Task.FromResult(profile);
        }

        public Task<IReadOnlyList<RangerHikerLocation>> GetHikerLocationsAsync(
            string rangerId,
            CancellationToken cancellationToken = default)
        {
            QueriedRangerIds.Add(rangerId);
            return Task.FromResult(locations);
        }

        public Task<IReadOnlyList<RangerAlertHistoryItem>> GetAlertHistoryAsync(
            string rangerId,
            CancellationToken cancellationToken = default)
        {
            QueriedRangerIds.Add(rangerId);
            return Task.FromResult(alerts);
        }
    } 
}
