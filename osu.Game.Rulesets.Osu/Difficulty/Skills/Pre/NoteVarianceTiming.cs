using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills.Pre
{
    public class NoteVarianceTiming : PrePerNoteStrainSkill
    {
        public NoteVarianceTiming(IBeatmap beatmap, Mod[] mods, double clockRate) : base(beatmap, mods, clockRate)
        {
        }

        protected override double SkillMultiplier => 1;

        protected override double StrainDecayBase => 0.5; // i want to affect on next 4 notes at least 0.1

        private double lastTerm = -1;
        protected override double StrainValueOf(PrePerNoteStrainSkill[] preSkills, int index, DifficultyHitObject current)
        {
            var osuCurrent = (OsuDifficultyHitObject)current;
            double term = osuCurrent.StrainTime;

            double timingBonus = 0;

            if(lastTerm == -1)
            {
                lastTerm = term;
            } else
            {
                // we want to skip 1/2, and 2/1, 3/1, 4/1.....
                double calculatedValue = Math.Min(lastTerm % term, term % lastTerm);
                if (calculatedValue > 0)
                {
                    timingBonus = Math.Min(calculatedValue / lastTerm, calculatedValue / term);
                    lastTerm = term;
                }
            }

            return timingBonus;
        }

        // returning timingBonus
        //private double calculateOneSide(double bigTerm, double smallTerm)
        //{
        //    double calculatedValue = bigTerm % smallTerm;

        //    return calculatedValue / smallTerm;
        //}
    }
}
