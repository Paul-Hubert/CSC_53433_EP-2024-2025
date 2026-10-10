using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>A test-only sense with two tokens; it returns the animal's id parity (T-CORE-02).</summary>
    public class ProbeSense : Sense
    {
        static readonly string[] tokens = { "even", "odd" };
        public int Reads;
        public override IReadOnlyList<string> Tokens => tokens;
        public override int Read(Animal a, SenseContext s) { Reads++; return a.Id % 2; }
    }
}
