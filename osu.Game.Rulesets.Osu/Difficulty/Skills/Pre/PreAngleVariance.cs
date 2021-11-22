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
    public class PreAngleVariance : PreStrainSkill
    {
        private const double angle_bonus_begin = Math.PI / 3;
        //private const double timing_threshold = 107; // 140bpm limit
        private const double timing_threshold = 75;

        protected override double SkillMultiplier => 0.05;

        protected override double StrainDecayBase => 0.75;

        private const double min_doubletap_nerf = 0.0; // minimum value (eventually on stacked)
        private const double max_doubletap_nerf = 1.0; // maximum value 
        private const double threshold_doubletap_contributing = 2.0; // minimum distance not influenced (2.0 means it is not stacked at least)

        public PreAngleVariance(Mod[] mods) : base(mods)
        {

        }

        protected override double StrainValueOf(Skill[] preSkills, int index, DifficultyHitObject current)
        {

            if (current.BaseObject is Spinner || current.LastObject is Spinner || Previous.Count == 0)
                return 0;

            var osuCurrObj = (OsuDifficultyHitObject)current;
            var osuLastObj = (OsuDifficultyHitObject)Previous[0];

            //double deltaTimeToBpm = 15000 / current.DeltaTime;
            double angle = osuCurrObj.Angle ?? angle_bonus_begin;

            double angleBonus = 0.0;

            if (Math.Max(osuCurrObj.StrainTime, osuLastObj.StrainTime) < 1.25 * Math.Min(osuCurrObj.StrainTime, osuLastObj.StrainTime)) // If rhythms are the same.
            {
                double lastAngle = osuLastObj.Angle ?? angle_bonus_begin;

                // obtuse bonus
                //angleBonus += 0.01 * Math.Max(Math.Sin(angle - angle_bonus_begin), 0);

                // bonus for changing angles frequently
                double angleVariance = Math.Sin(Math.Max(Math.Abs(angle - lastAngle) - Math.PI / 2, 0));

                angleBonus += angleVariance;

                // bonus for acute bonus at least 160bpm
                // 160bpm meant the bpm starting alternative to hit
                //if (deltaTimeToBpm >= 160)
                //{
                //    // limited to 210bpm
                //    angleBonus += Math.Sin(Math.Max((Math.PI / 2 - angle), 0))
                //        * Math.Min((deltaTimeToBpm - 160), 50) / 50
                //        * 0.05;
                //}
            }

            // short (stacked) stream nerf
            double distance = osuCurrObj.JumpDistance + osuCurrObj.TravelDistance;
            double radius = ((OsuHitObject)osuCurrObj.BaseObject).Radius * osuCurrObj.ScalingFactor;

            double multiplier = min_doubletap_nerf +
                Math.Max(Math.Min(distance / (radius * threshold_doubletap_contributing), 1.0), 0.0)
                * (max_doubletap_nerf - min_doubletap_nerf);
            angleBonus *= multiplier;


            return angleBonus;
        }
    }
}
