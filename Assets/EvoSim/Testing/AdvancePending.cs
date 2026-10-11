namespace EvoSim.Testing
{
    /// <summary>
    /// A wait that ends after a number of <see cref="World.Advance"/> calls: how the test fakes answer late (30 §2).
    /// In the Freeze wait mode the World calls <see cref="Wait"/>, which ends it at once.
    /// </summary>
    /// <remarks>
    /// <see cref="ScriptedBrain"/> and <see cref="FakeMutator"/> return one when their <c>DelayCalls</c> is above 0. In the
    /// Responsive mode the tick pauses at the phase that waits and goes on in the Advance call that makes it done
    /// (SPACE-13, SPACE-14); both modes must give the same events hash.
    /// </remarks>
    public sealed class AdvancePending : Pending
    {
        readonly World world;     // the world whose Advance calls are counted
        readonly int readyAt;     // the value of World.AdvanceCalls at which the wait ends
        bool forced;              // set by Wait (Freeze mode): done at once

        /// <summary>Starts a wait that ends <paramref name="delayCalls"/> Advance calls from now.</summary>
        /// <param name="world">The world whose Advance calls are counted (<see cref="World.AdvanceCalls"/>).</param>
        /// <param name="delayCalls">How many more Advance calls before the answer is in; 0 = already done.</param>
        public AdvancePending(World world, int delayCalls)
        {
            this.world = world;
            readyAt = world.AdvanceCalls + delayCalls;
        }

        /// <summary>True once the world has had enough Advance calls, or after <see cref="Wait"/>.</summary>
        public override bool IsDone => forced || world.AdvanceCalls >= readyAt;

        /// <summary>Ends the wait at once: in the Freeze mode a test must never block on a fake.</summary>
        public override void Wait() => forced = true;
    }
}
