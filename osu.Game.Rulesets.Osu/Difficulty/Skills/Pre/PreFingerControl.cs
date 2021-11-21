﻿using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills.Pre
{
    public class PreFingerControl : OsuStrainSkill
    {
        private double currentStrain = 0;

        private double skillMultiplier => 0.01;
        private double strainDecayBase => 0.5;

        private const double min_doubletap_nerf = 0.0; // minimum value (eventually on stacked)
        private const double max_doubletap_nerf = 1.0; // maximum value
        private const double threshold_doubletap_contributing = 2.0; // minimum distance not influenced (2.0 means it is not stacked at least)

        public PreFingerControl(Mod[] mods) : base(mods)
        {

        }

        private double strainValueOf(Skill[] preSkills, int index, DifficultyHitObject current)
        {
            OsuDifficultyHitObject osuCurrent = (OsuDifficultyHitObject)current;

            if (osuCurrent.BaseObject is Spinner || osuCurrent.LastObject is Spinner)
                return 0;

            // Calculates the percentage of alternative.
            // 160bpm meant the bpm starting alternative to hit
            // When bpm is 160, it gives 0.
            // When bpm is 210, 1.
            // As a result, when bpm is 210, we consider the note has 100% of percentage of alternative
            double deltaTimeToBpm = 15000 / current.DeltaTime;
            double probablityAlternative = Math.Max((deltaTimeToBpm - 160.0), 0) / (210.0 - 160.0);

            // short (stacked) stream nerf
            double distance = osuCurrent.JumpDistance + osuCurrent.TravelDistance;
            double radius = ((OsuHitObject)osuCurrent.LastObject).Radius * osuCurrent.ScalingFactor;

            double multiplier = min_doubletap_nerf +
                Math.Clamp(distance / (radius * threshold_doubletap_contributing), 0.0, 1.0)
                * (max_doubletap_nerf - min_doubletap_nerf);
            probablityAlternative *= multiplier;

            return probablityAlternative;
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
