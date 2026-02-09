using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WeatherViewer.Services
{
    public class GeocodingService
    {
        private readonly HttpClient _httpClient;

        public GeocodingService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<GeocodingResult>> GetSuggestionsAsync(string cityName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cityName) || cityName.Length < 2)
                    return [];

                string url = $"https://geocoding-api.open-meteo.com/v1/search?name={cityName}&count=10&language=en&format=json";
                var response = await _httpClient.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<GeocodingResponse>(response);

                return data?.Results ?? [];
            }
            catch
            {
                return [];
            }
        }

        public async Task<GeocodingResult?> GetCoordinatesAsync(string cityName)
        {
            var results = await GetSuggestionsAsync(cityName);
            return results.FirstOrDefault();
        }

        public async Task<string?> ReverseGeocodeAsync(double lat, double lon)
        {
            try
            {
                string url = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={lat}&longitude={lon}&localityLanguage=en";
                var response = await _httpClient.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<ReverseGeocodeResponse>(response);

                // Prefer city, then locality, then principalSubdivision
                return data?.City ?? data?.Locality ?? data?.PrincipalSubdivision;
            }
            catch
            {
                return null;
            }
        }
    }

    public class ReverseGeocodeResponse
    {
        [JsonProperty("city")]
        public string? City { get; set; }

        [JsonProperty("locality")]
        public string? Locality { get; set; }

        [JsonProperty("principalSubdivision")]
        public string? PrincipalSubdivision { get; set; }
    }

    public class GeocodingResponse
    {
        [JsonProperty("results")]
        public List<GeocodingResult>? Results { get; set; }
    }

    public class GeocodingResult
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("country")]
        public string? Country { get; set; }

        [JsonProperty("admin1")]
        public string? Admin1 { get; set; }
    }
}
