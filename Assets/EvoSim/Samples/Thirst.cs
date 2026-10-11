using UnityEngine;

namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §1: a thirst stat that grows every tick (with DiesOfThirst) and kills at a threshold.</summary>
    /// <remarks>
    /// <para><b>What it adds.</b> A stat <c>thirst</c> on every animal of the species, from 0 (at birth) to
    /// <see cref="Deadly"/>, and a rule line in the prompt. Thirst itself only declares and describes the stat: it grows,
    /// and kills, through <see cref="DiesOfThirst"/>, and <see cref="DrinkAction"/> sets it back to 0.</para>
    /// <para><b>Mechanism.</b> A <see cref="SpeciesModule"/> that declares a stat in <see cref="Declare"/> and writes a
    /// prompt rule in <see cref="WritePromptRules"/>. Other modules find it with <c>Species.Module&lt;Thirst&gt;()</c> and
    /// read the stat through <see cref="Value"/>; a <see cref="LevelSense"/> with stat "thirst" lets the brain sense it, with
    /// no code.</para>
    /// <para><b>In a scene.</b> Add it as a component on a new child GameObject of the species (next to its other stats),
    /// with a <see cref="DiesOfThirst"/>. Checklist of 22 §1: the cause "thirst" appears in death events and in
    /// <c>stats.csv</c> (<c>deaths_thirst</c>).</para>
    /// </remarks>
    public class Thirst : SpeciesModule
    {
        [SerializeField, Min(0f), Tooltip("Thirst added every tick (reference 0.5).")]
        float perTick = 0.5f;
        [SerializeField, Min(1f), Tooltip("Thirst at which an animal dies (reference 100).")]
        float deadly = 100f;

        /// <summary>The thirst stat: read and write it with <c>a[thirst.Value]</c>.</summary>
        public StatId Value { get; private set; }

        /// <summary>The thirst added every tick, in points.</summary>
        public float PerTick => perTick;

        /// <summary>The thirst at which an animal dies, in points: also the stat's maximum.</summary>
        public float Deadly => deadly;

        /// <summary>Declares the stat <c>thirst</c>: 0 at birth, kept in [0, <see cref="Deadly"/>].</summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b) =>
            Value = b.DeclareStat("thirst", StatStart.Fixed(0f), 0f, deadly);

        /// <summary>
        /// Adds the rule line "Thirst grows every step; an animal dies of thirst at 100. Drinking resets it.", with the
        /// number taken from <see cref="Deadly"/> (PROMPT-03).
        /// </summary>
        /// <inheritdoc/>
        public override void WritePromptRules(PromptWriter w) =>
            w.Rule($"Thirst grows every step; an animal dies of thirst at {Bands.Number(deadly)}. Drinking resets it.");

        /// <summary>Sets the module from code (tests).</summary>
        /// <param name="growth">The thirst added every tick, in points.</param>
        /// <param name="lethal">The thirst at which an animal dies, in points.</param>
        public void Configure(float growth, float lethal)
        {
            perTick = growth;
            deadly = lethal;
        }
    }
}
