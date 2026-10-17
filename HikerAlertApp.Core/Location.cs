namespace HikerAlertApp.Core;

public sealed record Location(
    double Latitude,
    double Longitude,
    DateTimeOffset RecordedAt);
