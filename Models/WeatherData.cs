using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeatherViewer.Models
{
    public class WeatherData
    {
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }

        [JsonProperty("hourly")]
        public HourlyData? Hourly { get; set; }
    }

    public class HourlyData
    {
        [JsonProperty("time")]
        public List<string>? Time { get; set; }

        [JsonProperty("temperature_2m")]
        public List<double>? Temperature2m { get; set; }

        [JsonProperty("precipitation_probability")]
        public List<int>? PrecipitationProbability { get; set; }

        [JsonProperty("cloudcover")]
        public List<int>? CloudCover { get; set; }

        [JsonProperty("weathercode")]
        public List<int>? WeatherCode { get; set; }
    }
}
