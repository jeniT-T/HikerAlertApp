using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HikerAlertApp.Core;

public interface ILastKnownLocationStore
{
    Task SaveAsync(Location location, CancellationToken cancellationToken = default);
    Task<Location?> GetLastKnownAsync(CancellationToken cancellationToken = default);
}

public sealed class LocationTrackingService(ILastKnownLocationStore store)
{
    public Task SaveLocationAsync(
        Location location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (location.Latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(location), "Latitude must be between -90 and 90.");

        if (location.Longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(location), "Longitude must be between -180 and 180.");

        return store.SaveAsync(location, cancellationToken);
    }
}
