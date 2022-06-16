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
        private const double slider_velocity_cap = 0.5;
        private const double slider_velocity_multiplier = 1.5;
        private const double total_slider_velocity_multiplier = 1;

        private const double distance_ratio_not_stacked_multiplier = 0.15;
        private const double distance_ratio_multiplier = 1;
        private const double total_distance_ratio_multiplier = 0.25;

        private const double angle_multiplier = 0.1;
        private const double total_angle_ratio_multiplier = 5;

        private const int history_time_max = 5000; // 5 seconds of complexities max.

        private double skillMultiplier => 9;
        private double strainDecayBase => 0.5;

        private double currentStrain;

        protected override double DifficultyMultiplier => 1;
        protected override int HistoryLength => 32;

        public Alternative(Mod[] mods)
            : base(mods)
        {
        }

        //public override double DifficultyValue()
        //{
        //    var peaks = GetCurrentStrainPeaks().Where(p => p > 0);
        //    List<double> strains = peaks.OrderByDescending(d => d).ToList();

        //    double difficulty = 0;
        //    foreach (double strain in strains.OrderByDescending(d => d))
        //    {
        //        difficulty += strain * 0.01;
        //    }

        //    return difficulty;
        //}

        private double calculateDistanceRatio(OsuDifficultyHitObject prevObj, OsuDifficultyHitObject currObj)
        {
            //OsuHitObject prevBaseObj = (OsuHitObject)prevObj.BaseObject;
            OsuHitObject currBaseObj = (OsuHitObject)currObj.BaseObject;

            double scalingFactor = 52.0 / currBaseObj.Radius;

            double radius = currBaseObj.Radius;
            double distance = currObj.LazyJumpDistance / scalingFactor; // not normalised

            double distanceRatioOfStacked = Math.Clamp(distance / (radius * 2), 0, 1);
            double distanceRatioOfNotStacked = Math.Max(0, (distance - (radius * 2) / (radius * 2)) * distance_ratio_not_stacked_multiplier);

            double totalDistanceRatio = distanceRatioOfStacked + distanceRatioOfNotStacked;
            double time = currObj.DeltaTime;

            double value = totalDistanceRatio * scalingFactor / time;

            return value;
        }

        private double calculateDistanceRatioComplexity(DifficultyHitObject current)
        {
            double distanceRatioComplexitySum = 0;

            int rhythmStart = 0;

            while (rhythmStart < Previous.Count - 2 && current.StartTime - Previous[rhythmStart].StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)Previous[i - 1];
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)Previous[i];
                OsuDifficultyHitObject lastObj = (OsuDifficultyHitObject)Previous[i + 1];

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                double prevDistanceValue = calculateDistanceRatio(lastObj, prevObj);
                double currDistanceValue = calculateDistanceRatio(prevObj, currObj);

                double totalDistanceValue = Math.Abs(currDistanceValue - prevDistanceValue) * distance_ratio_multiplier;

                distanceRatioComplexitySum += totalDistanceValue * currHistoricalDecay;
            }

            return distanceRatioComplexitySum * total_distance_ratio_multiplier;
        }

        private double calculateSliderVelocityComplexity(DifficultyHitObject current)
        {
            double sliderVelocityComplexitySum = 0;

            int rhythmStart = 0;

            while (rhythmStart < Previous.Count - 2 && current.StartTime - Previous[rhythmStart].StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)Previous[i - 1];
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)Previous[i];
                OsuDifficultyHitObject lastObj = (OsuDifficultyHitObject)Previous[i + 1];

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                // only sliders affect to result
                if (prevObj.BaseObject is Slider && lastObj.BaseObject is Slider)
                {
                    double currTravelVelocity = prevObj.TravelDistance / prevObj.TravelTime;
                    double pastTravelVelocity = lastObj.TravelDistance / lastObj.TravelTime;

                    // calculates velocity changes.
                    // gives a cap to ignore small velocity changes.
                    // gives a multiplier to mangify big slider velocity changes.
                    double result = Math.Max(0, Math.Abs(pastTravelVelocity - currTravelVelocity) - slider_velocity_cap) * slider_velocity_multiplier;

                    sliderVelocityComplexitySum += result * currHistoricalDecay;

                    if (sliderVelocityComplexitySum < 0)
                        sliderVelocityComplexitySum = 0;
                }
            }

            return sliderVelocityComplexitySum * total_slider_velocity_multiplier;
        }

        private double calculateAngleComplexity(DifficultyHitObject current)
        {
            double angleComplexitySum = 0;

            int rhythmStart = 0;

            while (rhythmStart < Previous.Count - 2 && current.StartTime - Previous[rhythmStart].StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)Previous[i - 1];
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)Previous[i];
                //OsuDifficultyHitObject lastObj = (OsuDifficultyHitObject)Previous[i + 1];
                OsuHitObject currBaseObj = (OsuHitObject)currObj.BaseObject;

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                double currDelta = currObj.StrainTime;
                double prevDelta = prevObj.StrainTime;
                //double lastDelta = lastObj.StrainTime;

                double scalingFactor = 52.0 / currBaseObj.Radius;

                double currAngle = currObj.Angle != null ? currObj.Angle.Value : 0;
                double prevAngle = prevObj.Angle != null ? prevObj.Angle.Value : 0;

                double radius = currBaseObj.Radius;
                double distance = currObj.LazyJumpDistance / scalingFactor; // not normalised
                double effectiveRatio = 1;

                // stacked note nerf
                effectiveRatio *= Math.Clamp(distance / (radius * 2), 0, 1);

                // nerf angle changes slowly, buff angle changes rapidly.
                // when bpm is 100 = 0.66
                // when bpm is 170 = 1.13
                // almost 90 means bpm 170
                effectiveRatio *= 100.0 / Math.Max(90, currDelta);

                if (Previous[i - 1].BaseObject is Slider) // bpm change is into slider, this is easy acc window
                    effectiveRatio *= 0.25;

                if (Previous[i].BaseObject is Slider) // bpm change was from a slider, this is easier typically than circle -> circle
                    effectiveRatio *= 0.5;

                if (Math.Max(currDelta, prevDelta) < 1.25 * Math.Min(currDelta, prevDelta)) // previous increase happened a note ago, 1/1->1/2-1/4, dont want to buff this.
                    effectiveRatio *= 0.25;

                double angleRatio = calcAngleDifference(prevAngle, currAngle);

                double result = angleRatio * effectiveRatio * angle_multiplier * currHistoricalDecay;

                angleComplexitySum += result;

                if (angleComplexitySum < 0)
                    angleComplexitySum = 0;
            }

            return angleComplexitySum * total_angle_ratio_multiplier; //produces multiplier that can be applied to strain. range [1, infinity) (not really though)
        }

        private double calcAngleDifference(double prevAngle, double currAngle)
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
            if (currAngle - prevAngle < angle45)
            {
                value -= Math.Sin(angleDifference - Math.Min(angleDifference, angle45) / 2);
            }

            return Math.Pow(value, 3);
        }

        private double strainValueOf(DifficultyHitObject current)
        {
            double distanceComplexity = calculateDistanceRatioComplexity(current);
            double alternativeComplexity = calculateAngleComplexity(current);
            double sliderVelocityComplexity = calculateSliderVelocityComplexity(current);

            //var currObj = (OsuDifficultyHitObject)current;
            //string timeStr = (int)(currObj.StartTime / 60000) + ":" + (int)((currObj.StartTime - ((int)currObj.StartTime / 60000) * 60000) / 1000) + "." + (int)Math.Round(currObj.StartTime % 1000);


            double value = distanceComplexity + alternativeComplexity + sliderVelocityComplexity;

            //if (value >= 3)
            //    Console.WriteLine(distanceComplexity + ", " + alternativeComplexity + ", " + sliderVelocityComplexity + ", " + timeStr);

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
