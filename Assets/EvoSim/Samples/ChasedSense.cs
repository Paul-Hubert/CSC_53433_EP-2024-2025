using System.Collections.Generic;

namespace EvoSim.Samples
{
    /// <summary>
    /// Recipe 22 §13: whether the nearest threat is hunting this animal — its target at the start of the tick
    /// (SENSE-04). Lets a cannibal tell a hungry neighbour from a peaceful one.
    /// </summary>
    public class ChasedSense : Sense
    {
        static readonly IReadOnlyList<string> tokens = System.Array.AsReadOnly(new[] { "no", "yes" });   // shared, so read-only (ARCH-07)

        public override IReadOnlyList<string> Tokens => tokens;
        protected override string DefaultLabel => "Chased";

        public override int Read(Animal a, SenseContext s)
        {
            var threat = s.NearestAnimal(a, AnimalSet.Threats, s.Vision(a));
            if (threat == null) return 0;
            var t = threat.Value.Animal;
            bool hunting = t.CurrentAction != null && t.CurrentAction.Hunts;          // the reference hunt, or any action that hunts
            return hunting && s.CurrentTarget(t) == a ? 1 : 0;              // the hunter's target this tick
        }

        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 ? (token == 1 ? "Something is chasing me." : "Nothing is chasing me.")
                                  : $"{Label}: {tokens[token]}.";
    }
}
