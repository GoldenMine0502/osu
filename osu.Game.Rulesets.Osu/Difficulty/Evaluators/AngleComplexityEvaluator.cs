// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public class AngleComplexityEvaluator
    {
        private const double angle_multiplier = 5.5;
        private const double total_angle_ratio_multiplier = 4;
        private const int history_time_max = 5000; // 5 seconds of calculatingAngleComplexity max.

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            double angleComplexitySum = 0;

            int historicalNoteCount = Math.Min(current.Index, 32);

            int rhythmStart = 0;

            while (rhythmStart < historicalNoteCount - 3 && current.StartTime - current.Previous(rhythmStart).StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject nextObj = (OsuDifficultyHitObject)current.Next(0);
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)current.Previous(i - 1);
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)current.Previous(i);
                OsuDifficultyHitObject prevprevObj = (OsuDifficultyHitObject)current.Previous(i + 1);

                OsuHitObject currBaseObj = (OsuHitObject)currObj.BaseObject;

                if (nextObj == null)
                    continue;

                double nextAngle = nextObj.Angle != null ? nextObj.Angle.Value : 0;
                double currAngle = currObj.Angle != null ? currObj.Angle.Value : 0;

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                double scalingFactor = 50.0 / currBaseObj.Radius;

                double radius = currBaseObj.Radius;
                double distanceNext = nextObj.LazyJumpDistance / scalingFactor; // not normalised
                double distanceCurrent = currObj.LazyJumpDistance / scalingFactor; // not normalised
                double distancePrev = prevObj.LazyJumpDistance / scalingFactor; // not normalised
                double distancePrevPrev = prevprevObj.LazyJumpDistance / scalingFactor; // not normalised
                double effectiveRatio = 1;

                // stacked note nerf
                effectiveRatio *= Math.Clamp(distanceNext / (radius * 2), 0, 1);
                effectiveRatio *= Math.Clamp(distanceCurrent / (radius * 2), 0, 1);
                effectiveRatio *= Math.Clamp(distancePrev / (radius * 2), 0, 1);
                effectiveRatio *= Math.Clamp(distancePrevPrev / (radius * 2), 0, 1);

                if (currObj.BaseObject is Slider) // angle difference is into slider, this is easy acc window
                    effectiveRatio *= 0.25;

                if (prevObj.BaseObject is Slider) // angle difference was from a slider, this is easier typically than circle -> circle
                    effectiveRatio *= 0.5;

                double angleRatio = calcAngleDifference(currAngle, nextAngle) / currObj.StrainTime;

                double result = angleRatio * effectiveRatio * angle_multiplier * currHistoricalDecay;

                angleComplexitySum += result;

                if (angleComplexitySum < 0)
                    angleComplexitySum = 0;
            }

            return angleComplexitySum * total_angle_ratio_multiplier;
        }

        private static double calcAngleDifference(double prevAngle, double currAngle)
        {
            double angle120 = Math.PI / 3 * 2;
            double angle45 = Math.PI / 4;

            // peak bonus when angle difference is pi/2 
            double angleDifference = Math.Abs(prevAngle - currAngle);

            double value = Math.Sin(angleDifference);

            // acute -> obtuse buff
            if (currAngle - prevAngle > angle120)
            {
                value += Math.Sin(angleDifference - Math.Min(angleDifference, angle120));
            }

            // small change nerf
            if (angleDifference < angle45)
            {
                value -= Math.Sin(angleDifference - Math.Min(angleDifference, angle45)) / 2;
            }

            return Math.Pow(value, 3);
        }
    }
}
