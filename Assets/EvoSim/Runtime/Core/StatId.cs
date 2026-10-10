namespace EvoSim
{
    /// <summary>A handle to a declared stat (ANIM-10): its slot in the animal record, its name and its range.</summary>
    public readonly struct StatId
    {
        public readonly int Index;
        public readonly string Name;
        public readonly float Min, Max;
        /// <summary>The trait that caps this stat per animal (stamina ≤ stamina.max), or -1.</summary>
        public readonly int MaxTrait;

        public StatId(int index, string name, float min, float max, int maxTrait)
        {
            Index = index; Name = name; Min = min; Max = max; MaxTrait = maxTrait;
        }

        public bool IsValid => Name != null;

        /// <summary>Clamps a value to the stat's range; gains never pass the maximum (ANIM-22).</summary>
        public float Clamp(float value, float[] traits)
        {
            float max = MaxTrait >= 0 ? System.Math.Min(Max, traits[MaxTrait]) : Max;
            if (value > max) return max;
            if (value < Min) return Min;
            return value;
        }

        public override string ToString() => Name;
    }
}
