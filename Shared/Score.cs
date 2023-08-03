using System.Text.Json.Serialization;

namespace UREstimator.Shared
{

    public class Score : ScoreSlim
    {
        [JsonPropertyName("user")]
        public Player? User { get; set; }

        [JsonPropertyName("beatmapset")]
        public BeatmapSetShort? BeatmapSet { get; set; }

        [JsonPropertyName("pp")]
        public double? Pp { get; set; }
        
        [JsonPropertyName("accuracy")]
        public double Accuracy { get; set; }

    }

    public class BeatmapSetShort
    {
        [JsonPropertyName("artist")]
        public string Artist { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("covers")]
        public CoverList? Covers { get; set; }

        public class CoverList
        {
            [JsonPropertyName("list")]
            public string List { get; set; }
        }
    }
}