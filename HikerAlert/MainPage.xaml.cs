using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;

namespace HikerAlert
{
    public partial class MainPage : ContentPage
    {
        private readonly NearbyTrailService _nearbyTrailService = new();
        private IReadOnlyList<NearbyTrail> _nearbyTrails = Array.Empty<NearbyTrail>();
        private Location? _currentLocation;
        private WebView _mapWebView = null!;
        private bool _isListening;
        private bool _isSearchingForTrails;
        private bool _isMapReady;
        private Location? _lastTrailLookupLocation;
        private DateTime _lastTrailLookupTime = DateTime.MinValue;

        public MainPage()
        {
            InitializeComponent();
            _mapWebView = (WebView)FindByName("MapWebView");
            _mapWebView.Navigated += OnMapNavigated;
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
                    StatusLabel.Text = "Location permission was not granted.";
                    return;
                }

                // Request foreground updates, at most once every five seconds.
                var request = new GeolocationListeningRequest(GeolocationAccuracy.Best)
                {
                    MinimumTime = TimeSpan.FromSeconds(5)
                };

                Geolocation.Default.LocationChanged += OnLocationChanged;
                _isListening = await Geolocation.Default.StartListeningForegroundAsync(request);

                if (!_isListening)
                {
                    Geolocation.Default.LocationChanged -= OnLocationChanged;
                    StatusLabel.Text = "Could not start location updates.";
                    return;
                }

                // Start listening first so location updates continue while trails are fetched.
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
                StatusLabel.Text = "Turn on device location services to use the map.";
            }
            catch (FeatureNotSupportedException)
            {
                StatusLabel.Text = "Location is not supported on this device.";
            }
            catch (PermissionException)
            {
                StatusLabel.Text = "The app does not have location permission.";
            }
            catch (Exception)
            {
                StatusLabel.Text = "Unable to get your location.";
            }
        }

        private void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CenterMapOn(e.Location);
                if (!_isSearchingForTrails)
                {
                    StatusLabel.Text = "Live location enabled";
                }
            });

            if (ShouldRefreshTrailSearch(e.Location))
            {
                _ = LoadNearbyTrailsAsync(e.Location);
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
                    StatusLabel.Text = "Searching for nearby hiking trails...";
                });
                var trails = await _nearbyTrailService.FindNearbyTrailsAsync(userLocation);
                _nearbyTrails = trails;
                await RenderMapAsync();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = trails.Count == 0
                        ? "No named hiking trails found within 5 km."
                        : $"Found {trails.Count} nearby hiking trail(s).";
                });
            }
            catch (Exception)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = "Could not load nearby trails. Check your internet connection.";
                });
            }
            finally
            {
                _isSearchingForTrails = false;
            }
        }

        private void CenterMapOn(Location location)
        {
            _currentLocation = location;
            _ = RenderMapAsync();
        }

        private async Task LoadMapPageAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("map.html");
                using var reader = new StreamReader(stream);
                _mapWebView.Source = new HtmlWebViewSource
                {
                    Html = await reader.ReadToEndAsync()
                };
            }
            catch (Exception)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = "Could not load the map page.";
                });
            }
        }

        private void OnMapNavigated(object? sender, WebNavigatedEventArgs e)
        {
            if (e.Result != WebNavigationResult.Success)
            {
                StatusLabel.Text = "Could not open the map. Check your internet connection.";
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
                    () => _mapWebView.EvaluateJavaScriptAsync(script));
            }
            catch (Exception)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.Text = "Could not update the map. Check your internet connection.";
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
    }
}
