using System.Text.Json.Serialization;

namespace UREstimator.Shared
{
    public class ScoreSlim
    {
        [JsonPropertyName("beatmap")]
        public BeatmapShort? BeatmapShort { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        [JsonPropertyName("mods")]
        public string[] Mods { get; set; }

        [JsonPropertyName("statistics")]
        public ScoreStatistics Statistics { get; set; }

        public class ScoreStatistics
        {
            [JsonPropertyName("count_50")]
            public int Count50 { get; set; }

            [JsonPropertyName("count_100")]
            public int Count100 { get; set; }

            [JsonPropertyName("count_300")]
            public int Count300 { get; set; }

            [JsonPropertyName("count_geki")]
            public int CountGeki { get; set; }

            [JsonPropertyName("count_katu")]
            public int CountKatu { get; set; }

            [JsonPropertyName("count_miss")]
            public int CountMiss { get; set; }
        }
    }

    public class BeatmapShort
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("count_circles")]
        public int CountCircles { get; set; }
        [JsonPropertyName("count_sliders")]
        public int CountSliders { get; set; }
        [JsonPropertyName("count_spinners")]
        public int CountSpinners { get; set; }
        [JsonPropertyName("accuracy")]
        public double OverallDifficulty { get; set; }
        [JsonPropertyName("convert")]
        public bool IsConvert { get; set; }
    }
}
