using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only sense with two tokens; it returns the animal's id parity (T-CORE-02).</summary>
    public class ProbeSense : Sense
    {
        static readonly string[] tokens = { "even", "odd" };   // backs Tokens; shared, never changed
        /// <summary>The calls to <see cref="Read"/> so far.</summary>
        public int Reads;

        /// <summary>"even" and "odd".</summary>
        public override IReadOnlyList<string> Tokens => tokens;

        /// <summary>Counts the read; "even" or "odd" after the animal's id.</summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s) { Reads++; return a.Id % 2; }
    }
}
