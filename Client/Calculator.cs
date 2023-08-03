using MathNet.Numerics;
using UREstimator.Shared;

namespace UREstimator.Client
{
    public static class Calculator
    {
        public static double CalculateDeviation(ScoreSlim score)
        {
            return score.Mode switch
            {
                "osu" => CalculateOsuDeviation(score),
                "taiko" => CalculateTaikoDeviation(score),
                "mania" => CalculateManiaDeviation(score),
                _ => 0.0
            } * 10.0;
        }

        private static double CalculateOsuDeviation(ScoreSlim score)
        {
            int totalSuccessfulHits = score.Statistics.Count300 + score.Statistics.Count100 + score.Statistics.Count50;
            if (totalSuccessfulHits == 0)
                return double.PositiveInfinity;

            double adjustedOd = score.BeatmapShort.OverallDifficulty;

            if (score.Mods.Any(x => x.ToUpper() == "HR"))
            {
                adjustedOd = Math.Min(adjustedOd * 1.4, 10);
            }
            else if (score.Mods.Any(x => x.ToUpper() == "EZ"))
            {
                adjustedOd *= 0.5;
            }

            double ms = 80.0 - (6.0 * adjustedOd);
            if (score.Mods.Any(x => x.ToUpper() == "DT") || score.Mods.Any(x => x.ToUpper() == "NC"))
            {
                adjustedOd = (80.0 - (ms / 1.5)) / 6.0;
            }
            else if (score.Mods.Any(x => x.ToUpper() == "HT"))
            {
                adjustedOd = (80.0 - (ms / 0.75)) / 6.0;
            }

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

        private static double CalculateTaikoDeviation(ScoreSlim score)
        {
            var multiplier = 1.0;

            var adjustedOd = score.BeatmapShort.OverallDifficulty;
            if (score.Mods.Any(x => x.ToUpper() == "HR"))
            {
                adjustedOd = Math.Min(adjustedOd * 1.4, 10);
            }
            else if (score.Mods.Any(x => x.ToUpper() == "EZ"))
            {
                adjustedOd *= 0.5;
            }

            if (score.Mods.Any(x => x.ToUpper() == "DT") || score.Mods.Any(x => x.ToUpper() == "NC"))
                multiplier = 2.0 / 3.0;
            else if (score.Mods.Any(x => x.ToUpper() == "HT"))
                multiplier = 4.0 / 3.0;

            double h300 = (50.0 - 3.0 * adjustedOd) * multiplier;
            double h100 = (80.0 - 8.0 * (adjustedOd - 5)) * multiplier;

            if (adjustedOd < 5)
            {
                h100 = (120.0 - 6.0 * adjustedOd) * multiplier;
            }

            int totalSuccessfulHits = score.Statistics.Count300 + score.Statistics.Count100;
            if (totalSuccessfulHits == 0)
                return double.PositiveInfinity;

            if (h300 <= 0)
                return double.PositiveInfinity;

            // Determines the probability of a deviation leading to the score's hit evaluations. The curve's apex represents the most probable deviation.
            double likelihoodGradient(double d)
            {
                if (d <= 0)
                    return 0;

                double p300 = logDiff(0, logPcHit(h300, d));
                double p100 = logDiff(logPcHit(h300, d), logPcHit(h100, d));

                double gradient = Math.Exp(
                    (score.Statistics.Count300 * p300
                     + (score.Statistics.Count100 + 0.5) * p100) / totalSuccessfulHits
                );

                return -gradient;
            }

            double deviation = FindMinimum.OfScalarFunction(likelihoodGradient, 30);

            return deviation;
        }
        
        private static double CalculateManiaDeviation(ScoreSlim score)
        {
            var maniaCalc = new ManiaCalculator(score);
            return maniaCalc.ComputeEstimatedUr() ?? 0;
        }

        private static double logPcHit(double x, double deviation) => logErfcApprox(x / (deviation * Math.Sqrt(2)));

        // Utilises a numerical approximation to extend the computation range of ln(erfc(x)).
        private static double logErfcApprox(double x) => x <= 5
            ? Math.Log(SpecialFunctions.Erfc(x))
            : -Math.Pow(x, 2) - Math.Log(x * Math.Sqrt(Math.PI)); // https://www.desmos.com/calculator/kdbxwxgf01

        // Subtracts the base value of two logs, circumventing log rules that typically complicate subtraction of non-logarithmic values.
        private static double logDiff(double firstLog, double secondLog)
        {
            double maxVal = Math.Max(firstLog, secondLog);

            // To avoid a NaN result, a check is performed to prevent subtraction of two negative infinity values.
            if (double.IsNegativeInfinity(maxVal))
            {
                return maxVal;
            }

            return firstLog + SpecialFunctions.Log1p(-Math.Exp(-(firstLog - secondLog)));
        }
    }
}
