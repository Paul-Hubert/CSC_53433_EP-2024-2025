using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A stat as low, medium or high (ANIM-12): low &lt; low threshold ≤ medium ≤ high threshold &lt; high.
    /// Reference: energy 30 / 70, stamina prey 20 / 40, predators 10 / 20.
    /// </summary>
    /// <remarks>
    /// <para>The reference of teaching-path exercise 2 (<c>MyLevelSense</c>, 22 §0). What it does:</para>
    /// <list type="bullet">
    /// <item><description>Tokens, in this order: "low" (0), "medium" (1), "high" (2).</description></item>
    /// <item><description><see cref="Initialize"/> finds the stat named in the Inspector once, with
    /// <c>Species.Declarations.FindStat(stat)</c>.</description></item>
    /// <item><description><see cref="Read"/> reads the animal's value of that stat (<c>a[value]</c>, 0 when the stat is
    /// missing) and returns "low" below the low threshold, "high" above the high threshold, else "medium": a value equal to a
    /// threshold is medium.</description></item>
    /// <item><description><see cref="Write"/> gives "Energy: low." in V1, and in V2 the Inspector's sentence for the token
    /// ("I am hungry and weak.", "I have some energy.", "I am well fed and strong."), or the V1 text when that sentence is
    /// missing or empty.</description></item>
    /// <item><description>The default label is the stat's name with a capital letter ("energy" gives "Energy").</description></item>
    /// <item><description><see cref="Validate"/> reports V-31 when the low threshold is above the high one, V-20 when no
    /// module declares the stat.</description></item>
    /// </list>
    /// <para>One component per stat: the reference species have one for energy and one for stamina; recipe 22 §2 adds one
    /// for thirst with no code.</para>
    /// </remarks>
    public class LevelSense : Sense
    {
        [SerializeField, Tooltip("The stat to sense, by name (\"energy\", \"stamina\", \"thirst\"…).")]
        string stat = "energy";
        [SerializeField, Tooltip("Below this the level is low (reference energy 30, stamina 20 prey / 10 predators).")]
        float low = 30f;
        [SerializeField, Tooltip("Above this the level is high (reference energy 70, stamina 40 prey / 20 predators).")]
        float high = 70f;
        [SerializeField, Tooltip("V2 sentences for low, medium and high (SENSE-11).")]
        string[] words = { "I am hungry and weak.", "I have some energy.", "I am well fed and strong." };

        readonly List<string> tokens = new List<string> { "low", "medium", "high" };
        StatId value;   // the sensed stat, found in Initialize; invalid when no module declares it

        /// <inheritdoc/>
        public override IReadOnlyList<string> Tokens => tokens;

        /// <summary>The name of the sensed stat ("energy", "stamina"…).</summary>
        public string Stat => stat;

        /// <summary>The low threshold, in the stat's points: below it the level is "low".</summary>
        public float Low => low;

        /// <summary>The high threshold, in the stat's points: above it the level is "high".</summary>
        public float High => high;

        /// <summary>The stat's name with a capital letter ("Energy"), or "Level" when no stat is set.</summary>
        /// <inheritdoc/>
        protected override string DefaultLabel => stat.Length == 0 ? "Level" : char.ToUpperInvariant(stat[0]) + stat.Substring(1);

        /// <summary>Finds the sensed stat among the species' declarations, once, before the run.</summary>
        /// <inheritdoc/>
        public override void Initialize() => value = Species.Declarations.FindStat(stat);

        /// <summary>
        /// "low" (0) when the animal's stat is below <see cref="Low"/>, "high" (2) when above <see cref="High"/>, else
        /// "medium" (1). A missing stat reads as 0.
        /// </summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            float v = value.IsValid ? a[value] : 0f;
            return v < low ? 0 : v > high ? 2 : 1;
        }

        /// <summary>V1 "Energy: low."; V2 the Inspector's sentence for the token, or the V1 text when it has none (SENSE-11).</summary>
        /// <inheritdoc/>
        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 && words != null && token < words.Length && !string.IsNullOrEmpty(words[token])
                ? words[token]
                : $"{Label}: {tokens[token]}.";

        /// <summary>Sets the sense from code (WorldBuilder, tests), as the Inspector fields do.</summary>
        /// <param name="statName">The stat to sense, by name ("energy", "stamina"…).</param>
        /// <param name="lowThreshold">Below this the level is low, in the stat's points.</param>
        /// <param name="highThreshold">Above this the level is high, in the stat's points.</param>
        /// <param name="v2Words">The V2 sentences for low, medium and high; null keeps the current ones.</param>
        /// <returns>This sense, to chain calls.</returns>
        public LevelSense Configure(string statName, float lowThreshold, float highThreshold, string[] v2Words = null)
        {
            stat = statName;
            low = lowThreshold;
            high = highThreshold;
            if (v2Words != null) words = v2Words;
            return this;
        }

        /// <summary>Reports V-31 when the low threshold is above the high one, and V-20 when no module declares the stat.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (low > high)
                report.Error("V-31", this, $"Sense '{Label}': low threshold {low} above high threshold {high}.",
                              new ValidationFix("Swap", () => (low, high) = (high, low)));
            if (!Species.Declarations.FindStat(stat).IsValid)
                report.Error("V-20", this, $"Sense '{Label}' reads the stat '{stat}', which no module declares.");
        }
    }
}
