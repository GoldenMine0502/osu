// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Alternative : OsuStrainSkill
    {
        private const double alternative_cap = 0.5;

        private double skillMultiplier => 15;
        private double strainDecayBase => 0.2;

        private double currentStrain;

        protected override double DifficultyMultiplier => 1;

        public Alternative(Mod[] mods)
            : base(mods)
        {
        }

        private double strainValueOf(DifficultyHitObject current)
        {
            double distanceComplexity = DistanceComplexityEvaluator.EvaluateDifficultyOf(current);
            double angleComplexity = AngleComplexityEvaluator.EvaluateDifficultyOf(current);
            double sliderVelocityComplexity = SVComplexityEvaluator.EvaluateDifficultyOf(current);

            // ignore small alternative value.
            double value = Math.Max(distanceComplexity + angleComplexity + sliderVelocityComplexity - alternative_cap, 0);

            return value;
        }

        private double strainDecay(double decayBase, double ms) => Math.Pow(decayBase, ms / 1000);

        private double strainDecay(double ms) => strainDecay(strainDecayBase, ms);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current) => currentStrain * strainDecay(time - current.Previous(0).StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += strainValueOf(current) * skillMultiplier;

            return currentStrain;
        }
    }
}
