using System.Text.Json.Serialization;

namespace UREstimator.Shared
{
    public class Player
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }


        [JsonPropertyName("username")]
        public string? Username { get; set; }


        [JsonPropertyName("statistics")]
        public Statistics Statistics { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    public class Statistics
    {
        [JsonPropertyName("global_rank")]
        public int GlobalRank { get; set; }

        [JsonPropertyName("pp")]
        public double Pp { get; set; }
    }
}
