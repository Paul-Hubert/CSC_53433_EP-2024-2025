namespace EvoSim.Testing
{
    /// <summary>
    /// A test-only action: stay, and eat a snack of <see cref="Energy"/> through a custom interaction that feeds the animal
    /// with <see cref="InteractionContext.Feed"/> (a meal counted like any other).
    /// </summary>
    /// <example>
    /// <code>
    /// var w = New(name: "feed").Ecology(40f, 0f, 0f, prey: s =&gt; s.Population(0).Action&lt;FeedAction&gt;("snack"),
    ///                                   predator: s =&gt; s.Population(0), regrow: 0f)
    ///     .Phase&lt;ActPhase&gt;().Build();
    /// var a = Place.Animal(w.FindSpecies("prey"), Place.At(10, 10));
    /// a.Choose("snack");
    /// a.SetStat("energy", 50f);
    /// w.Advance(1);
    /// Assert.AreEqual(1, a.Meals);
    /// </code>
    /// </example>
    public class FeedAction : AnimalAction
    {
        /// <summary>The snack's energy, in points.</summary>
        public float Energy = 5f;

        /// <summary>"stay and eat a snack (a test probe)".</summary>
        protected override string DefaultDescription => "stay and eat a snack (a test probe)";

        /// <summary>Stays, and eats the snack on arrival, which is at once.</summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            c.Stay();
            c.OnArrival(new Snack(Energy));
        }

        /// <summary>The custom interaction: feeds the actor a fixed energy.</summary>
        sealed class Snack : IInteraction
        {
            readonly float energy;   // the snack's energy, in points

            /// <summary>A snack worth <paramref name="e"/> points.</summary>
            public Snack(float e) { energy = e; }

            /// <summary>0.5 m; the animal stays, so it is always within reach.</summary>
            public float Reach => 0.5f;

            /// <summary>Feeds the actor the snack's energy (a meal).</summary>
            public void Apply(Animal actor, InteractionContext c) => c.Feed(actor, energy);
        }
    }
}
