// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public class DistanceComplexityEvaluator
    {
        private const double distance_ratio_not_stacked_multiplier = 0.15;
        private const double distance_ratio_multiplier = 1;
        private const double total_distance_ratio_multiplier = 0.25;

        private const int history_time_max = 5000; // 5 seconds of complexities max.
        private static double calculateDistanceRatio(OsuDifficultyHitObject prevObj, OsuDifficultyHitObject currObj)
        {
            OsuHitObject currBaseObj = (OsuHitObject)currObj.BaseObject;

            double scalingFactor = 52.0 / currBaseObj.Radius;

            double radius = currBaseObj.Radius;
            double distance = currObj.LazyJumpDistance / scalingFactor; // not normalised

            // stacked distance ratio. 0 when current object completely covers prev obj, 0~1 half stacked, and 1 not stacked
            double distanceRatioOfStacked = Math.Clamp(distance / (radius * 2), 0, 1);

            // not stacked distance ratio. It is not for stacked notes, but it is used when the distance change is too large. this uses a multiple of the diameter.
            double distanceRatioOfNotStacked = Math.Max(0, (distance - (radius * 2) / (radius * 2)) * distance_ratio_not_stacked_multiplier);

            double totalDistanceRatio = distanceRatioOfStacked + distanceRatioOfNotStacked;

            double value = totalDistanceRatio;

            return value;
        }

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            double distanceRatioComplexitySum = 0;

            int rhythmStart = 0;

            int historicalNoteCount = Math.Min(current.Index, 32);

            while (rhythmStart < historicalNoteCount - 2 && current.StartTime - current.Previous(rhythmStart).StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)current.Previous(i - 1);
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)current.Previous(i);
                OsuDifficultyHitObject lastObj = (OsuDifficultyHitObject)current.Previous(i + 1);

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                double prevDistanceValue = calculateDistanceRatio(lastObj, prevObj);
                double currDistanceValue = calculateDistanceRatio(prevObj, currObj);

                double totalDistanceValue = Math.Abs(currDistanceValue - prevDistanceValue) / currObj.StrainTime * distance_ratio_multiplier;

                distanceRatioComplexitySum += totalDistanceValue * currHistoricalDecay;
            }

            return distanceRatioComplexitySum * total_distance_ratio_multiplier;
        }
    }
}
