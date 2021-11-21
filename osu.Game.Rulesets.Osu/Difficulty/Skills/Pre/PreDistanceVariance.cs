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
    public class PreDistanceVariance : PreStrainSkill
    {
        protected override double SkillMultiplier => 0.01;

        protected override double StrainDecayBase => 0.5;

        public PreDistanceVariance(Mod[] mods) : base(mods)
        {

        }

        protected override double StrainValueOf(Skill[] preSkills, int index, DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count == 0 || Previous[0].BaseObject is Spinner)
            {
                return 0;
            }

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuLastObj = (OsuDifficultyHitObject)Previous[0];

            double radius = ((OsuHitObject)osuCurrObj.BaseObject).Radius * osuCurrObj.ScalingFactor;

            double variance = Math.Max(osuCurrObj.MovementDistance, radius) / Math.Max(osuLastObj.MovementDistance, radius);
            if (variance < 1) variance = (1 / (variance * 2));
            if (variance < 1) variance = 1;

            return variance - 1;
        }
    }
}
