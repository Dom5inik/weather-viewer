using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WeatherViewer.Models;

namespace WeatherViewer.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<WeatherData?> GetForecastAsync(double lat, double lon)
        {
            try
            {
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={lon.ToString(System.Globalization.CultureInfo.InvariantCulture)}&hourly=temperature_2m,precipitation_probability,cloudcover,weathercode&forecast_days=1";
                string json = await _httpClient.GetStringAsync(url);
                return JsonConvert.DeserializeObject<WeatherData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
