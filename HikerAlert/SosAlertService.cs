using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;

namespace HikerAlert
{
    public sealed record SosAlert(
        Guid Id,
        string Urgency,
        DateTimeOffset SentAtUtc,
        double? Latitude,
        double? Longitude,
        string Weather);

    public static class SosAlertService
    {
        private const string EndpointPreferenceKey = "RangerSosEndpoint";
        private static readonly string QueuePath =
            Path.Combine(FileSystem.AppDataDirectory, "pending-sos-alerts.json");

        private static readonly SemaphoreSlim QueueLock = new(1, 1);
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        static SosAlertService()
        {
            Connectivity.Current.ConnectivityChanged += (_, args) =>
            {
                if (args.NetworkAccess == NetworkAccess.Internet)
                {
                    _ = FlushPendingAsync();
                }
            };
        }

        public static async Task<bool> SubmitAsync(string urgency)
        {
            var sentAt = DateTimeOffset.UtcNow;
            var (latitude, longitude) = await GetLocationAsync();

            var weather = "Unavailable";
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet &&
                latitude.HasValue && longitude.HasValue)
            {
                weather = await GetWeatherAsync(latitude.Value, longitude.Value);
            }

            var alert = new SosAlert(
                Guid.NewGuid(),
                urgency,
                sentAt,
                latitude,
                longitude,
                weather);

            await QueueLock.WaitAsync();
            try
            {
                var pending = await ReadQueueAsync();
                pending.Add(alert);
                await SaveQueueAsync(pending);
            }
            finally
            {
                QueueLock.Release();
            }

            return await TrySendPendingAsync(alert.Id);
        }

        public static async Task FlushPendingAsync()
        {
            await TrySendPendingAsync(null);
        }

        private static async Task<bool> TrySendPendingAsync(Guid? requestedAlertId)
        {
            var endpointText = Preferences.Default.Get(EndpointPreferenceKey, string.Empty);
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet ||
                !Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint) ||
                endpoint.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            await QueueLock.WaitAsync();
            try
            {
                var pending = await ReadQueueAsync();
                var requestedAlertSent = false;

                foreach (var alert in pending.ToArray())
                {
                    try
                    {
                        using var response = await HttpClient.PostAsJsonAsync(endpoint, alert);
                        if (!response.IsSuccessStatusCode)
                        {
                            break;
                        }

                        pending.Remove(alert);
                        await SaveQueueAsync(pending);

                        if (alert.Id == requestedAlertId)
                        {
                            requestedAlertSent = true;
                        }
                    }
                    catch
                    {
                        // Keep the alert in the local queue for a later retry.
                        break;
                    }
                }

                return requestedAlertSent;
            }
            finally
            {
                QueueLock.Release();
            }
        }

        private static async Task<(double? Latitude, double? Longitude)> GetLocationAsync()
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.High,
                    TimeSpan.FromSeconds(5));

                var location = await Geolocation.Default.GetLocationAsync(request);
                return location is null
                    ? (null, null)
                    : (location.Latitude, location.Longitude);
            }
            catch
            {
                return (null, null);
            }
        }

        private static async Task<string> GetWeatherAsync(double latitude, double longitude)
        {
            try
            {
                var url =
                    $"https://api.open-meteo.com/v1/forecast" +
                    $"?latitude={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                    $"&longitude={longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                    "&current=temperature_2m,weather_code&timezone=auto";

                var result = await HttpClient.GetFromJsonAsync<WeatherResponse>(url);
                return result?.Current is { } current
                    ? $"{current.TemperatureCelsius:0.#}°C, weather code {current.WeatherCode}"
                    : "Unavailable";
            }
            catch
            {
                return "Unavailable";
            }
        }

        private static async Task<List<SosAlert>> ReadQueueAsync()
        {
            if (!File.Exists(QueuePath))
            {
                return [];
            }

            var json = await File.ReadAllTextAsync(QueuePath);
            return JsonSerializer.Deserialize<List<SosAlert>>(json) ?? [];
        }

        private static async Task SaveQueueAsync(List<SosAlert> alerts)
        {
            var temporaryPath = QueuePath + ".tmp";
            var json = JsonSerializer.Serialize(alerts);
            await File.WriteAllTextAsync(temporaryPath, json);
            File.Move(temporaryPath, QueuePath, overwrite: true);
        }

        private sealed class WeatherResponse
        {
            [JsonPropertyName("current")]
            public CurrentWeather? Current { get; set; }
        }

        private sealed class CurrentWeather
        {
            [JsonPropertyName("temperature_2m")]
            public double TemperatureCelsius { get; set; }

            [JsonPropertyName("weather_code")]
            public int WeatherCode { get; set; }
        }
    }
}
