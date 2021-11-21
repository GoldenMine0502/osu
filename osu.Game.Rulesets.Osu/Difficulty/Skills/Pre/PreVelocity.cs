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
    public class PreVelocity : OsuStrainSkill
    {
        private double currentStrain = 0;

        private double skillMultiplier => 23.25;
        private double strainDecayBase => 0.15;

        private readonly double hitWindowGreat;
        private readonly bool withSliders;

        public PreVelocity(Mod[] mods, double hitWindowGreat, bool withSliders) : base(mods)
        {
            this.hitWindowGreat = hitWindowGreat;
            this.withSliders = withSliders;
        }

        private double strainValueOf(Skill[] preSkills, int index, DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count == 0 || Previous[0].BaseObject is Spinner)
            {
                return 0;
            }

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuLastObj = (OsuDifficultyHitObject)Previous[0];

            var preSliderVelocityVariance = ((StrainSkill)preSkills[0]).GetAllStrainPeaks();
            var preAngleVariance = ((StrainSkill)preSkills[1]).GetAllStrainPeaks();
            var preDistanceVariance = ((StrainSkill)preSkills[2]).GetAllStrainPeaks();
            var preFingerControlVariance = ((StrainSkill)preSkills[3]).GetAllStrainPeaks();

            double sliderBonus = 0.99 + preSliderVelocityVariance[index];
            double totalBonus = Math.Pow(
                Math.Pow(0.99 + preAngleVariance[index], 1.1) *
                Math.Pow(0.99 + preDistanceVariance[index], 1.1) *
                Math.Pow(0.99 + preFingerControlVariance[index], 1.1)
                , (1.0 / 1.1));

            // Calculate the velocity to the current hitobject, which starts with a base distance / time assuming the last object is a hitcircle.
            double currVelocity = osuCurrObj.JumpDistance / osuCurrObj.StrainTime;

            if (osuLastObj.BaseObject is Slider && withSliders)
            {
                double sliderStartNerf = osuCurrObj.BaseObject is Slider ? hitWindowGreat : 0;

                double movementVelocity = osuCurrObj.MovementDistance / (osuCurrObj.MovementTime + sliderStartNerf); // calculate the movement velocity from slider end to current object
                double travelVelocity = osuCurrObj.TravelDistance / osuCurrObj.TravelTime * sliderBonus; // calculate the slider velocity from slider head to slider end.

                currVelocity = Math.Max(currVelocity, movementVelocity + travelVelocity); // take the larger total combined velocity.
            }

            currVelocity *= totalBonus;

            return currVelocity;
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
