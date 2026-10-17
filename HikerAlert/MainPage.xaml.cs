using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace HikerAlert;

public partial class MainPage : ContentPage
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private readonly NearbyTrailService _nearbyTrailService = new();
    private IReadOnlyList<NearbyTrail> _nearbyTrails = Array.Empty<NearbyTrail>();
    private Location? _currentLocation;
    private bool _isListening;
    private bool _isSearchingForTrails;
    private bool _isMapReady;
    private Location? _lastTrailLookupLocation;
    private DateTime _lastTrailLookupTime = DateTime.MinValue;

    static MainPage()
    {
        // Nominatim requires an identifying User-Agent.
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HikerAlert/1.0");
    }

    public MainPage()
    {
        InitializeComponent();
        MapWebView.Navigated += OnMapNavigated;
        _ = LoadMapPageAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartLocationTrackingAsync();
    }

    protected override void OnDisappearing()
    {
        StopLocationTracking();
        base.OnDisappearing();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        RefreshButton.IsEnabled = false;
        try
        {
            // Force a fresh lookup even if the user hasn't moved.
            _lastTrailLookupLocation = null;
            _lastTrailLookupTime = DateTime.MinValue;

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(10)));

            if (location is not null)
            {
                CenterMapOn(location);
                await LoadNearbyTrailsAsync(location);
            }
            else
            {
                MapStatusLabel.Text = "Could not get your location. Try again shortly.";
            }
        }
        catch (Exception)
        {
            MapStatusLabel.Text = "Could not refresh. Check location services and connection.";
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private async Task StartLocationTrackingAsync()
    {
        try
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (permission != PermissionStatus.Granted)
            {
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (permission != PermissionStatus.Granted)
            {
                StreetLabel.Text = "Location unavailable";
                MapStatusLabel.Text = "Location permission was not granted.";
                return;
            }

            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best)
            {
                MinimumTime = TimeSpan.FromSeconds(5)
            };

            Geolocation.Default.LocationChanged += OnLocationChanged;
            _isListening = await Geolocation.Default.StartListeningForegroundAsync(request);

            if (!_isListening)
            {
                Geolocation.Default.LocationChanged -= OnLocationChanged;
                MapStatusLabel.Text = "Could not start location updates.";
                return;
            }

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(10)));

            if (location is not null)
            {
                CenterMapOn(location);
                await LoadNearbyTrailsAsync(location);
            }
        }
        catch (FeatureNotEnabledException)
        {
            MapStatusLabel.Text = "Turn on device location services to use the map.";
        }
        catch (FeatureNotSupportedException)
        {
            MapStatusLabel.Text = "Location is not supported on this device.";
        }
        catch (PermissionException)
        {
            MapStatusLabel.Text = "The app does not have location permission.";
        }
        catch (Exception)
        {
            MapStatusLabel.Text = "Unable to get your location.";
        }
    }

    private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        var location = e.Location;
        if (location is null)
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentLocation = location;
            CoordinatesLabel.Text = FormatCoordinates(location);
            _ = RenderMapAsync();

            if (!_isSearchingForTrails)
            {
                MapStatusLabel.Text = "Live location enabled";
            }
        });

        if (ShouldRefreshTrailSearch(location))
        {
            _ = LoadNearbyTrailsAsync(location);
        }
    }

    private bool ShouldRefreshTrailSearch(Location location)
    {
        return _lastTrailLookupLocation is null ||
            location.CalculateDistance(_lastTrailLookupLocation, DistanceUnits.Kilometers) >= 1 ||
            DateTime.UtcNow - _lastTrailLookupTime >= TimeSpan.FromMinutes(5);
    }

    private async Task LoadNearbyTrailsAsync(Location userLocation)
    {
        if (_isSearchingForTrails || !ShouldRefreshTrailSearch(userLocation))
        {
            return;
        }

        _isSearchingForTrails = true;
        _lastTrailLookupLocation = userLocation;
        _lastTrailLookupTime = DateTime.UtcNow;

        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapStatusLabel.Text = "Searching for nearby hiking trails...";
            });

            var trails = await _nearbyTrailService.FindNearbyTrailsAsync(userLocation);
            _nearbyTrails = trails;
            await RenderMapAsync();

            // Refresh street address and weather for the new position.
            var addressTask = GetStreetAddressAsync(userLocation.Latitude, userLocation.Longitude);
            var weatherTask = GetWeatherAsync(userLocation.Latitude, userLocation.Longitude);
            await Task.WhenAll(addressTask, weatherTask);

            var address = await addressTask;
            var weather = await weatherTask;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                StreetLabel.Text = address;
                WeatherLabel.Text = weather;
                MapStatusLabel.Text = trails.Count == 0
                    ? "No named hiking trails found within 5 km."
                    : $"Found {trails.Count} nearby hiking trail(s).";
            });
        }
        catch (Exception)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapStatusLabel.Text = "Could not load nearby trails. Check your internet connection.";
            });
        }
        finally
        {
            _isSearchingForTrails = false;
        }
    }

    private static string FormatCoordinates(Location location) =>
        $"{location.Latitude.ToString("0.00000", CultureInfo.InvariantCulture)}, " +
        $"{location.Longitude.ToString("0.00000", CultureInfo.InvariantCulture)}";

    private void CenterMapOn(Location location)
    {
        _currentLocation = location;
        CoordinatesLabel.Text = FormatCoordinates(location);
        _ = RenderMapAsync();
    }

    private async Task LoadMapPageAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
            using var reader = new StreamReader(stream);
            MapWebView.Source = new HtmlWebViewSource
            {
                Html = await reader.ReadToEndAsync()
            };
        }
        catch (Exception)
        {
            MapStatusLabel.Text = "Could not load the map page.";
        }
    }

    private void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success)
        {
            MapStatusLabel.Text = "Could not open the map. Check your internet connection.";
            return;
        }

        _isMapReady = true;
        _ = RenderMapAsync();
    }

    private async Task RenderMapAsync()
    {
        var location = _currentLocation;
        if (!_isMapReady || location is null)
        {
            return;
        }

        var locationJson = JsonSerializer.Serialize(new
        {
            latitude = location.Latitude,
            longitude = location.Longitude
        });
        var trailsJson = JsonSerializer.Serialize(_nearbyTrails.Select(trail => new
        {
            name = trail.Name,
            latitude = trail.Location.Latitude,
            longitude = trail.Location.Longitude
        }));
        var script = $"window.updateHikerMap?.({locationJson}, {trailsJson});";

        try
        {
            await MainThread.InvokeOnMainThreadAsync(
                () => MapWebView.EvaluateJavaScriptAsync(script));
        }
        catch (Exception)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MapStatusLabel.Text = "Could not update the map. Check your internet connection.";
            });
        }
    }

    private void StopLocationTracking()
    {
        if (!_isListening)
        {
            return;
        }

        Geolocation.Default.LocationChanged -= OnLocationChanged;
        Geolocation.Default.StopListeningForeground();
        _isListening = false;
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

            if (!document.RootElement.TryGetProperty("display_name", out var displayName))
            {
                return "Street address unavailable";
            }

            return displayName.GetString() ?? "Street address unavailable";
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

    private async void OnSosClicked(object? sender, EventArgs e)
    {
        var urgency = await DisplayActionSheetAsync(
            "What kind of help do you need?",
            "Cancel",
            null,
            "Lost",
            "Medical emergency",
            "Police assistance",
