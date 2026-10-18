namespace HikerAlertApp.Core;

public sealed class SosAlertFactory
{
    private readonly ILocationProvider _locationProvider;
    private readonly IClock _clock;

    public SosAlertFactory(ILocationProvider locationProvider, IClock? clock = null)
    {
        _locationProvider = locationProvider
            ?? throw new ArgumentNullException(nameof(locationProvider));

        _clock = clock ?? SystemClock.Instance;
    }

    public async Task<SosAlert> CreateAsync(
        SosAlertInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!Enum.IsDefined(input.EmergencyLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                input.EmergencyLevel,
                $"'{input.EmergencyLevel}' is not a recognised emergency level.");
        }

        var location = await _locationProvider.GetCurrentLocationAsync(cancellationToken);

        if (location is null)
        {
            throw new InvalidOperationException(
                "A location fix is required before an SOS alert can be created.");
        }

        if (!IsValidLatitude(location.Latitude) || !IsValidLongitude(location.Longitude))
        {
            throw new InvalidOperationException(
                $"Location ({location.Latitude}, {location.Longitude}) is outside valid coordinate ranges.");
        }

        return new SosAlert(
            input.EmergencyLevel,
            input.Notes,
            location,
            _clock.UtcNow,
            input.HikerId);
    }

    private static bool IsValidLatitude(double latitude)
        => latitude is >= -90.0 and <= 90.0;

    private static bool IsValidLongitude(double longitude)
        => longitude is >= -180.0 and <= 180.0;
}
