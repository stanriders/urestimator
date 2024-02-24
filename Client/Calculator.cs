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
            int totalSuccessfulHits = score.Statistics.Count300 + score.Statistics.Count100 ?? 0 + score.Statistics.Count50 ?? 0;
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
            double hitWindow100 = (140 - 8 * ((80 - hitWindow300 * clockRate) / 6)) / clockRate;
            double hitWindow50 = (200 - 10 * ((80 - hitWindow300 * clockRate) / 6)) / clockRate;

            int circleCount = score.BeatmapShort.CountCircles;
            int missCountCircles = Math.Min(score.Statistics.CountMiss ?? 0, circleCount);
            int mehCountCircles = Math.Min(score.Statistics.Count50 ?? 0, circleCount - missCountCircles);
            int okCountCircles = Math.Min(score.Statistics.Count100 ?? 0, circleCount - missCountCircles - mehCountCircles);
            int greatCountCircles = Math.Max(0, circleCount - missCountCircles - mehCountCircles - okCountCircles);

            // Assume 100s, 50s, and misses happen on circles. If there are less non-300s on circles than 300s,
            // compute the deviation on circles.
            if (greatCountCircles > 0)
            {
                // The probability that a player hits a circle is unknown, but we can estimate it to be
                // the number of greats on circles divided by the number of circles, and then add one
                // to the number of circles as a bias correction.
                double greatProbabilityCircle = greatCountCircles / (circleCount - missCountCircles - mehCountCircles + 1.0);

                // Compute the deviation assuming 300s and 100s are normally distributed, and 50s are uniformly distributed.
                // Begin with the normal distribution first.
                double deviationOnCircles = hitWindow300 / (Math.Sqrt(2) * SpecialFunctions.ErfInv(greatProbabilityCircle));
                deviationOnCircles *= Math.Sqrt(1 - Math.Sqrt(2 / Math.PI) * hitWindow100 * Math.Exp(-0.5 * Math.Pow(hitWindow100 / deviationOnCircles, 2))
                    / (deviationOnCircles * SpecialFunctions.Erf(hitWindow100 / (Math.Sqrt(2) * deviationOnCircles))));

                // Then compute the variance for 50s.
                double mehVariance = (hitWindow50 * hitWindow50 + hitWindow100 * hitWindow50 + hitWindow100 * hitWindow100) / 3;

                // Find the total deviation.
                deviationOnCircles = Math.Sqrt(((greatCountCircles + okCountCircles) * Math.Pow(deviationOnCircles, 2) + mehCountCircles * mehVariance) / (greatCountCircles + okCountCircles + mehCountCircles));

                return deviationOnCircles;
            }

            // If there are more non-300s than there are circles, compute the deviation on sliders instead.
            // Here, all that matters is whether or not the slider was missed, since it is impossible
            // to get a 100 or 50 on a slider by mis-tapping it.
            int sliderCount = score.BeatmapShort.CountSliders;
            int missCountSliders = Math.Min(sliderCount, score.Statistics.CountMiss ?? 0 - missCountCircles);
            int greatCountSliders = sliderCount - missCountSliders;

            // We only get here if nothing was hit. In this case, there is no estimate for deviation.
            // Note that this is never negative, so checking if this is only equal to 0 makes sense.
            if (greatCountSliders == 0)
            {
                return 0.0;
            }

            double greatProbabilitySlider = greatCountSliders / (sliderCount + 1.0);
            double deviationOnSliders = hitWindow50 / (Math.Sqrt(2) * SpecialFunctions.ErfInv(greatProbabilitySlider));

            return deviationOnSliders;
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

            int totalSuccessfulHits = score.Statistics.Count300 + score.Statistics.Count100 ?? 0;
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
                     + (score.Statistics.Count100 ?? 0 + 0.5) * p100) / totalSuccessfulHits
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
