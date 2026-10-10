namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §3: walk to the nearest visible water's shore and drink there, which resets thirst.</summary>
    [RequiresModule(typeof(Thirst))]
    public class DrinkAction : AnimalAction
    {
        Thirst thirst;

        protected override string DefaultDescription => "go to the nearest visible water and drink";

        public override void Initialize() => thirst = Species.Module<Thirst>();

        public override void Act(Animal a, ActContext c)
        {
            var water = c.Ground != null ? c.Ground.NearestWater(a.Position, c.Vision(a)) : null;
            if (water == null) { c.Search(); return; }
            c.WalkTo(water.Value.Shore, 0f);
            c.OnArrival(new Drink(thirst));
        }

        public override bool IsRelevant(ObservationView o)
        {
            var t = o.Token("Water");
            return t != null && t != "none";
        }

        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Thirst>() == null) report.Error("V-20", this, "Drink needs a Thirst module to reset.");
        }

        /// <summary>The contact effect: thirst back to zero (ACT-10 reach).</summary>
        sealed class Drink : IInteraction
        {
            readonly Thirst thirst;
            public Drink(Thirst t) { thirst = t; }
            public float Reach => 0.5f;
            public void Apply(Animal a, InteractionContext c) { if (thirst != null) a[thirst.Value] = 0f; }
        }
    }
}
