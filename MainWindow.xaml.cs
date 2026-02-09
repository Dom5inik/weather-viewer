using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WeatherViewer.ViewModels;

namespace WeatherViewer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
            InitializeWebView();
            this.Loaded += MainWindow_Loaded;
        }

        private async void InitializeWebView()
        {
            try
            {
                await RadarWebView.EnsureCoreWebView2Async();
                string radarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "radar.html");
                RadarWebView.CoreWebView2.Navigate(radarPath);

                // Initialize map once navigation is complete
                RadarWebView.NavigationCompleted += (s, e) =>
                {
                    if (DataContext is MainViewModel vm)
                    {
                        RadarWebView.CoreWebView2.ExecuteScriptAsync($"if(typeof initMap === 'function') initMap({vm.Latitude.ToString(CultureInfo.InvariantCulture)}, {vm.Longitude.ToString(CultureInfo.InvariantCulture)})");
                        if (!string.IsNullOrEmpty(vm.RadarTimestamp))
                        {
                            RadarWebView.CoreWebView2.ExecuteScriptAsync($"if(typeof updateRadar === 'function') updateRadar({vm.RadarTimestamp})");
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView Error: {ex.Message}", "Error");
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.RadarTimestamp))
            {
                if (DataContext is MainViewModel vm && !string.IsNullOrEmpty(vm.RadarTimestamp))
                {
                    if (RadarWebView.CoreWebView2 != null)
                    {
                        try
                        {
                            await RadarWebView.CoreWebView2.ExecuteScriptAsync($"if(typeof updateRadar === 'function') updateRadar({vm.RadarTimestamp})");
                        }
                        catch { /* Ignore if WebView not ready */ }
                    }
                }
            }

            if (e.PropertyName == nameof(MainViewModel.IsRadarVisible))
            {
                if (DataContext is MainViewModel vm)
                {
                    RadarWebView.Visibility = vm.IsRadarVisible ? Visibility.Visible : Visibility.Collapsed;
                    if (vm.IsRadarVisible && RadarWebView.CoreWebView2 != null)
                    {
                        // Ensure map is centered on current location when opened
                        await RadarWebView.CoreWebView2.ExecuteScriptAsync($"if(typeof initMap === 'function') initMap({vm.Latitude.ToString(CultureInfo.InvariantCulture)}, {vm.Longitude.ToString(CultureInfo.InvariantCulture)})");
                    }
                }
            }

            if (e.PropertyName == nameof(MainViewModel.Latitude) || e.PropertyName == nameof(MainViewModel.Longitude))
            {
                if (DataContext is MainViewModel vm && RadarWebView.CoreWebView2 != null)
                {
                    await RadarWebView.CoreWebView2.ExecuteScriptAsync($"if(typeof initMap === 'function') initMap({vm.Latitude.ToString(CultureInfo.InvariantCulture)}, {vm.Longitude.ToString(CultureInfo.InvariantCulture)})");
                }
            }
        }

        private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.DragMove();
            }
            catch { }
        }

        private void ToggleSettings_Click(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.IsSettingsVisible = false;
            }
        }
    }

    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && parameter is string p && double.TryParse(p, NumberStyles.Any, CultureInfo.InvariantCulture, out double factor))
            {
                return d * factor;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}