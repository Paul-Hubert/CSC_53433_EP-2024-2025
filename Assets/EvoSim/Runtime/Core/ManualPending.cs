using System;

namespace EvoSim
{
    /// <summary>A pending completed by code: Wait() runs the given completion if it isn't done yet.</summary>
    /// <remarks>
    /// Whoever does the work calls <see cref="SetDone"/> when it is finished, from any thread. With a completion, the
    /// Freeze wait mode does the work at once on the main thread; without one, Wait spins until another thread calls
    /// SetDone.
    /// </remarks>
    /// <example>
    /// A test phase whose answer comes a few Advance calls later (Testing/Probes/DelayedPhase):
    /// <code>
    /// pending = new ManualPending(() =&gt; { });          // Freeze: Wait() completes it at once
    /// if (World.AdvanceCalls &gt;= readyAtCall) pending.SetDone();
    /// return pending;
    /// </code>
    /// </example>
    public sealed class ManualPending : Pending
    {
        readonly Action complete;      // what Wait does when the work isn't done yet; null = spin until SetDone
        volatile bool done;            // set by SetDone or Wait, read from any thread

        /// <summary>Makes a pending that is not done yet.</summary>
        /// <param name="completeOnWait">What Wait() does when not done yet (null: Wait spins until another thread completes it).</param>
        public ManualPending(Action completeOnWait = null) { complete = completeOnWait; }

        /// <inheritdoc/>
        public override bool IsDone => done;
        /// <summary>Marks the work done; safe from any thread.</summary>
        public void SetDone() => done = true;

        /// <summary>Returns at once when done; else runs the completion and marks it done, or, without one, spins until done.</summary>
        /// <inheritdoc/>
        public override void Wait()
        {
            if (done) return;
            if (complete != null) { complete(); done = true; return; }
            var spin = new System.Threading.SpinWait();
            while (!done) spin.SpinOnce();
        }
    }
}
