using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Microsoft.Maui.Devices.Sensors;

namespace HikerAlert;

public partial class HikingTrailsPage : ContentPage
{
    private const double AucklandLatitude = -36.8485;
    private const double AucklandLongitude = 174.7633;
    private const double EarthRadiusKm = 6371.0;

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    // Limit simultaneous weather requests when refreshing the whole list.
    private static readonly SemaphoreSlim WeatherRequestLimit = new(3);

    private bool _isLoading;

    public ObservableCollection<TrailCardViewModel> Trails { get; } = new();

    public HikingTrailsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Trails.Count == 0)
        {
            await LoadTrailsAsync();
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadTrailsAsync();
    }

    private async Task LoadTrailsAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        RefreshButton.IsEnabled = false;
        StatusLabel.Text = "Finding your location and sorting trails...";

        try
        {
            var (latitude, longitude, usingFallback) = await GetReferenceLocationAsync();

            var nearbyTrails = GetAucklandTrails()
                .Select(trail => new TrailCardViewModel(
                    trail,
                    CalculateDistanceKm(
                        latitude,
                        longitude,
                        trail.Latitude,
                        trail.Longitude)))
                .OrderBy(trail => trail.DistanceKm)
                .ToList();

            Trails.Clear();

            foreach (var trail in nearbyTrails)
            {
                Trails.Add(trail);
            }

            StatusLabel.Text = usingFallback
                ? "Location unavailable. Sorted from central Auckland; enable location for distances from you."
                : "Sorted from your current location. Distances are straight-line estimates.";

            // Show the list first, then update each card's weather as requests finish.
            await Task.WhenAll(Trails.Select(UpdateWeatherAsync));
        }
        finally
        {
            RefreshButton.IsEnabled = true;
            _isLoading = false;
        }
    }

    private static async Task<(double Latitude, double Longitude, bool UsingFallback)>
        GetReferenceLocationAsync()
    {
        try
        {
            var request = new GeolocationRequest(
                GeolocationAccuracy.Medium,
                TimeSpan.FromSeconds(10));

            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location is not null)
            {
                return (location.Latitude, location.Longitude, false);
            }
        }
        catch
        {
            // Permission denied, location services disabled, or location lookup failed.
        }

        // Auckland is a transparent fallback for this Auckland-focused sample.
        return (AucklandLatitude, AucklandLongitude, true);
    }

    private static List<TrailLocation> GetAucklandTrails() =>
    [
        new("Maungawhau / Mount Eden", "Auckland", -36.8770, 174.7644),
        new("Auckland Domain", "Central Auckland", -36.8606, 174.7714),
        new("Cornwall Park / One Tree Hill", "Auckland", -36.9000, 174.7830),
        new("Long Bay Regional Park", "North Shore", -36.6845, 174.7523),
        new("Rangitoto Summit Track", "Hauraki Gulf", -36.7870, 174.8620),
        new("Shakespear Regional Park", "Whangaparāoa Peninsula", -36.6370, 174.8380),
        new("Wenderholm Regional Park", "Waiwera", -36.5214, 174.7085),
        new("Hunua Falls Track", "Hunua Ranges", -37.0812, 175.0910),
        new("Karamatura Track", "Huia, Waitākere Ranges", -37.0057, 174.5543)
    ];

    private static double CalculateDistanceKm(
        double startLatitude,
        double startLongitude,
        double endLatitude,
        double endLongitude)
    {
        static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        var latitudeDifference = ToRadians(endLatitude - startLatitude);
        var longitudeDifference = ToRadians(endLongitude - startLongitude);

        var a =
            Math.Pow(Math.Sin(latitudeDifference / 2), 2) +
            Math.Cos(ToRadians(startLatitude)) *
            Math.Cos(ToRadians(endLatitude)) *
            Math.Pow(Math.Sin(longitudeDifference / 2), 2);

        a = Math.Clamp(a, 0.0, 1.0);

        return EarthRadiusKm * 2.0 *
               Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
    }

    private static async Task UpdateWeatherAsync(TrailCardViewModel trail)
    {
        await WeatherRequestLimit.WaitAsync();

        try
        {
            var latitude = trail.Location.Latitude.ToString(CultureInfo.InvariantCulture);
            var longitude = trail.Location.Longitude.ToString(CultureInfo.InvariantCulture);

            var url =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={latitude}&longitude={longitude}" +
                "&current=temperature_2m,weather_code&timezone=auto";

            var result = await HttpClient.GetFromJsonAsync<WeatherResponse>(url);

            if (result?.Current is null)
            {
                trail.WeatherText = "Weather unavailable";
                return;
            }

            var temperature = result.Current.TemperatureCelsius
                .ToString("0.#", CultureInfo.InvariantCulture);

            trail.WeatherText =
                $"{temperature}°C · {DescribeWeather(result.Current.WeatherCode)}";
        }
        catch
        {
            trail.WeatherText = "Weather unavailable";
        }
        finally
        {
            WeatherRequestLimit.Release();
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
        61 or 63 or 65 => "Rain",
        71 or 73 or 75 or 77 => "Snow",
        80 or 81 or 82 => "Rain showers",
        95 or 96 or 99 => "Thunderstorm",
        _ => "Current conditions"
    };

    // make the nested record at least as accessible as the constructor that uses it
    internal sealed record TrailLocation(
        string Name,
        string Area,
        double Latitude,
        double Longitude);

    public sealed class TrailCardViewModel : INotifyPropertyChanged
    {
        private string _weatherText = "Loading weather...";

        internal TrailCardViewModel(TrailLocation location, double distanceKm)
        {
            Location = location;
            Name = location.Name;
            Area = location.Area;
            DistanceKm = distanceKm;
            DistanceText = $"{distanceKm.ToString("0.0", CultureInfo.InvariantCulture)} km away";

            var latitude = location.Latitude.ToString("F5", CultureInfo.InvariantCulture);
            var longitude = location.Longitude.ToString("F5", CultureInfo.InvariantCulture);

            MapImageUrl =
                "https://staticmap.openstreetmap.de/staticmap.php" +
                $"?center={latitude},{longitude}&zoom=14&size=640x340" +
                $"&maptype=mapnik&markers={latitude},{longitude},red-pushpin";
        }

        internal TrailLocation Location { get; }

        public string Name { get; }
        public string Area { get; }
        public string DistanceText { get; }
        public string MapImageUrl { get; }
        public double DistanceKm { get; }

        public string WeatherText
        {
            get => _weatherText;
            set
            {
                if (_weatherText == value)
                {
                    return;
                }

                _weatherText = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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