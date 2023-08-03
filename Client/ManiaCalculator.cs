using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using UREstimator.Shared;

namespace UREstimator.Client
{
    public class ManiaCalculator
    {
        public class ManiaDifficultyAttributes
        {
            public double OverallDifficulty { get; set; }
            public int NoteCount { get; set; }
            public int HoldNoteCount { get; set; }
            public bool IsConvert { get; set; }

        }
        private const double tail_multiplier = 1.5; // Lazer LN tails have 1.5x the hit window of a Note or an LN head.
        private const double tail_deviation_multiplier = 1.75; // Empirical testing shows that players get ~1.8x the deviation on tails.

        private int countPerfect;
        private int countGreat;
        private int countGood;
        private int countOk;
        private int countMeh;
        private int countMiss;
        private bool isLegacyScore;
        private double[] hitWindows = null!;
        private readonly ManiaDifficultyAttributes attributes;

        public ManiaCalculator(ScoreSlim score)
        {
            countPerfect = score.Statistics.CountGeki;
            countGreat = score.Statistics.Count300;
            countGood = score.Statistics.CountKatu;
            countOk = score.Statistics.Count100;
            countMeh = score.Statistics.Count50;
            countMiss = score.Statistics.CountMiss;
            isLegacyScore = true;

            attributes = new ManiaDifficultyAttributes
            {
                HoldNoteCount = score.BeatmapShort.CountSliders,
                IsConvert = score.BeatmapShort.IsConvert,
                NoteCount = score.BeatmapShort.CountCircles,
                OverallDifficulty = score.BeatmapShort.OverallDifficulty,
            };

            hitWindows = getLegacyHitWindows(score, attributes);
        }

        private double totalJudgements => countPerfect + countOk + countGreat + countGood + countMeh + countMiss;
        private double totalSuccessfulJudgements => countPerfect + countOk + countGreat + countGood + countMeh;

        public double? ComputeEstimatedUr()
        {
            if (totalSuccessfulJudgements == 0 || attributes.NoteCount + attributes.HoldNoteCount == 0)
                return null;

            // Lazer LN heads are the same as Notes, so return NoteCount + HoldNoteCount for lazer scores.
            double logNoteCount = isLegacyScore ? Math.Log(attributes.NoteCount) : Math.Log(attributes.NoteCount + attributes.HoldNoteCount);
            double logHoldCount = Math.Log(attributes.HoldNoteCount);

            double noteHeadPortion = (double)(attributes.NoteCount + attributes.HoldNoteCount) / (attributes.NoteCount + attributes.HoldNoteCount * 2);
            double tailPortion = (double)attributes.HoldNoteCount / (attributes.NoteCount + attributes.HoldNoteCount * 2);

            double likelihoodGradient(double d)
            {
                if (d <= 0)
                    return 0;

                // Since tails have a higher deviation, find the deviation values for notes/heads and tails that average out to the final deviation value.
                double dNote = d / Math.Sqrt(noteHeadPortion + tailPortion * Math.Pow(tail_deviation_multiplier, 2));
                double dTail = dNote * tail_deviation_multiplier;

                JudgementProbs pNotes = pNote(dNote);
                // Since lazer tails have the same hit behaviour as Notes, return pNote instead of pHold for them.
                JudgementProbs pHolds = isLegacyScore ? pHold(dNote, dTail) : pNote(dTail, tail_multiplier);

                return -totalProb(pNotes, pHolds, logNoteCount, logHoldCount);
            }

            // Finding the minimum of the function returns the most likely deviation for the hit results. UR is deviation * 10.
            double deviation = FindMinimum.OfScalarFunction(likelihoodGradient, 30);

            return deviation;
        }

