using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §1: a thirst stat that grows every tick (with DiesOfThirst) and kills at a threshold.</summary>
    public class Thirst : SpeciesModule
    {
        [SerializeField, Min(0f), Tooltip("Thirst added every tick (reference 0.5).")]
        float perTick = 0.5f;
        [SerializeField, Min(1f), Tooltip("Thirst at which an animal dies (reference 100).")]
        float deadly = 100f;

        public StatId Value { get; private set; }
        public float PerTick => perTick;
        public float Deadly => deadly;

        public override void Declare(SpeciesBuilder b) =>
            Value = b.DeclareStat("thirst", StatStart.Fixed(0f), 0f, deadly);

        public override void WritePromptRules(PromptWriter w) =>
            w.Rule($"Thirst grows every step; an animal dies of thirst at {Bands.Number(deadly)}. Drinking resets it.");

        public void Configure(float growth, float lethal)
        {
            perTick = growth;
            deadly = lethal;
        }
    }
}
