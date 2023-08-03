using UREstimator.Shared;

namespace UREstimator.Client.Models
{
    public class LeaderboardModel
    {
        public Player Player { get; set; }
        public double AverageEstimate { get; set; }
        public double WeightedEstimate { get; set; }
    }
}