        private double[] getLegacyHitWindows(ScoreSlim score, ManiaDifficultyAttributes attributes)
        {
            double[] legacyHitWindows = new double[5];

            double overallDifficulty = attributes.OverallDifficulty;
            double greatWindowLeniency = 0;
            double goodWindowLeniency = 0;

            // When converting beatmaps to osu!mania in stable, the resulting hit window sizes are dependent on whether the beatmap's OD is above or below 4.
            if (attributes.IsConvert)
            {
                overallDifficulty = 10;

                if (attributes.OverallDifficulty <= 4)
                {
                    greatWindowLeniency = 13;
                    goodWindowLeniency = 10;
                }
            }

            double windowMultiplier = 1;

            if (score.Mods.Any(m => m is "HR"))
                windowMultiplier *= 1 / 1.4;
            else if (score.Mods.Any(m => m is "EZ"))
                windowMultiplier *= 1.4;

            legacyHitWindows[0] = Math.Floor(16 * windowMultiplier);
            legacyHitWindows[1] = Math.Floor((64 - 3 * overallDifficulty + greatWindowLeniency) * windowMultiplier);
            legacyHitWindows[2] = Math.Floor((97 - 3 * overallDifficulty + goodWindowLeniency) * windowMultiplier);
            legacyHitWindows[3] = Math.Floor((127 - 3 * overallDifficulty) * windowMultiplier);
            legacyHitWindows[4] = Math.Floor((151 - 3 * overallDifficulty) * windowMultiplier);

            return legacyHitWindows;
        }
        private struct JudgementProbs
        {
            public double PMax;
            public double P300;
            public double P200;
            public double P100;
            public double P50;
            public double P0;
        }

        // Probability of hitting a certain judgement on Notes given a deviation. The multiplier is for lazer LN tails, which are 1.5x as lenient.
        private JudgementProbs pNote(double d, double multiplier = 1)
        {
            JudgementProbs probabilities = new JudgementProbs
            {
                PMax = logDiff(0, logPcNote(hitWindows[0] * multiplier, d)),
                P300 = logDiff(logPcNote(hitWindows[0] * multiplier, d), logPcNote(hitWindows[1] * multiplier, d)),
                P200 = logDiff(logPcNote(hitWindows[1] * multiplier, d), logPcNote(hitWindows[2] * multiplier, d)),
                P100 = logDiff(logPcNote(hitWindows[2] * multiplier, d), logPcNote(hitWindows[3] * multiplier, d)),
                P50 = logDiff(logPcNote(hitWindows[3] * multiplier, d), logPcNote(hitWindows[4] * multiplier, d)),
                P0 = logPcNote(hitWindows[4] * multiplier, d)
            };

            return probabilities;
        }

        // Probability of hitting a certain judgement on legacy LNs, which have different hit behaviour to Notes and lazer LNs.
        private JudgementProbs pHold(double dHead, double dTail)
        {
            JudgementProbs probabilities = new JudgementProbs
            {
                PMax = logDiff(0, logPcHold(hitWindows[0] * 1.2, dHead, dTail)),
                P300 = logDiff(logPcHold(hitWindows[0] * 1.2, dHead, dTail), logPcHold(hitWindows[1] * 1.1, dHead, dTail)),
                P200 = logDiff(logPcHold(hitWindows[1] * 1.1, dHead, dTail), logPcHold(hitWindows[2], dHead, dTail)),
                P100 = logDiff(logPcHold(hitWindows[2], dHead, dTail), logPcHold(hitWindows[3], dHead, dTail)),
                P50 = logDiff(logPcHold(hitWindows[3], dHead, dTail), logPcHold(hitWindows[4], dHead, dTail)),
                P0 = logPcHold(hitWindows[4], dHead, dTail)
            };

            return probabilities;
        }

