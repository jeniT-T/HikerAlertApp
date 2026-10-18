using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace HikerAlert;

public partial class MainPage : ContentPage
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private bool _hasLoadedWeather;
    private bool _isRefreshing;

    public ObservableCollection<HikingLocation> HikingLocations { get; } = new()
    {
        new("Wenderholm Regional Park", "Auckland, New Zealand",
            -36.5000, 174.7700, "wenderholm_regional_park.jpg"),
        new("Shakespear Regional Park", "Auckland, New Zealand",
            -36.6300, 174.8200, "shakespear_regional_park.jpg"),
        new("Rangitoto Summit Track", "Auckland, New Zealand",
            -36.7870, 174.8600, "rangitoto_summit_track.jpg"),
        new("Mount Eden", "Auckland, New Zealand",
            -36.8770, 174.7640, "mount_eden.jpg"),
        new("Long Bay Regional Park", "Auckland, New Zealand",
            -36.6900, 174.7500, "long_bay_regional_park.jpg"),
        new("Karamatura Track", "Auckland, New Zealand",
            -37.0000, 174.5500, "karamatura_track.jpg"),
        new("Hunua Falls Track", "Auckland, New Zealand",
            -37.0800, 175.0800, "hunua_falls_track.jpg"),
        new("Cornwall Park", "Auckland, New Zealand",
            -36.9000, 174.7800, "cornwall_park.jpg"),
        new("Auckland Domain", "Auckland, New Zealand",
            -36.8600, 174.7700, "auckland_domain.jpg")
    };

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_hasLoadedWeather)
        {
            _hasLoadedWeather = true;
            await RefreshWeatherAsync();
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshWeatherAsync();
    }

    private async Task RefreshWeatherAsync()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;

        if (senderRefreshButton() is { } refreshButton)
        {
            refreshButton.IsEnabled = false;
        }

        try
        {
            foreach (var location in HikingLocations)
            {
                location.WeatherText = "Loading weather...";
            }

            await Task.WhenAll(HikingLocations.Select(UpdateWeatherAsync));
        }
        finally
        {
            _isRefreshing = false;

            if (senderRefreshButton() is { } button)
            {
                button.IsEnabled = true;
            }
        }

        Button? senderRefreshButton() =>
            this.FindByName<Button>("RefreshButton");
    }

    private static async Task UpdateWeatherAsync(HikingLocation location)
    {
        try
        {
            var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
            var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);
            var url =
                "https://api.open-meteo.com/v1/forecast" +
                $"?latitude={latitude}&longitude={longitude}" +
                "&current=temperature_2m,weather_code&timezone=auto";

            var response = await HttpClient.GetFromJsonAsync<WeatherResponse>(url);

            if (response?.Current is null)
            {
                location.WeatherText = "Weather unavailable";
                return;
            }

            var temperature = response.Current.TemperatureCelsius
                .ToString("0.#", CultureInfo.InvariantCulture);

            location.WeatherText =
                $"{temperature}°C · {DescribeWeather(response.Current.WeatherCode)}";
        }
        catch
        {
            location.WeatherText = "Weather unavailable";
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

    private async void OnSosClicked(object? sender, EventArgs e)
    {
        var urgency = await DisplayActionSheetAsync(
            "What kind of help do you need?",
            "Cancel",
            null,
            "Lost",
            "Medical emergency",
            "Police assistance",
            "Injured / unable to walk",
            "Other");

        if (string.IsNullOrWhiteSpace(urgency) || urgency == "Cancel")
        {
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "Send SOS?",
            $"Create an alert for: {urgency}?",
            "Send SOS",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        var button = sender as Button;
        if (button is not null)
        {
            button.IsEnabled = false;
        }

        try
        {
            var sent = await SosAlertService.SubmitAsync(urgency);

            await DisplayAlertAsync(
                sent ? "SOS sent" : "SOS saved on this device",
                sent
                    ? "The ranger service accepted the alert."
                    : "Delivery has not been confirmed. The alert is queued on this device and will retry when connectivity returns.",
                "OK");
        }
        catch
        {
            await DisplayAlertAsync(
                "SOS could not be saved",
                "The alert was not saved. Try again if it is safe to do so.",
                "OK");
        }
        finally
        {
            if (button is not null)
            {
                button.IsEnabled = true;
            }
        }
    }

    public sealed class HikingLocation : INotifyPropertyChanged
    {
        private string _weatherText = "Weather not loaded";

        public HikingLocation(
            string name,
            string location,
            double latitude,
            double longitude,
            string imageUrl)
        {
            Name = name;
            Location = location;
            Latitude = latitude;
            Longitude = longitude;
            ImageUrl = imageUrl;
        }

        public string Name { get; }
        public string Location { get; }
        public double Latitude { get; }
        public double Longitude { get; }
        public string ImageUrl { get; }

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

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
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