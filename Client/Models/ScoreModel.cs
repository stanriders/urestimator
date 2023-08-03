namespace UREstimator.Client.Models
{
    public class ScoreModel
    {
        public string Title { get; set; }
        public double UnstableRate { get; set; }
        public string[] Mods { get; set; }
        public ulong BeatmapId { get; set; }
        public double Pp { get; set; }
    }
}
