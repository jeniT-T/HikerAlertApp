using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HikerAlertApp.Core;

public enum EmergencyLevel
{
    Lost,
    Injured,
    LifeThreatening,
    Other
}

public sealed record WeatherSummary(string Description, double TemperatureCelsius);

public sealed record SosAlert(
    EmergencyLevel Emergency,
    string? Notes,
    Location Location,
    WeatherSummary? Weather);

public interface ICurrentLocationProvider
{
    Task<Location> GetCurrentAsync(CancellationToken cancellationToken = default);
}


public interface IWeatherProvider
{
    Task<WeatherSummary?> GetCurrentAsync(
        Location location,
        CancellationToken cancellationToken = default);
}

public interface ISosAlertSender
{
    Task SendAsync(SosAlert alert, CancellationToken cancellationToken = default);
}

public interface ISosCountdownDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
}

public sealed class SosAlertService(
    ICurrentLocationProvider locationProvider,
    ILastKnownLocationStore locationStore,
    IWeatherProvider weatherProvider,
    ISosAlertSender sender,
    ISosCountdownDelay countdownDelay)
{
    public async Task<SosAlert> PrepareAsync(
        EmergencyLevel emergency,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(emergency))
            throw new ArgumentOutOfRangeException(nameof(emergency));

        var location = await locationProvider.GetCurrentAsync(cancellationToken);
        await locationStore.SaveAsync(location, cancellationToken);

        WeatherSummary? weather;
        try
        {
            weather = await weatherProvider.GetCurrentAsync(location, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Weather is helpful, but a weather-service failure must not block an SOS.
            weather = null;
        }

        return new SosAlert(
            emergency,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            location,
            weather);
    }

    public async Task SendAfterCountdownAsync(
        SosAlert alert,
        IProgress<int>? countdown = null,
        CancellationToken cancellationToken = default)
    {
        for (var secondsRemaining = 10; secondsRemaining > 0; secondsRemaining--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            countdown?.Report(secondsRemaining);

            await countdownDelay.DelayAsync(
                TimeSpan.FromSeconds(1),
                cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await sender.SendAsync(alert, cancellationToken);
    }
}
