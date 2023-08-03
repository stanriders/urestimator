using System.Text.Json.Serialization;
using UREstimator.Shared;

namespace UREstimator.Server.OsuApi
{
    public class Leaderboard
    {
        [JsonPropertyName("ranking")]
        public LeaderboardRanking[] Ranking { get; set; }

        public class LeaderboardRanking
        {
            [JsonPropertyName("user")]
            public Player Player { get; set; }
        }
    }
}
