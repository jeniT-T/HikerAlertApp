namespace HikerAlertApp.Core;

public enum EmergencyLevel
{
    Low,
    Moderate,
    High,
    Critical
}

public sealed record SosAlertInput(
    EmergencyLevel EmergencyLevel,
    string? Notes = null,
    string HikerId ="");

public sealed record SosLocation(
    double Latitude,
    double Longitude);

public sealed record SosAlert(
    EmergencyLevel EmergencyLevel,
    string? Notes,
    SosLocation Location,
    DateTimeOffset SentAtUtc,
    string HikerId = "");

public interface ILocationProvider
{
    Task<SosLocation> GetCurrentLocationAsync(
        CancellationToken cancellationToken = default);
}
