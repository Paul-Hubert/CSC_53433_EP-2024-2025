namespace EvoSim.Testing
{
    /// <summary>A test-only action: stay, and eat a snack of <see cref="Energy"/> through a custom interaction.</summary>
    public class FeedAction : AnimalAction
    {
        public float Energy = 5f;

        protected override string DefaultDescription => "stay and eat a snack (a test probe)";

        public override void Act(Animal a, ActContext c)
        {
            c.Stay();
            c.OnArrival(new Snack(Energy));
        }

        sealed class Snack : IInteraction
        {
            readonly float energy;
            public Snack(float e) { energy = e; }
            public float Reach => 0.5f;
            public void Apply(Animal actor, InteractionContext c) => c.Feed(actor, energy);
        }
    }
}
