// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Difficulty.Evaluators
{
    public class AngleDifference
    {
        public static double EvaluateDifficultyOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || current.Index <= 1 || current.Previous(0).BaseObject is Spinner)
                return 0;

            OsuDifficultyHitObject nextObj = (OsuDifficultyHitObject)current.Next(0);
            OsuDifficultyHitObject currObj = (OsuDifficultyHitObject)current;
            //OsuDifficultyHitObject prevObj = (OsuDifficultyHitObject)current.Previous(0);

            if (nextObj == null)
                return 0;
            double nextAngle = nextObj.Angle != null ? nextObj.Angle.Value : 0;
            double currAngle = currObj.Angle != null ? currObj.Angle.Value : 0;

            return calcAngleDifference(currAngle, nextAngle);
        }
        private static double calcAngleDifference(double prevAngle, double currAngle)
        {
            double angle120 = Math.PI / 3 * 2;
            double angle45 = Math.PI / 4;

            // peak pi/2 
            double angleDifference = Math.Abs(prevAngle - currAngle);

            double value = Math.Sin(angleDifference);

            // acute -> obtuse buff
            if (currAngle - prevAngle > angle120)
            {
                value += Math.Sin(angleDifference - Math.Min(angleDifference, angle120));
            }

            // small change nerf
            if (angleDifference < angle45)
            {
                value -= Math.Sin(angleDifference - Math.Min(angleDifference, angle45)) / 2;
            }

            return Math.Pow(value, 3);
        }
    }
}
