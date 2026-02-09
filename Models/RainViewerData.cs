using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace WeatherViewer.Models
{
    public class RainViewerData
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("generated")]
        public long Generated { get; set; }

        [JsonPropertyName("host")]
        public string? Host { get; set; }

        [JsonPropertyName("radar")]
        public RadarData? Radar { get; set; }
    }

    public class RadarData
    {
        [JsonPropertyName("past")]
        public List<RadarFrame>? Past { get; set; }

        [JsonPropertyName("nowcast")]
        public List<RadarFrame>? Nowcast { get; set; }
    }

    public class RadarFrame
    {
        [JsonPropertyName("time")]
        public long Time { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
