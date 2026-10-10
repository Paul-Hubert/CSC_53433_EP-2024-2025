namespace EvoSim.Testing
{
    /// <summary>A test phase that reads the blackboard (logging what it sees), then optionally writes it.</summary>
    public class BlackboardPhase : TickPhase
    {
        public Blackboard Board;
        public bool Writes;

        public override void Run(TickContext t)
        {
            Board.Seen.Add(t.Tick + ":" + gameObject.name + "=" + Board.Value);
            if (Writes) Board.Value = t.Tick + 1;
        }
    }
}
