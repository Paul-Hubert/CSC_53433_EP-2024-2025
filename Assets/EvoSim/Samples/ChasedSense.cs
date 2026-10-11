using System.Collections.Generic;

namespace EvoSim.Samples
{
    /// <summary>
    /// Recipe 22 §13: whether the nearest threat is hunting this animal — its target at the start of the tick
    /// (SENSE-04). Lets a cannibal tell a hungry neighbour from a peaceful one.
    /// </summary>
    /// <remarks>
    /// <para><b>What it adds.</b> A sense labelled "Chased" with two tokens, "no" and "yes": yes when the nearest threat in
    /// sight is doing an action that hunts (<see cref="AnimalAction.Hunts"/>) and its target is this animal. V2 texts:
    /// "Something is chasing me." / "Nothing is chasing me.".</para>
    /// <para><b>Mechanism.</b> A plain <see cref="Sense"/>: fixed <see cref="Tokens"/>, a <see cref="Read"/> that only asks
    /// the <see cref="SenseContext"/> (the nearest threat, and <see cref="SenseContext.CurrentTarget"/>, read from the state
    /// at the start of the tick), and its own <see cref="Write"/> for the V2 sentences.</para>
    /// <para><b>In a scene.</b> Add it on a new child GameObject among the species' senses. It doubles the observation
    /// space; it only says yes when the species has threats (a predator, or itself for a cannibal).</para>
    /// </remarks>
    public class ChasedSense : Sense
    {
        static readonly IReadOnlyList<string> tokens = System.Array.AsReadOnly(new[] { "no", "yes" });   // shared, so read-only (ARCH-07)

        /// <summary>"no" and "yes".</summary>
        public override IReadOnlyList<string> Tokens => tokens;

        /// <summary>"Chased".</summary>
        protected override string DefaultLabel => "Chased";

        /// <summary>
        /// "yes" when the nearest threat within vision hunts and targets this animal, else "no" (also when no threat is in sight).
        /// </summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            var threat = s.NearestAnimal(a, AnimalSet.Threats, s.Vision(a));
            if (threat == null) return 0;
            var t = threat.Value.Animal;
            bool hunting = t.CurrentAction != null && t.CurrentAction.Hunts;          // the reference hunt, or any action that hunts
            return hunting && s.CurrentTarget(t) == a ? 1 : 0;              // the hunter's target this tick
        }

        /// <summary>V2: "Something is chasing me." or "Nothing is chasing me."; V1: "Chased: yes." or "Chased: no.".</summary>
        /// <inheritdoc/>
        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 ? (token == 1 ? "Something is chasing me." : "Nothing is chasing me.")
                                  : $"{Label}: {tokens[token]}.";
    }
}
