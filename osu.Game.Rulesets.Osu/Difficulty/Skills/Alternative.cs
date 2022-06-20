// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    public class Alternative : OsuStrainSkill
    {

        private double skillMultiplier => 11;
        private double strainDecayBase => 0.3;

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

            double value = distanceComplexity + angleComplexity + sliderVelocityComplexity;

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
