using System.Collections.Generic;

namespace EvoSim.Samples
{
    /// <summary>
    /// Recipe 22 §13: whether the nearest threat is hunting this animal — its target at the start of the tick
    /// (SENSE-04). Lets a cannibal tell a hungry neighbour from a peaceful one.
    /// </summary>
    public class ChasedSense : Sense
    {
        static readonly List<string> tokens = new List<string> { "no", "yes" };

        public override IReadOnlyList<string> Tokens => tokens;
        protected override string DefaultLabel => "Chased";

        public override int Read(Animal a, SenseContext s)
        {
            var threat = s.NearestAnimal(a, AnimalSet.Threats, s.Vision(a));
            if (threat == null) return 0;
            var t = threat.Value.Animal;
            bool hunting = t.CurrentAction is HuntAction;
            return hunting && s.CurrentTarget(t) == a ? 1 : 0;              // the hunter's target this tick
        }

        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 ? (token == 1 ? "Something is chasing me." : "Nothing is chasing me.")
                                  : $"{Label}: {tokens[token]}.";
    }
}
