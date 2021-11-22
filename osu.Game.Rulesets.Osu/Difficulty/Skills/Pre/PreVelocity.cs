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
    public class PreVelocity : PreStrainSkill
    {
        protected override double SkillMultiplier => 1.0;

        protected override double StrainDecayBase => 0;

        private readonly double hitWindowGreat;
        private readonly bool withSliders;

        public PreVelocity(Mod[] mods, double hitWindowGreat, bool withSliders) : base(mods)
        {
            this.hitWindowGreat = hitWindowGreat;
            this.withSliders = withSliders;
        }
        protected override double StrainValueOf(Skill[] preSkills, int index, DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count == 0 || Previous[0].BaseObject is Spinner)
            {
                return 0;
            }

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuLastObj = (OsuDifficultyHitObject)Previous[0];

            var preSliderVelocityVariance = ((PreStrainSkill)preSkills[0]).GetAllStrainPeaks();
            var preAngleVariance = ((PreStrainSkill)preSkills[1]).GetAllStrainPeaks();
            var preDistanceVariance = ((PreStrainSkill)preSkills[2]).GetAllStrainPeaks();
            var preFingerControlVariance = ((PreStrainSkill)preSkills[3]).GetAllStrainPeaks();
            var preRhythmBonus = ((PreStrainSkill)preSkills[4]).GetAllStrainPeaks();

            double sliderBonus = 0.95 + preSliderVelocityVariance[index];
            double totalBonus = Math.Pow(
                Math.Pow(0.95 + preAngleVariance[index], 1.1) *
                Math.Pow(0.95 + preDistanceVariance[index], 1.1) *
                Math.Pow(0.95 + preFingerControlVariance[index], 1.1) *
                Math.Pow(0.95 + preRhythmBonus[index], 1.1)
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
    }
}