        /// <summary>
        /// Combines pNotes and pHolds/pTails into a single probability value for each judgement, and compares them to the judgements of the play.
        /// </summary>
        private double totalProb(JudgementProbs firstProbs, JudgementProbs secondProbs, double firstObjectCount, double secondObjectCount)
        {
            // firstObjectCount can be either Notes, or Notes + Holds, as stable LN heads don't behave like Notes but lazer LN heads do.
            double pMax = logSum(firstProbs.PMax + firstObjectCount, secondProbs.PMax + secondObjectCount) - Math.Log(totalSuccessfulJudgements);
            double p300 = logSum(firstProbs.P300 + firstObjectCount, secondProbs.P300 + secondObjectCount) - Math.Log(totalSuccessfulJudgements);
            double p200 = logSum(firstProbs.P200 + firstObjectCount, secondProbs.P200 + secondObjectCount) - Math.Log(totalSuccessfulJudgements);
            double p100 = logSum(firstProbs.P100 + firstObjectCount, secondProbs.P100 + secondObjectCount) - Math.Log(totalSuccessfulJudgements);
            double p50 = logSum(firstProbs.P50 + firstObjectCount, secondProbs.P50 + secondObjectCount) - Math.Log(totalSuccessfulJudgements);

            double totalProb = Math.Exp(
                (countPerfect * pMax
                 + (countGreat + 0.5) * p300
                 + countGood * p200
                 + countOk * p100
                 + countMeh * p50) / totalSuccessfulJudgements
            );

            return totalProb;
        }

        /// <summary>
        /// The log complementary probability of getting a certain judgement with a certain deviation.
        /// </summary>
        /// <returns>
        /// A value from 0 (log of 1, 0% chance) to negative infinity (log of 0, 100% chance).
        /// </returns>
        private double logPcNote(double window, double deviation) => logErfc(window / (deviation * Math.Sqrt(2)));

        /// <summary>
        /// The log complementary probability of getting a certain judgement with a certain deviation.
        /// Exclusively for stable LNs, as they give a result from 2 error values (total error on the head + the tail).
        /// </summary>
        /// <returns>
        /// A value from 0 (log of 1, 0% chance) to negative infinity (log of 0, 100% chance).
        /// </returns>
        private double logPcHold(double window, double headDeviation, double tailDeviation)
        {
            double root2 = Math.Sqrt(2);

            double logPcHead = logErfc(window / (headDeviation * root2));

            // Calculate the expected value of the distance from 0 of the head hit, given it lands within the current window.
            // We'll subtract this from the tail window to approximate the difficulty of landing both hits within 2x the current window.
            double beta = window / headDeviation;
            double z = Normal.CDF(0, 1, beta) - 0.5;
            double expectedValue = headDeviation * (Normal.PDF(0, 1, 0) - Normal.PDF(0, 1, beta)) / z;

            double logPcTail = logErfc((2 * window - expectedValue) / (tailDeviation * root2));

            return logDiff(logSum(logPcHead, logPcTail), logPcHead + logPcTail);
        }

        private double logErfc(double x) => x <= 5
            ? Math.Log(SpecialFunctions.Erfc(x))
            : -Math.Pow(x, 2) - Math.Log(x * Math.Sqrt(Math.PI)); // This is an approximation, https://www.desmos.com/calculator/kdbxwxgf01

        private double logSum(double firstLog, double secondLog)
        {
            double maxVal = Math.Max(firstLog, secondLog);
            double minVal = Math.Min(firstLog, secondLog);

            // 0 in log form becomes negative infinity, so return negative infinity if both numbers are negative infinity.
            if (double.IsNegativeInfinity(maxVal))
            {
                return maxVal;
            }

            return maxVal + Math.Log(1 + Math.Exp(minVal - maxVal));
        }

        private double logDiff(double firstLog, double secondLog)
        {
            double maxVal = Math.Max(firstLog, secondLog);

            // Avoid negative infinity - negative infinity (NaN) by checking if the higher value is negative infinity. See comment in logSum.
            if (double.IsNegativeInfinity(maxVal))
            {
                return maxVal;
            }

            return firstLog + SpecialFunctions.Log1p(-Math.Exp(-(firstLog - secondLog)));
        }
    }
}
