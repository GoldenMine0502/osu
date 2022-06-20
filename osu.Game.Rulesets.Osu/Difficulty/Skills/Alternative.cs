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



        private double strainValueOf(DifficultyHitObject current)
        {
            double distanceComplexity = DistanceComplexityEvaluator.EvaluateDifficultyOf(current);
            double angleComplexity = AngleComplexityEvaluator.EvaluateDifficultyOf(current);
            double sliderVelocityComplexity = SVComplexityEvaluator.EvaluateDifficultyOf(current);

            //var currObj = (OsuDifficultyHitObject)current;
            //string timeStr = (int)(currObj.StartTime / 60000) + ":" + (int)((currObj.StartTime - ((int)currObj.StartTime / 60000) * 60000) / 1000) + "." + (int)Math.Round(currObj.StartTime % 1000);


            double value = distanceComplexity + angleComplexity + sliderVelocityComplexity;

            //if (value >= 3)
            //    Console.WriteLine(distanceComplexity + ", " + alternativeComplexity + ", " + sliderVelocityComplexity + ", " + timeStr);

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
