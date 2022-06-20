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

            while (rhythmStart < historicalNoteCount - 2 && current.StartTime - current.Previous(rhythmStart).StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)current.Previous(i - 1);
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)current.Previous(i);
                //OsuDifficultyHitObject lastObj = (OsuDifficultyHitObject)current.Previous(i + 1);

                OsuHitObject currBaseObj = (OsuHitObject)currObj.BaseObject;

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                //double currDelta = currObj.StrainTime;
                //double prevDelta = prevObj.StrainTime;
                //double lastDelta = lastObj.StrainTime;

                double scalingFactor = 50.0 / currBaseObj.Radius;

                //double currAngle = currObj.Angle != null ? currObj.Angle.Value : 0;
                //double prevAngle = prevObj.Angle != null ? prevObj.Angle.Value : 0;

                double radius = currBaseObj.Radius;
                double distance = currObj.LazyJumpDistance / scalingFactor; // not normalised
                double effectiveRatio = 1;

                // stacked note nerf
                effectiveRatio *= Math.Clamp(distance / (radius * 2), 0, 1);

                // nerf angle changes slowly, buff angle changes rapidly.
                // when bpm is 100 = 0.66
                // when bpm is 170 = 1.13
                // almost 90 means bpm 170
                //effectiveRatio *= 100.0 / Math.Max(90, currDelta);

                if (currObj.BaseObject is Slider) // bpm change is into slider, this is easy acc window
                    effectiveRatio *= 0.25;

                if (prevObj.BaseObject is Slider) // bpm change was from a slider, this is easier typically than circle -> circle
                    effectiveRatio *= 0.5;

                //if (Math.Max(currDelta, prevDelta) < 1.25 * Math.Min(currDelta, prevDelta)) // previous increase happened a note ago, 1/1->1/2-1/4, dont want to buff this.
                //    effectiveRatio *= 0.25;

                double angleRatio = AngleDifference.EvaluateDifficultyOf(currObj) / currObj.StrainTime;

                double result = angleRatio * effectiveRatio * angle_multiplier * currHistoricalDecay;

                angleComplexitySum += result;

                if (angleComplexitySum < 0)
                    angleComplexitySum = 0;
            }

            return angleComplexitySum * total_angle_ratio_multiplier; //produces multiplier that can be applied to strain. range [1, infinity) (not really though)
        }

        private static double calcAngleDifference(double prevAngle, double currAngle)
        {
            double angle120 = Math.PI / 3 * 2;
            double angle45 = Math.PI / 4;

            // peak pi/2 
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
