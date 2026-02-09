using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using WeatherViewer.Models;
using WeatherViewer.Services;
using Windows.Devices.Geolocation;

namespace WeatherViewer.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly WeatherService _weatherService;
        private readonly RadarService _radarService;
        private readonly GeocodingService _geocodingService;
        private WeatherData? _weatherData;
        private RainViewerData? _radarData;
        private System.Timers.Timer? _timer;
        private System.Timers.Timer? _debounceTimer;

        [ObservableProperty]
        private string _currentTemperature = "--°C";

        [ObservableProperty]
        private string _currentPrecipitationChance = "--%";

        [ObservableProperty]
        private string _currentCloudCover = "--%";

        [ObservableProperty]
        private string _currentWeatherDescription = "Loading...";

        [ObservableProperty]
        private string _forecastTemperature = "--°C";

        [ObservableProperty]
        private string _forecastPrecipitationChance = "--%";

        [ObservableProperty]
        private string _forecastCloudCover = "--%";

        [ObservableProperty]
        private string _forecastWeatherDescription = "--";

        [ObservableProperty]
        private double _sliderValue = 0;

        [ObservableProperty]
        private string _selectedTime = "--:--";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RadarButtonText))]
        private bool _isRadarVisible = false;

        [ObservableProperty]
        private string _currentSystemTime = "--:--:--";

        [ObservableProperty]
        private string _radarTimestamp = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RadarTimeLabel))]
        private double _radarTimeOffset = 0; // Minutes from now (-120 to +120)

        public string RadarTimeLabel
        {
            get
            {
                int mins = (int)RadarTimeOffset;
                if (mins == 0) return Localize("Now", "Teraz", "Jetzt");
                if (mins > 0) return $"+{mins} min";
                return $"{mins} min";
            }
        }

        [ObservableProperty]
        private double _latitude = 52.2297; // Warsaw default

        [ObservableProperty]
        private double _longitude = 21.0122;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayLocationName))]
        private string _locationName = "...";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayLocationName))]
        private bool _isLocationDetected = false;

        public string DisplayLocationName
        {
            get
            {
                string name = LocationName;
                // If it's the fallback "My Location", use the localized version
                if (name == "MY_LOCATION_KEY") name = MyLocationLabel;

                if (IsLocationDetected)
                {
                    return $"{name} ({DetectedLabel})";
                }
                return name;
            }
        }

        [ObservableProperty]
        private bool _isSearchVisible;

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty] private ObservableCollection<GeocodingResult> _locationSuggestions = [];
        [ObservableProperty] private bool _isAlwaysOnTop = false;

        [ObservableProperty] private double _baseFontSize = 12;
        [ObservableProperty] private bool _isSettingsVisible = false;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LocationLabel))]
        [NotifyPropertyChangedFor(nameof(RadarButtonText))]
        [NotifyPropertyChangedFor(nameof(SettingsHeader))]
        [NotifyPropertyChangedFor(nameof(FontSizeLabel))]
        [NotifyPropertyChangedFor(nameof(LanguageLabel))]
        [NotifyPropertyChangedFor(nameof(AlwaysOnTopLabel))]
        [NotifyPropertyChangedFor(nameof(SettingsCloseLabel))]
        [NotifyPropertyChangedFor(nameof(RainChanceLabel))]
        [NotifyPropertyChangedFor(nameof(ForecastTimeLabel))]
        [NotifyPropertyChangedFor(nameof(SearchPlaceholder))]
        [NotifyPropertyChangedFor(nameof(SearchButtonText))]
        [NotifyPropertyChangedFor(nameof(CurrentWeatherHeader))]
        [NotifyPropertyChangedFor(nameof(ForecastWeatherHeader))]
        [NotifyPropertyChangedFor(nameof(CloudCoverLabel))]
        [NotifyPropertyChangedFor(nameof(DisplayLocationName))]
        [NotifyPropertyChangedFor(nameof(RadarTimeLabel))]
        private string? _languageOverride = null;

        public string RadarButtonText => IsRadarVisible
            ? Localize("Hide Radar", "Ukryj Radar", "Radar ausblenden")
            : Localize("Show Radar", "Pokaż Radar", "Radar anzeigen");

        public string RainChanceLabel => Localize("Rain Chance:", "Szansa na deszcz:", "Regenwahrscheinlichkeit:");
        public string ForecastTimeLabel => Localize("Forecast Time:", "Czas prognozy:", "Vorhersagezeit:");
        public string LocationLabel => Localize("Location", "Lokalizacja", "Standort");
        public string SearchPlaceholder => Localize("Enter city name...", "Wpisz nazwę miasta...", "Stadt eingeben...");
        public string SearchButtonText => Localize("Search", "Szukaj", "Suchen");

        public string CurrentWeatherHeader => Localize("Current Weather", "Aktualna Pogoda", "Aktuelles Wetter");
        public string ForecastWeatherHeader => Localize("Forecast", "Prognoza", "Vorhersage");
        public string CloudCoverLabel => Localize("Cloud Cover:", "Zachmurzenie:", "Bewölkung:");
        public string SettingsHeader => Localize("Settings", "Ustawienia", "Einstellungen");
        public string FontSizeLabel => Localize("Font Size:", "Wielkość czcionki:", "Schriftgröße:");
        public string LanguageLabel => Localize("Language:", "Język:", "Sprache:");
        public string AlwaysOnTopLabel => Localize("Always on Top", "Zawsze na wierzchu", "Immer oben");
        public string SettingsCloseLabel => Localize("Close", "Zamknij", "Schließen");
        public string PleaseSelectLocationLabel => Localize("Please select location", "Proszę wybrać lokalizację", "Bitte Standort wählen");
        public string DetectedLabel => Localize("detected", "wykryto", "erkannt");
        public string MyLocationLabel => Localize("My Location", "Moja lokalizacja", "Mein Standort");

        public MainViewModel()
        {
            _weatherService = new WeatherService();
            _radarService = new RadarService();
            _geocodingService = new GeocodingService();
            _currentWeatherDescription = Localize("Loading...", "Ładowanie...", "Laden...");
            _locationName = Localize("Detecting...", "Wykrywanie...", "Erkennen...");

            _debounceTimer = new System.Timers.Timer(500);
            _debounceTimer.AutoReset = false;
            _debounceTimer.Elapsed += async (s, e) => await FetchSuggestionsAsync();

            StartClock();
            _ = InitializeLocationAsync();
        }

        private async Task InitializeLocationAsync()
        {
            try
            {
                var accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus == GeolocationAccessStatus.Allowed)
                {
                    var geolocator = new Geolocator { DesiredAccuracyInMeters = 100 };
                    // Set a timeout for position retrieval
                    var position = await geolocator.GetGeopositionAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));

                    Latitude = position.Coordinate.Point.Position.Latitude;
                    Longitude = position.Coordinate.Point.Position.Longitude;

                    // Attempt to find friendly name via reverse geocoding
                    var detectedName = await _geocodingService.ReverseGeocodeAsync(Latitude, Longitude);
                    if (!string.IsNullOrEmpty(detectedName))
                    {
                        LocationName = detectedName;
                    }
                    else
                    {
                        LocationName = "MY_LOCATION_KEY";
                    }
                    IsLocationDetected = true;
                    // Trigger data reload with new coordinates
                    await LoadDataAsync();
                }
                else
                {
                    LocationName = PleaseSelectLocationLabel;
                    IsLocationDetected = false;
                    await LoadDataAsync();
                }
            }
            catch
            {
                LocationName = PleaseSelectLocationLabel;
            }

            await LoadDataAsync();
        }

        private void StartClock()
        {
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) => CurrentSystemTime = DateTime.Now.ToString("HH:mm:ss");
            _timer.Start();
        }

        [RelayCommand]
        private void ToggleSearch()
        {
            IsSearchVisible = !IsSearchVisible;
            if (IsSearchVisible) IsSettingsVisible = false;
            if (!IsSearchVisible)
            {
                SearchQuery = string.Empty;
                LocationSuggestions.Clear();
            }
        }

        [RelayCommand]
        private void ToggleSettings()
        {
            IsSettingsVisible = !IsSettingsVisible;
            if (IsSettingsVisible)
            {
                IsSearchVisible = false;
                IsRadarVisible = false;
            }
        }
        [RelayCommand] private void ToggleAlwaysOnTop() => IsAlwaysOnTop = !IsAlwaysOnTop;
        [RelayCommand] private static void CloseApp() => System.Windows.Application.Current.Shutdown();
        [RelayCommand] private void SetLanguage(string? lang) { LanguageOverride = (lang == "AUTO") ? null : lang; UpdateDisplayedData(); }

        partial void OnSearchQueryChanged(string value)
        {
            _debounceTimer?.Stop();
            if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            {
                LocationSuggestions.Clear();
                return;
            }
            _debounceTimer?.Start();
        }

        private async Task FetchSuggestionsAsync()
        {
            var results = await _geocodingService.GetSuggestionsAsync(SearchQuery);
            App.Current.Dispatcher.Invoke(() =>
            {
                LocationSuggestions.Clear();
                foreach (var result in results)
                {
                    LocationSuggestions.Add(result);
                }
            });
        }

        [RelayCommand]
        private async Task SelectSuggestion(GeocodingResult suggestion)
        {
            if (suggestion == null) return;

            Latitude = suggestion.Latitude;
            Longitude = suggestion.Longitude;
            LocationName = suggestion.Name ?? "Unknown Location";
            IsLocationDetected = false;
            IsSearchVisible = false;
            SearchQuery = string.Empty;
            LocationSuggestions.Clear();
            await RefreshData();
        }

        [RelayCommand]
        private async Task SearchLocation()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            var result = await _geocodingService.GetCoordinatesAsync(SearchQuery);
            if (result != null)
            {
                await SelectSuggestion(result);
            }
        }

        [RelayCommand]
        private void ToggleRadar()
        {
            IsRadarVisible = !IsRadarVisible;
            if (IsRadarVisible)
            {
                // Reset radar time to now when opening
                RadarTimeOffset = 0;
            }
        }

        [RelayCommand]
        private async Task RefreshData()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            _weatherData = await _weatherService.GetForecastAsync(Latitude, Longitude);
            _radarData = await _radarService.GetRadarTimestampsAsync();
            UpdateDisplayedData();
        }

        partial void OnSliderValueChanged(double value)
        {
            UpdateDisplayedData();
        }

        partial void OnIsRadarVisibleChanged(bool value)
        {
            if (value)
            {
                // Reset radar time to now when opening
                RadarTimeOffset = 0;
            }
        }

        partial void OnRadarTimeOffsetChanged(double value)
        {
            UpdateDisplayedData();
        }

        private void UpdateDisplayedData()
        {
            if (_weatherData?.Hourly?.Time == null) return;

            // 1. Current Weather Logic (nearest hour)
            DateTime now = DateTime.Now;
            int currentIndex = 0;
            double minDiff = double.MaxValue;
            for (int i = 0; i < _weatherData.Hourly.Time.Count; i++)
            {
                DateTime t = DateTime.Parse(_weatherData.Hourly.Time[i]);
                double diff = Math.Abs((t - now).TotalMinutes);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    currentIndex = i;
                }
            }

            CurrentTemperature = $"{_weatherData.Hourly.Temperature2m?[currentIndex]}°C";
            CurrentPrecipitationChance = $"{_weatherData.Hourly.PrecipitationProbability?[currentIndex]}%";
            CurrentCloudCover = $"{_weatherData.Hourly.CloudCover?[currentIndex]}%";
            CurrentWeatherDescription = GetWeatherDescription(_weatherData.Hourly.WeatherCode?[currentIndex] ?? 0);

            // 2. Forecast Weather Logic (based on slider)
            int index = (int)Math.Round(SliderValue);
            if (index < 0) index = 0;
            if (index >= _weatherData.Hourly.Time.Count) index = _weatherData.Hourly.Time.Count - 1;

            ForecastTemperature = $"{_weatherData.Hourly.Temperature2m?[index]}°C";
            ForecastPrecipitationChance = $"{_weatherData.Hourly.PrecipitationProbability?[index]}%";
            ForecastCloudCover = $"{_weatherData.Hourly.CloudCover?[index]}%";
            ForecastWeatherDescription = GetWeatherDescription(_weatherData.Hourly.WeatherCode?[index] ?? 0);
            SelectedTime = DateTime.Parse(_weatherData.Hourly.Time[index]).ToString("HH:mm");

            // 3. Radar Timestamp
            if (_radarData?.Radar != null)
            {
                var frames = new List<RadarFrame>();
                if (_radarData.Radar.Past != null) frames.AddRange(_radarData.Radar.Past);
                if (_radarData.Radar.Nowcast != null) frames.AddRange(_radarData.Radar.Nowcast);

                if (frames.Count > 0)
                {
                    // Calculate target time based on current time + radar offset
                    var targetTime = DateTime.Now.AddMinutes(RadarTimeOffset);
                    long targetUnix = ((DateTimeOffset)targetTime).ToUnixTimeSeconds();

                    // Find frame closest to the target time
                    var sortedFrames = frames.OrderBy(f => Math.Abs(f.Time - targetUnix)).ToList();
                    var closestFrame = sortedFrames.First();

                    // RainViewer typically has data for +/- 2 hours from 'now'.
                    // If target is more than 2 hours away from any frame, it's effectively "no data"
                    if (Math.Abs(closestFrame.Time - targetUnix) > 7200) // 2 hours
                    {
                        RadarTimestamp = string.Empty; // Signal "no radar data"
                    }
                    else
                    {
                        string newTimestamp = closestFrame.Time.ToString();
                        if (RadarTimestamp != newTimestamp)
                        {
                            RadarTimestamp = newTimestamp;
                        }
                    }
                }
            }
        }

        private string GetWeatherDescription(int code)
        {
            return code switch
            {
                0 => Localize("Clear sky", "Bezchmurnie", "Klarer Himmel"),
                1 => Localize("Mainly clear", "Przeważnie bezchmurnie", "Meist klar"),
                2 => Localize("Partly cloudy", "Częściowe zachmurzenie", "Teilweise bewölkt"),
                3 => Localize("Overcast", "Pochmurno", "Bedeckt"),
                45 or 48 => Localize("Fog", "Mgła", "Nebel"),
                51 or 53 or 55 => Localize("Drizzle", "Mżawka", "Nieselregen"),
                61 or 63 or 65 => Localize("Rain", "Deszcz", "Regen"),
                71 or 73 or 75 => Localize("Snow", "Śnieg", "Schnee"),
                95 or 96 or 99 => Localize("Thunderstorm", "Burza", "Gewitter"),
                _ => Localize("Unknown", "Nieznane", "Unbekannt")
            };
        }

        private string Localize(string en, string pl, string de)
        {
            var lang = LanguageOverride ?? System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return lang.ToLower() switch
            {
                "pl" => pl,
                "de" => de,
                _ => en
            };
        }
    }
}
