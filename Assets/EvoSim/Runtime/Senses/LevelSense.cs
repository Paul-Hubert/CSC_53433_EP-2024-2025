using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// A stat as low, medium or high (ANIM-12): low &lt; low threshold ≤ medium ≤ high threshold &lt; high.
    /// Reference: energy 30 / 70, stamina prey 20 / 40, predators 10 / 20.
    /// </summary>
    public class LevelSense : Sense
    {
        [SerializeField, Tooltip("The stat to sense, by name (\"energy\", \"stamina\", \"thirst\"…).")]
        string stat = "energy";
        [SerializeField, Tooltip("Below this the level is low.")]
        float low = 30f;
        [SerializeField, Tooltip("Above this the level is high.")]
        float high = 70f;
        [SerializeField, Tooltip("V2 sentences for low, medium and high (SENSE-11).")]
        string[] words = { "I am hungry and weak.", "I have some energy.", "I am well fed and strong." };

        readonly List<string> tokens = new List<string> { "low", "medium", "high" };
        StatId value;

        public override IReadOnlyList<string> Tokens => tokens;
        public string Stat => stat;
        public float Low => low;
        public float High => high;

        protected override string DefaultLabel => stat.Length == 0 ? "Level" : char.ToUpperInvariant(stat[0]) + stat.Substring(1);

        public override void Initialize() => value = Species.Declarations.FindStat(stat);

        public override int Read(Animal a, SenseContext s)
        {
            float v = value.IsValid ? a[value] : 0f;
            return v < low ? 0 : v > high ? 2 : 1;
        }

        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 && words != null && token < words.Length && !string.IsNullOrEmpty(words[token])
                ? words[token]
                : $"{Label}: {tokens[token]}.";

        /// <summary>Sets the sense from code (WorldBuilder, tests).</summary>
        public LevelSense Configure(string statName, float lowThreshold, float highThreshold, string[] v2Words = null)
        {
            stat = statName;
            low = lowThreshold;
            high = highThreshold;
            if (v2Words != null) words = v2Words;
            return this;
        }

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
