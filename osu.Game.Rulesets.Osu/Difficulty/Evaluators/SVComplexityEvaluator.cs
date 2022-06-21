// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public class SVComplexityEvaluator
    {
        private const double slider_velocity_cap = 0.5;
        private const double slider_velocity_multiplier = 85;
        private const double total_slider_velocity_multiplier = 1;

        private const int history_time_max = 5000; // 5 seconds of complexities max.

        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            double sliderVelocityComplexitySum = 0;

            int rhythmStart = 0;

            double lastTravelVelocity = -1;
            double lastTime = -1;

            int historicalNoteCount = Math.Min(current.Index, 32);

            while (rhythmStart < historicalNoteCount - 2 && current.StartTime - current.Previous(rhythmStart).StartTime < history_time_max)
                rhythmStart++;

            for (int i = rhythmStart; i > 0; i--)
            {
                OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)current.Previous(i - 1);
                OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)current.Previous(i);

                double currHistoricalDecay = (history_time_max - (current.StartTime - currObj.StartTime)) / history_time_max; // scales note 0 to 1 from history to now

                // only sliders affect to result
                if (prevObj.BaseObject is Slider)
                {
                    double currTravelVelocity = prevObj.TravelDistance / prevObj.TravelTime;

                    if (lastTravelVelocity >= 0)
                    {
                        // time from past slider head to current slider tail
                        double time = (prevObj.StartTime + prevObj.TravelTime) - lastTime;

                        // calculates velocity changes.
                        // gives a cap to ignore small velocity changes.
                        // gives a multiplier to mangify big slider velocity changes.
                        double result = Math.Max(0, Math.Abs(lastTravelVelocity - currTravelVelocity) - slider_velocity_cap) / time * slider_velocity_multiplier;

                        sliderVelocityComplexitySum += result * currHistoricalDecay;
                    }

                    lastTravelVelocity = currTravelVelocity;
                    lastTime = currObj.StartTime;

                    if (sliderVelocityComplexitySum < 0)
                        sliderVelocityComplexitySum = 0;
                }
            }

            return sliderVelocityComplexitySum * total_slider_velocity_multiplier;
        }
    }
}
