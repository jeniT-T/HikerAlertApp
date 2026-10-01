using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Devices.Sensors;

namespace HikerAlert;

public partial class MainPage : ContentPage
{
    private const double AucklandLatitude = -36.8485;
    private const double AucklandLongitude = 174.7633;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private bool _hasAttemptedInitialLoad;
    private bool _isRefreshing;

    static MainPage()
    {
        // Nominatim requires an identifying User-Agent.
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HikerAlert/1.0");
    }

    public MainPage()
    {
        InitializeComponent();

        // Show Auckland while waiting for permission and device location.
        MapWebView.Source = CreateMapSource();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAttemptedInitialLoad)
        {
            return;
        }

        _hasAttemptedInitialLoad = true;
        await RefreshLocationAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await RefreshLocationAsync();
    }

    private async Task RefreshLocationAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        RefreshButton.IsEnabled = false;

        const double latitude = AucklandLatitude;
        const double longitude = AucklandLongitude;

        StreetLabel.Text = "Finding an address in Auckland...";
        CoordinatesLabel.Text = "Auckland, New Zealand · -36.84850, 174.76330";
        WeatherLabel.Text = "Loading Auckland weather...";
        MapStatusLabel.Text = "Showing Auckland, New Zealand.";

        try
        {
            MapWebView.Source = CreateMapSource(latitude, longitude);

            var addressTask = GetStreetAddressAsync(latitude, longitude);
            var weatherTask = GetWeatherAsync(latitude, longitude);

            await Task.WhenAll(addressTask, weatherTask);

            StreetLabel.Text = AddAucklandRegion(await addressTask);
            WeatherLabel.Text = await weatherTask;
        }
        catch
        {
            StreetLabel.Text = "Auckland, New Zealand";
            WeatherLabel.Text = "Weather or street address unavailable.";
            MapStatusLabel.Text = "Showing Auckland, New Zealand.";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            _isRefreshing = false;
        }
    }

    private static string AddAucklandRegion(string streetAddress)
    {
        if (streetAddress == "Street address unavailable")
        {
            return "Auckland, New Zealand";
        }

        var parts = streetAddress
            .Split(',')
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        if (!parts.Any(part =>
                part.Contains("Auckland", StringComparison.OrdinalIgnoreCase)))
        {
            parts.Add("Auckland");
        }

        if (!parts.Any(part =>
                part.Contains("New Zealand", StringComparison.OrdinalIgnoreCase)))
        {
            parts.Add("New Zealand");
        }

        return string.Join(", ", parts);
    }

    private void ShowLocationUnavailable(string message)
    {
        StreetLabel.Text = "Location unavailable";
        CoordinatesLabel.Text = message;
        WeatherLabel.Text = "Weather unavailable until location is available.";
        MapStatusLabel.Text = "Showing Auckland. Enable location access and refresh to show your position.";
        MapWebView.Source = CreateMapSource();
    }

    private static async Task<string> GetStreetAddressAsync(
        double latitude,
        double longitude)
    {
        try
        {
            var lat = latitude.ToString(CultureInfo.InvariantCulture);
            var lon = longitude.ToString(CultureInfo.InvariantCulture);
            var url =
                $"https://nominatim.openstreetmap.org/reverse" +
                $"?format=jsonv2&lat={lat}&lon={lon}&zoom=18&addressdetails=1";

            using var response = await HttpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);

            if (!document.RootElement.TryGetProperty("address", out var address))
            {
                return "Street address unavailable";
            }

            string? GetAddressPart(params string[] names)
            {
                foreach (var name in names)
                {
                    if (address.TryGetProperty(name, out var value) &&
                        value.ValueKind == JsonValueKind.String)
                    {
                        var text = value.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }

                return null;
            }

            var houseNumber = GetAddressPart("house_number");
            var road = GetAddressPart("road", "pedestrian", "footway", "path", "residential");
            var locality = GetAddressPart(
                "suburb",
                "neighbourhood",
                "city_district",
                "city",
                "town",
                "village");

            var street = string.Join(
                " ",
                new[] { houseNumber, road }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));

            if (!string.IsNullOrWhiteSpace(street) &&
                !string.IsNullOrWhiteSpace(locality))
            {
                return $"{street}, {locality}";
            }

            if (!string.IsNullOrWhiteSpace(street))
            {
                return street;
            }

            return locality ?? "Street address unavailable";
        }
        catch
        {
            return "Street address unavailable";
        }
    }

    private static async Task<string> GetWeatherAsync(
        double latitude,
        double longitude)
    {
        try
        {
            var lat = latitude.ToString(CultureInfo.InvariantCulture);
            var lon = longitude.ToString(CultureInfo.InvariantCulture);

            var url =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={lat}&longitude={lon}" +
                "&current=temperature_2m,weather_code" +
                "&timezone=auto";

            var weather = await HttpClient.GetFromJsonAsync<WeatherResponse>(url);
            if (weather?.Current is null)
            {
                return "Weather unavailable";
            }

            var temperature = weather.Current.TemperatureCelsius
                .ToString("0.#", CultureInfo.InvariantCulture);

            return $"{temperature}°C · {DescribeWeather(weather.Current.WeatherCode)}";
        }
        catch
        {
            return "Weather unavailable";
        }
    }

    private static string DescribeWeather(int code) => code switch
    {
        0 => "Clear",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Fog",
        51 or 53 or 55 => "Drizzle",
        56 or 57 => "Freezing drizzle",
        61 or 63 or 65 => "Rain",
        66 or 67 => "Freezing rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Rain showers",
        85 or 86 => "Snow showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Current conditions"
    };

    private static HtmlWebViewSource CreateMapSource(
        double? latitude = null,
        double? longitude = null)
    {
        var hasLocation = latitude.HasValue && longitude.HasValue;

        var centerLatitude = (latitude ?? AucklandLatitude)
            .ToString(CultureInfo.InvariantCulture);
        var centerLongitude = (longitude ?? AucklandLongitude)
            .ToString(CultureInfo.InvariantCulture);
        var zoom = hasLocation ? 15 : 12;

        var markerScript = hasLocation
            ? $$"""
                L.circleMarker([{{centerLatitude}}, {{centerLongitude}}], {
                    radius: 9,
                    color: "#ffffff",
                    weight: 3,
                    fillColor: "#1687ff",
                    fillOpacity: 1
                }).addTo(map).bindPopup("Your current location");
                """
            : string.Empty;

        var html = $$"""
            <!doctype html>
            <html>
            <head>
                <meta name="viewport"
                      content="width=device-width, initial-scale=1.0, maximum-scale=1.0">
                <link rel="stylesheet"
                      href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
                <style>
                    html, body, #map {
                        width: 100%;
                        height: 100%;
                        margin: 0;
                        padding: 0;
                    }
                </style>
            </head>
            <body>
                <div id="map"></div>
                <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
                <script>
                    const map = L.map("map").setView(
                        [{{centerLatitude}}, {{centerLongitude}}],
                        {{zoom}});

                    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
                        maxZoom: 19,
                        attribution: "&copy; OpenStreetMap contributors"
                    }).addTo(map);

                    {{markerScript}}
                </script>
            </body>
            </html>
            """;

        return new HtmlWebViewSource { Html = html };
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
