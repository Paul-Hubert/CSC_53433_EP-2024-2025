namespace EvoSim
{
    /// <summary>A handle to a declared trait (ANIM-15): a per-animal number fixed at birth, with a default and a range.</summary>
    public readonly struct TraitId
    {
        public readonly int Index;
        public readonly string Name;
        public readonly float Default, Min, Max;
        /// <summary>False for every reference trait: traits don't change during life (ANIM-17).</summary>
        public readonly bool Changeable;

        public TraitId(int index, string name, float defaultValue, float min, float max, bool changeable)
        {
            Index = index; Name = name; Default = defaultValue; Min = min; Max = max; Changeable = changeable;
        }

        public bool IsValid => Name != null;

        /// <summary>A value clamped to the trait's range (ANIM-18).</summary>
        public float Clamp(float value) => value < Min ? Min : value > Max ? Max : value;

        public override string ToString() => Name;
    }
}
