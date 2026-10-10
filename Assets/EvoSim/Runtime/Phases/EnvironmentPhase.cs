namespace EvoSim
{
    /// <summary>Resource layers regrow, each with its own stream; entities move, rot or vanish (TICK-04 #9, ENV-03, ENV-22).</summary>
    public class EnvironmentPhase : TickPhase
    {
        public override void Run(TickContext t)
        {
            var services = World.Services;
            for (int i = 0; i < services.Count; i++)
            {
                if (services[i] is ResourceLayer layer) layer.Regrow(t.Stream(layer.StreamName));
                else if (services[i] is EntityKind kind) kind.UpdateEntities(t);
            }
        }
    }
}
