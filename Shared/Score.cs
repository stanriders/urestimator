using System.Text.Json.Serialization;

namespace UREstimator.Shared
{

    public class Score
    {
        [JsonPropertyName("id")]
        public ulong? Id { get; set; }

        [JsonPropertyName("user_id")]
        public uint UserId { get; set; }

        [JsonPropertyName("user")]
        public Player? User { get; set; }

        [JsonPropertyName("beatmap")]
        public BeatmapShort? BeatmapShort { get; set; }

        [JsonPropertyName("beatmapset")]
        public BeatmapSetShort? BeatmapSet { get; set; }

        [JsonPropertyName("rank")]
        //[JsonConverter(typeof(StringEnumConverter))]
        public string? Grade { get; set; }

        [JsonPropertyName("pp")]
        public double? Pp { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; }

        private double accuracy = 0.0;
        [JsonPropertyName("accuracy")]
        public double Accuracy
        {
            get
            {
                if (accuracy <= 0.0)
                {
                    /*
                     * Accuracy = Total points of hits / (Total number of hits * 300)
                     * Total points of hits  =  Number of 50s * 50 + Number of 100s * 100 + Number of 300s * 300
                     * Total number of hits  =  Number of misses + Number of 50's + Number of 100's + Number of 300's
                     */

                    double totalPoints = Statistics.Count50 * 50 + Statistics.Count100 * 100 + Statistics.Count300 * 300;
                    double totalHits = Statistics.CountMiss + Statistics.Count50 + Statistics.Count100 + Statistics.Count300;

                    accuracy = totalPoints / (totalHits * 300) * 100;
                }

                return accuracy;
            }
            set => accuracy = value * 100.0;
        }

        [JsonPropertyName("max_combo")]
        public uint Combo { get; set; }

        [JsonPropertyName("mods")]
        public string[] Mods { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? Date { get; set; }

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

        [JsonPropertyName("beatmapset_id")]
        public int BeatmapSetId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("mode")]
        public string ModeName { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("max_combo")]
        public uint? MaxCombo { get; set; }

        [JsonPropertyName("user_id")]
        public uint CreatorId { get; set; }

        [JsonPropertyName("count_circles")]
        public int CountCircles { get; set; }
        [JsonPropertyName("count_sliders")]
        public int CountSliders { get; set; }
        [JsonPropertyName("count_spinners")]
        public int CountSpinners { get; set; }
        [JsonPropertyName("accuracy")]
        public double OverallDifficulty { get; set; }
    }

    public class BeatmapSetShort
    {
        [JsonPropertyName("id")]
        public ulong Id { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("creator")]
        public string CreatorName { get; set; }

        [JsonPropertyName("user_id")]
        public uint CreatorId { get; set; }

        [JsonPropertyName("covers")]
        public CoverList? Covers { get; set; }

        public class CoverList
        {
            [JsonPropertyName("list")]
            public string List { get; set; }
        }
    }
}