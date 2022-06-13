// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Alternative : OsuStrainSkill
    {
        private const double distance_ratio_multiplier = 0.1;
        private const double angle_multiplier = 0.1;
        private const double total_distance_ratio_multiplier = 1;
        private const double total_angle_ratio_multiplier = 3;
        private const int history_time_max = 5000; // 1 seconds of calculatingAngleBonus max.

        private double skillMultiplier => 5;
        private double strainDecayBase => 0.5;

        private double currentStrain;

        protected override double DifficultyMultiplier => 1;
        protected override int HistoryLength => 32;

        public Alternative(Mod[] mods)
            : base(mods)
        {
        }

        private double calculateDistanceRatio(OsuDifficultyHitObject prevObj, OsuDifficultyHitObject currObj)
        {
            //OsuHitObject prevBaseObj = (OsuHitObject)prevObj.BaseObject;
            OsuHitObject currBaseObj = (OsuHitObject)currObj.BaseObject;

            double scalingFactor = 52.0 / currBaseObj.Radius;

            double radius = currBaseObj.Radius;
            double distance = currObj.LazyJumpDistance / scalingFactor; // not normalised

            double distanceRatioOfStacked = Math.Clamp(distance / (radius * 2), 0, 1);
            double distanceRatioOfNotStacked = Math.Max(0, (distance - (radius * 2) / (radius * 2)) * distance_ratio_multiplier);

            double totalDistanceRatio = distanceRatioOfStacked + distanceRatioOfNotStacked;
            //double bpm180 = 15000.0 / 180.0;
            double time = currObj.DeltaTime;

            double value = totalDistanceRatio * scalingFactor / time * total_distance_ratio_multiplier;

            return value;
        }

        private double calculateTotalDistanceRatio(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count <= 1 || Previous[0].BaseObject is Spinner)
                return 0;

            var currObj = (OsuDifficultyHitObject)current;
            var lastObj = (OsuDifficultyHitObject)Previous[0];
            var lastLastObj = (OsuDifficultyHitObject)Previous[1];
            //double distanceRatio = calculateDistanceRatio(current);

            double prevDistanceValue = calculateDistanceRatio(lastLastObj, lastObj);
            double currDistanceValue = calculateDistanceRatio(lastObj, currObj);

            //double currAngle = currObj.Angle != null ? currObj.Angle.Value : 0;
            //double prevAngle = lastObj.Angle != null ? lastObj.Angle.Value : 0;

            //double distanceValue = Math.Abs(currDistanceValue - prevDistanceValue) * total_distance_ratio_multiplier;
            //double angleDifferenceValue = Math.Sin(Math.Max(0, currAngle - prevAngle - Math.PI / 2)) * total_angle_ratio_multiplier;
            double totalDistanceValue = Math.Abs(currDistanceValue - prevDistanceValue) * total_distance_ratio_multiplier;

            return totalDistanceValue;
        }

        private double calculateAlternativeComplexity(DifficultyHitObject current)
        {
            double angleComplexitySum = 0;

            int rhythmStart = 0;

            while (rhythmStart < Previous.Count - 2 && current.StartTime - Previous[rhythmStart].StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)Previous[i - 1];
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)Previous[i];
                OsuDifficultyHitObject lastObj = (OsuDifficultyHitObject)Previous[i + 1];
                OsuHitObject currBaseObj = (OsuHitObject)currObj.BaseObject;

                //if (currObj.BaseObject is Slider) continue;

                double currDelta = currObj.StrainTime;
                double prevDelta = prevObj.StrainTime;
                double lastDelta = lastObj.StrainTime;

                double scalingFactor = 52.0 / currBaseObj.Radius;

                double currAngle = currObj.Angle != null ? currObj.Angle.Value : 0;
                double prevAngle = prevObj.Angle != null ? prevObj.Angle.Value : 0;

                double radius = currBaseObj.Radius;
                double distance = currObj.LazyJumpDistance / scalingFactor; // not normalised
                //double distanceLast = prevObj.LazyJumpDistance / scalingFactor; // not normalised
                double effectiveRatio = 1;

                if (distance < radius * 2) // stacked note nerf
                    effectiveRatio *= distance / (radius * 2);

                // nerf angle changes slowly, buff angle changes rapidly.
                // when bpm is 100 = 0.66 ^ 2
                // when bpm is 170 = 1.13 ^ 2
                // almost 90 means bpm 170
                effectiveRatio *= Math.Pow(100.0 / Math.Max(90, currDelta), 3);

                if (lastDelta > prevDelta + 10 && prevDelta > currDelta + 10) // previous increase happened a note ago, 1/1->1/2-1/4, dont want to buff this.
                    effectiveRatio *= 0.125;

                double angleRatio = (Math.Max(prevAngle, currAngle) > 0 ? Math.Pow(Math.Sin(Math.PI / 2 * Math.Abs(prevAngle - currAngle) / Math.Max(prevAngle, currAngle)), 2) : 0);

                // 각도 차이가 나지 않는 경우 너프
                //if (Math.Abs(prevAngle - currAngle) <= Math.PI / 4)
                //{
                //    double minusAngleRatio = Math.Max(prevAngle, currAngle) > 0 ? Math.Pow(Math.Sin((Math.PI / 4 - Math.Abs(prevAngle - currAngle)) * 2), 2) : 0;
                //    angleRatio -= minusAngleRatio / effectiveRatio;
                //}

                double result = angleRatio * effectiveRatio * angle_multiplier;
                // 0 to pi / 2
                //double bpm130time = 15000.0 / 130.0;
                //double time = currObj.DeltaTime;

                // 130bpm 이상만 잡아냄
                //double angleValue = Math.Sin(Math.Max(0, Math.Abs(prevAngle - currAngle) - Math.PI / 2)) * (Math.Max(0, time - bpm130time) / bpm130time) * angle_multiplier;
                //double angleMinusValue = Math.Sin(Math.Min(0, Math.Abs(prevAngle - currAngle) - Math.PI / 2)) * angle_multiplier;
                // 이전 값을 줄이고 현재 값을 더함. /*Math.Pow(angleComplexitySum, 0.99)*/
                //if (angleComplexitySum > 0)
                //    angleComplexitySum *= strainDecay(0.1, time);

                //Console.WriteLine(angleMinusValue);

                //angleComplexitySum += angleMinusValue * 0.5;
                angleComplexitySum += result;

                if (angleComplexitySum < 0)
                    angleComplexitySum = 0;
            }

            return angleComplexitySum * total_angle_ratio_multiplier; //produces multiplier that can be applied to strain. range [1, infinity) (not really though)
        }

        private double calcWideAngleBonus(double angle) => Math.Pow(Math.Sin(3.0 / 4 * (Math.Min(5.0 / 6 * Math.PI, Math.Max(Math.PI / 6, angle)) - Math.PI / 6)), 2);

        private double calcAcuteAngleBonus(double angle) => 1 - calcWideAngleBonus(angle);

        private double strainValueOf(DifficultyHitObject current)
        {
            double distanceComplexity = calculateTotalDistanceRatio(current);
            double alternativeComplexity = calculateAlternativeComplexity(current);

            //var currObj = (OsuDifficultyHitObject)current;
            //string timeStr = (int)(currObj.StartTime / 60000) + ":" + (int)((currObj.StartTime - ((int)currObj.StartTime / 60000) * 60000) / 1000) + "." + (int)Math.Round(currObj.StartTime % 1000);


            double value = distanceComplexity + alternativeComplexity;

            //if (value > 2)
            //    Console.WriteLine(distanceComplexity + ", " + alternativeComplexity + ", " + timeStr);

            return value;
        }

        private double strainDecay(double decayBase, double ms) => Math.Pow(decayBase, ms / 1000);

        private double strainDecay(double ms) => strainDecay(strainDecayBase, ms);

        protected override double CalculateInitialStrain(double time) => currentStrain * strainDecay(time - Previous[0].StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += strainValueOf(current) * skillMultiplier;

            return currentStrain;
        }
    }
}
