using System.Globalization;
using System.Text.Json;
using Microsoft.Maui.Devices.Sensors;

namespace HikerAlert;

public sealed record NearbyTrail(string Name, Location Location);

public sealed class NearbyTrailService
{
    public const int SearchRadiusMeters = 5_000;
    private const int MaximumTrails = 10;

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<IReadOnlyList<NearbyTrail>> FindNearbyTrailsAsync(
        Location userLocation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userLocation);

        var latitude = userLocation.Latitude.ToString(CultureInfo.InvariantCulture);
        var longitude = userLocation.Longitude.ToString(CultureInfo.InvariantCulture);
        var query = $"""
            [out:json][timeout:20];
            (
              relation(around:{SearchRadiusMeters},{latitude},{longitude})["route"="hiking"]["name"];
              way(around:{SearchRadiusMeters},{latitude},{longitude})["highway"~"path|footway|track"]["name"];
            );
            out center tags 50;
            """;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["data"] = query
        });
        using var response = await HttpClient.PostAsync(
            "https://overpass-api.de/api/interpreter",
            content,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("elements", out var elements))
        {
            return Array.Empty<NearbyTrail>();
        }

        var trails = new List<NearbyTrail>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var element in elements.EnumerateArray())
        {
            if (!element.TryGetProperty("tags", out var tags) ||
                !tags.TryGetProperty("name", out var nameElement))
            {
                continue;
            }

            var name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
            {
                continue;
            }

            var position = element.TryGetProperty("center", out var center) ? center : element;
            if (!position.TryGetProperty("lat", out var latitudeElement) ||
                !position.TryGetProperty("lon", out var longitudeElement))
            {
                continue;
            }

            trails.Add(new NearbyTrail(
                name,
                new Location(latitudeElement.GetDouble(), longitudeElement.GetDouble())));
        }

        return trails
            .OrderBy(trail => userLocation.CalculateDistance(trail.Location, DistanceUnits.Kilometers))
            .Take(MaximumTrails)
            .ToArray();
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HikerAlertApp/1.0");
        return client;
    }
}
