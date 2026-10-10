using System;

namespace EvoSim
{
    /// <summary>A pending completed by code: Wait() runs the given completion if it isn't done yet.</summary>
    public sealed class ManualPending : Pending
    {
        readonly Action complete;
        volatile bool done;

        /// <param name="completeOnWait">What Wait() does when not done yet (null: Wait spins until another thread completes it).</param>
        public ManualPending(Action completeOnWait = null) { complete = completeOnWait; }

        public override bool IsDone => done;
        public void SetDone() => done = true;

        public override void Wait()
        {
            if (done) return;
            if (complete != null) { complete(); done = true; return; }
            var spin = new System.Threading.SpinWait();
            while (!done) spin.SpinOnce();
        }
    }
}
