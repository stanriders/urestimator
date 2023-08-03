using MathNet.Numerics;
using UREstimator.Shared;

namespace UREstimator.Client
{
    public static class Calculator
    {
        public static double CalculateDeviation(Score score)
        {
            return score.Mode switch
            {
                "osu" => CalculateOsuDeviation(score),
                _ => 0.0
            } * 10.0;
        }

        private static double CalculateOsuDeviation(Score score)
        {
            int totalSuccessfulHits = score.Statistics.Count300 + score.Statistics.Count100 + score.Statistics.Count50;
            if (totalSuccessfulHits == 0)
                return double.PositiveInfinity;

            var adjustedOd = AdjustedOd(score.Mods, score.BeatmapShort.OverallDifficulty);

            double clockRate = score.Mods.Any(x => x.ToUpper() == "DT") ? 1.5 : score.Mods.Any(x => x.ToUpper() == "HT") ? 0.66 : 1;

            double hitWindow300 = 80 - 6 * adjustedOd;
            double hitWindow50 = (200 - 10 * ((80 - hitWindow300 * clockRate) / 6)) / clockRate;

            int greatCountOnCircles = score.BeatmapShort.CountCircles - score.Statistics.Count100 - score.Statistics.Count50 - score.Statistics.CountMiss;

            // The probability that a player hits a circle is unknown, but we can estimate it to be
            // the number of greats on circles divided by the number of circles, and then add one
            // to the number of circles as a bias correction / bayesian prior.
            double greatProbabilityCircle = Math.Max(0, greatCountOnCircles / (score.BeatmapShort.CountCircles + 1.0));
            double greatProbabilitySlider;

            if (greatCountOnCircles < 0)
            {
                int nonCircleMisses = -greatCountOnCircles;
                greatProbabilitySlider = Math.Max(0, (score.BeatmapShort.CountSliders - nonCircleMisses) / (score.BeatmapShort.CountSliders + 1.0));
            }
            else
            {
                greatProbabilitySlider = score.BeatmapShort.CountSliders / (score.BeatmapShort.CountSliders + 1.0);
            }

            if (greatProbabilityCircle == 0 && greatProbabilitySlider == 0)
                return double.PositiveInfinity;

            double deviationOnCircles = hitWindow300 / (Math.Sqrt(2) * SpecialFunctions.ErfInv(greatProbabilityCircle));
            double deviationOnSliders = hitWindow50 / (Math.Sqrt(2) * SpecialFunctions.ErfInv(greatProbabilitySlider));

            return Math.Min(deviationOnCircles, deviationOnSliders);
        }

        private static double AdjustedOd(string[] mods, double currentOd)
        {
            double finalOD = currentOd;

            if (mods.Any(x => x.ToUpper() == "HR"))
            {
                finalOD = Math.Min(finalOD * 1.4, 10);
            }
            else if (mods.Any(x => x.ToUpper() == "EZ"))
            {
                finalOD *= 0.5;
            }

            double ms = 80.0 - (6.0 * finalOD);
            if (mods.Any(x => x.ToUpper() == "DT") || mods.Any(x => x.ToUpper() == "NC"))
            {
                finalOD = (80.0 - (ms / 1.5)) / 6.0;
            }
            else if (mods.Any(x => x.ToUpper() == "HT"))
            {
                finalOD = (80.0 - (ms / 0.75)) / 6.0;
            }
            return finalOD;
        }
    }
}
