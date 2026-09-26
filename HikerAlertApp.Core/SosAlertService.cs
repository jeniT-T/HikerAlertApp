namespace HikerAlertApp.Core;

public sealed class SosAlertService
{
    private readonly ILocationProvider _locationProvider;

    public SosAlertService(ILocationProvider locationProvider)
    {
        _locationProvider = locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));
    }

    public async Task<SosAlert> CreateAsync(
        SosAlertInput input,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!Enum.IsDefined(input.EmergencyLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Emergency level is not valid.");
        }

        var location = await _locationProvider.GetCurrentLocationAsync(cancellationToken);

        if (location is null ||
            !double.IsFinite(location.Latitude) ||
            location.Latitude is < -90 or > 90 ||
            !double.IsFinite(location.Longitude) ||
            location.Longitude is < -180 or > 180)
        {
            throw new InvalidOperationException("The location provider returned invalid coordinates.");
        }

        return new SosAlert(
            input.EmergencyLevel,
            input.Notes,
            location,
            DateTimeOffset.UtcNow);
    }
}
