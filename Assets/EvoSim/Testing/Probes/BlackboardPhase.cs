namespace EvoSim.Testing
{
    /// <summary>
    /// A test phase that reads the blackboard (logging what it sees), then optionally writes it: only the phases after a
    /// writer see the new value in the same tick (T-TICK-02).
    /// </summary>
    public class BlackboardPhase : TickPhase
    {
        /// <summary>The blackboard shared by the test's phases; set it before the world runs.</summary>
        public Blackboard Board;
        /// <summary>True: after reading, the phase writes the next tick's number into <see cref="Blackboard.Value"/>.</summary>
        public bool Writes;

        /// <summary>Logs <c>"tick:name=value"</c> to the blackboard, then writes tick + 1 when <see cref="Writes"/> is set.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            Board.Seen.Add(t.Tick + ":" + gameObject.name + "=" + Board.Value);
            if (Writes) Board.Value = t.Tick + 1;
        }
    }
}
