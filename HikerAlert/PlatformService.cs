
using HikerAlertApp.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace HikerAlert;

/// Reports whether the device currently has Internet access (NFR01).
public sealed class MauiConnectivityProvider : IConnectivityProvider
{
    public bool HasInternetConnection
        => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}

///Speed-dials emergency services through the platform dialer (FR11).
public sealed class MauiEmergencyDialer : IEmergencyDialer
{
    public Task<bool> DialAsync(string dialEmergencyNumber, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dialEmergencyNumber))
        {
            return Task.FromResult(false);
        }

        if (!PhoneDialer.Default.IsSupported)
        {
            return Task.FromResult(false);
        }

        PhoneDialer.Default.Open(dialEmergencyNumber);
        return Task.FromResult(true);
    }
}



/// Supplies a location fix for SOS alerts, falling back to the last known fix
/// when the platform cannot produce a fresh one (FR03).

public sealed class MauiLocationProvider : ILocationProvider
{
    private SosLocation? _lastKnown;

    public async Task<SosLocation> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GeolocationRequest(
                GeolocationAccuracy.High,
                TimeSpan.FromSeconds(5));

            var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);

            if (location is not null)
            {
                _lastKnown = new SosLocation(location.Latitude, location.Longitude);
                return _lastKnown;
            }
        }
        catch (Exception ex) when (ex is PermissionException or FeatureNotSupportedException)
        {
            // Fall through to the cached fix below.
        }

        return _lastKnown
            ?? throw new InvalidOperationException(
                "Location is unavailable and no previous fix has been recorded.");
    }
}


/// The platform half of the alert relay: resolves the ranger endpoint from
/// device preferences, seeded from the build configuration.

public sealed class PreferenceSosEndpointProvider : ISosEndpointProvider
{
    public const string EndpointPreferenceKey = "RangerSosEndpoint";

    public PreferenceSosEndpointProvider()
    {
        var stored = Preferences.Default.Get(EndpointPreferenceKey, string.Empty);

        if (string.IsNullOrWhiteSpace(stored))
        {
            Preferences.Default.Set(EndpointPreferenceKey, Constants.RangerSosEndpoint);
        }
    }

    public Uri? GetEndpoint()
    {
        var stored = Preferences.Default.Get(EndpointPreferenceKey, string.Empty);

        return Uri.TryCreate(stored, UriKind.Absolute, out var endpoint)
               && endpoint.Scheme == Uri.UriSchemeHttps
            ? endpoint
            : null;
    }
}
