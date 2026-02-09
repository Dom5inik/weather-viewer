using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherViewer.Models;

namespace WeatherViewer.Services
{
    public class RadarService
    {
        private readonly HttpClient _httpClient;

        public RadarService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<RainViewerData?> GetRadarTimestampsAsync()
        {
            try
            {
                string url = "https://api.rainviewer.com/public/weather-maps.json";
                string json = await _httpClient.GetStringAsync(url);
                return JsonSerializer.Deserialize<RainViewerData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
