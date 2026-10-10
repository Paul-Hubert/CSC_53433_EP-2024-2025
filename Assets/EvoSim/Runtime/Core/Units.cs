namespace EvoSim
{
    /// <summary>Units of the simulation: meters on the horizontal plane, ticks for time (README, Units).</summary>
    public static class Units
    {
        /// <summary>Moving less than this (1 mm) counts as not moving (ANIM-23).</summary>
        public const float Epsilon = 0.001f;
    }
}
