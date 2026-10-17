using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HikerAlertApp.Core;

// Implement this with ranger-only authentication and account storage.
public interface IRangerAuthenticationService
{
    Task<bool> SignInAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}

// Supplied by the authenticated ranger session.
public interface ICurrentRangerIdentity
{
    string? RangerId { get; }
}

public sealed record RangerProfile(
    string RangerId,
    string Name,
    string? BadgeNumber,
    string? Agency);

public sealed record RangerHikerLocation(
    string HikerId,
    string HikerName,
    Location? LastKnownLocation);

public sealed record RangerAlertHistoryItem(
    string AlertId,
    string HikerId,
    EmergencyLevel Emergency,
    DateTimeOffset CreatedAt,
    Location? Location,
    string? Notes);

public sealed record RangerDashboard(
    RangerProfile Profile,
    IReadOnlyList<RangerHikerLocation> HikerLocations,
    IReadOnlyList<RangerAlertHistoryItem> AlertHistory);

public interface IRangerDashboardRepository
{
    Task<RangerProfile?> GetProfileAsync(
        string rangerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RangerHikerLocation>> GetHikerLocationsAsync(
        string rangerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RangerAlertHistoryItem>> GetAlertHistoryAsync(
        string rangerId,
        CancellationToken cancellationToken = default);
}

public sealed class RangerDashboardService(
    ICurrentRangerIdentity currentRanger,
    IRangerDashboardRepository repository)
{
    public async Task<RangerDashboard?> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        var rangerId = currentRanger.RangerId;

        if (string.IsNullOrWhiteSpace(rangerId))
            throw new UnauthorizedAccessException("A ranger must be signed in.");

        var profile = await repository.GetProfileAsync(rangerId, cancellationToken);
        if (profile is null)
            return null;

        var locations = await repository.GetHikerLocationsAsync(rangerId, cancellationToken);
        var alerts = await repository.GetAlertHistoryAsync(rangerId, cancellationToken);

        return new RangerDashboard(profile, locations, alerts);
    }
}
