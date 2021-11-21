using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills.Pre
{
    public class PreSliderVelocityVariance : OsuStrainSkill
    {
        private double currentStrain = 0;

        private double skillMultiplier => 0.1;
        private double strainDecayBase => 0.5;

        public PreSliderVelocityVariance(Mod[] mods) : base(mods)
        {
            
        }

        private double lastVelocity = -1;
        private double velocityCap = 0.25;

        private double strainValueOf(Skill[] preSkills, int index, DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count == 0 || Previous[0].BaseObject is Spinner)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            double sliderBonus = 0;
            if (osuCurrent.LastObject is Slider)
            {
                double travelVelocity = osuCurrent.TravelDistance / osuCurrent.TravelTime;

                double adaptedVelocity = Math.Max(travelVelocity - velocityCap, 0);
                if (lastVelocity >= 0)
                    sliderBonus = Math.Max(adaptedVelocity - lastVelocity, (lastVelocity - adaptedVelocity) / 2);

                lastVelocity = adaptedVelocity;
            }

            return sliderBonus;
        }

        protected override double CalculateInitialStrain(double time) => currentStrain * strainDecay(time - Previous[0].StartTime);
        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double StrainValueAt(Skill[] preSkills, int index, DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += strainValueOf(preSkills, index, current) * skillMultiplier;

            return currentStrain;
        }
    }
}
