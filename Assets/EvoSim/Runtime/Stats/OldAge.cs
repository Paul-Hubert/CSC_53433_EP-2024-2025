using UnityEngine;

namespace EvoSim
{
    /// <summary>Death of old age: age above the maximum (reference 1 500 ticks; ANIM-40).</summary>
    /// <remarks>
    /// The death phase ages every animal by one tick before it asks the death rules, so an animal dies of old age in the
    /// tick its age first exceeds <see cref="MaxAge"/>.
    /// </remarks>
    public class OldAge : DeathRule
    {
        [SerializeField, Min(1), Tooltip("Maximum age, in ticks (reference 1 500).")]
        int maxAge = 1500;

        /// <summary>The maximum age, in ticks: an animal older than this dies (reference 1 500).</summary>
        public int MaxAge => maxAge;

        /// <summary>One cause: <c>"old_age"</c>.</summary>
        public override System.Collections.Generic.IEnumerable<string> Causes => new[] { "old_age" };

        /// <summary><c>"old_age"</c> when the animal's age is above <see cref="MaxAge"/>, else null.</summary>
        /// <inheritdoc/>
        public override string CauseOfDeath(Animal a) => a.Age > maxAge ? "old_age" : null;

        /// <summary>Sets the maximum age from code (WorldBuilder, scenarios).</summary>
        /// <param name="ticks">The maximum age, in ticks; at least 1.</param>
        public void SetMaxAge(int ticks) => maxAge = Mathf.Max(1, ticks);
    }
}
