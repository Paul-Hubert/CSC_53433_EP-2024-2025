namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §3: walk to the nearest visible water's shore and drink there, which resets thirst.</summary>
    /// <remarks>
    /// <para><b>What it adds.</b> An action the brain can choose, "drink": the animal walks to the shore of the nearest water
    /// in sight and its <see cref="Thirst"/> drops to 0 on arrival. No water in sight: it searches (ACT-04).</para>
    /// <para><b>Mechanism.</b> An <see cref="AnimalAction"/> that only gives an intent (walk to the shore) and an interaction
    /// on arrival: a custom <see cref="IInteraction"/>, applied by the act phase within its reach (ACT-10). The action never
    /// changes the animal itself. <see cref="IsRelevant"/> reads the "Water" token of <see cref="NearestWaterSense"/>.</para>
    /// <para><b>In a scene.</b> Add it on a new GameObject named "drink" under the species' actions, with a
    /// <see cref="TextGene"/> on the same GameObject whose founders say when to drink ("Drink whenever you pass water.",
    /// "Drink only when very thirsty."). The species needs a <see cref="Thirst"/>, and the world a ground with water (the
    /// terrain ground). Save the GameObject as a prefab in <c>Modules/</c> to share it.</para>
    /// </remarks>
    [RequiresModule(typeof(Thirst))]
    public class DrinkAction : AnimalAction
    {
        Thirst thirst;   // the species' thirst module, found in Initialize

        /// <summary>"go to the nearest visible water and drink".</summary>
        protected override string DefaultDescription => "go to the nearest visible water and drink";

        /// <summary>Finds the species' <see cref="Thirst"/> module.</summary>
        public override void Initialize() => thirst = Species.Module<Thirst>();

        /// <summary>
        /// Walks to the shore of the nearest water within vision and drinks on arrival; no water in sight (or no ground):
        /// searches (ACT-04).
        /// </summary>
        /// <inheritdoc/>
        public override void Act(Animal a, ActContext c)
        {
            var water = c.Ground != null ? c.Ground.NearestWater(a.Position, c.Vision(a)) : null;
            if (water == null) { c.Search(); return; }
            c.WalkTo(water.Value.Shore, 0f);
            c.OnArrival(new Drink(thirst));
        }

        /// <summary>True when the "Water" sense reports water in sight (CTRL-10); false without that sense.</summary>
        /// <inheritdoc/>
        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token("Water");
            return t != null && t != "none";
        }

        /// <summary>Reports an error (V-20) when the species has no <see cref="Thirst"/> module.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Thirst>() == null) report.Error("V-20", this, "Drink needs a Thirst module to reset.");
        }

        /// <summary>The contact effect: thirst back to zero (ACT-10 reach).</summary>
        sealed class Drink : IInteraction
        {
            readonly Thirst thirst;   // the module whose stat is reset

            /// <summary>A drink that resets the stat of <paramref name="t"/>.</summary>
            public Drink(Thirst t) { thirst = t; }

            /// <summary>0.5 m: the animal drinks once within half a meter of the shore point.</summary>
            public float Reach => 0.5f;

            /// <summary>Sets the animal's thirst to 0.</summary>
            public void Apply(Animal a, InteractionContext c) { if (thirst != null) a[thirst.Value] = 0f; }
        }
    }
}
