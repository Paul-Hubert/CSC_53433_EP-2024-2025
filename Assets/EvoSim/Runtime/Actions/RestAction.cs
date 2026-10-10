namespace EvoSim
{
    /// <summary>Stay still to catch your breath and save energy (07 §4). Never searches; the metabolism charges its rest cost.</summary>
    public class RestAction : AnimalAction
    {
        protected override string DefaultDescription => "stay still to catch your breath and save energy";

        public override bool Rests => true;

        public override void Act(Animal a, ActContext c) => c.Stay();
    }
}
