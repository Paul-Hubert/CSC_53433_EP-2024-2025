using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvoSim
{
    /// <summary>Work a phase waits for: brain answers, mutations (TICK-03, SPACE-13).</summary>
    /// <remarks>
    /// <para>A phase returns one from <see cref="TickPhase.WaitsFor"/>. The World reads <see cref="IsDone"/> on the
    /// main thread: in Responsive mode it comes back on a later frame while the work is not done; in Freeze mode it
    /// calls <see cref="Wait"/>, which blocks until it is (SPACE-14).</para>
    /// <para>Build one with <see cref="Done"/>, <see cref="Of"/> (a Task), <see cref="All"/> (several), or
    /// <see cref="ManualPending"/>. A subclass must make IsDone safe to read from the main thread while the work runs on
    /// another, and Wait must never need the main thread to finish (it is blocked).</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override Pending WaitsFor(TickContext t) => Pending.Of(requestTask);   // done when the task completes
    /// </code>
    /// </example>
    public abstract class Pending
    {
        /// <summary>True once every answer is in.</summary>
        public abstract bool IsDone { get; }

        /// <summary>Blocks the calling thread until done (the Freeze wait mode, SPACE-14).</summary>
        public abstract void Wait();

        /// <summary>A pending that is already done.</summary>
        /// <returns>A new pending whose <see cref="IsDone"/> is always true.</returns>
        public static Pending Done() => new DonePending();

        /// <summary>Done when the task completes.</summary>
        /// <param name="task">The work, or null for nothing to wait for.</param>
        /// <returns>A pending done when the task completes, faulted or not; an already done one for a null task.</returns>
        /// <remarks>Its Wait swallows the task's exception: the brain or mutator that made the task turns it into a failure.</remarks>
        public static Pending Of(Task task) => task == null ? Done() : new TaskPending(task);

        /// <summary>Done when all of them are done; null entries count as done.</summary>
        /// <param name="parts">The parts to wait for.</param>
        /// <returns>Null when there is no part (nothing to wait for), the part itself when there is one, else a pending
        /// done when every part is.</returns>
        public static Pending All(IReadOnlyList<Pending> parts)
        {
            if (parts == null || parts.Count == 0) return null;
            if (parts.Count == 1) return parts[0];
            return new AllPending(parts);
        }

        /// <summary>A pending that is always done.</summary>
        sealed class DonePending : Pending
        {
            /// <summary>Always true.</summary>
            /// <inheritdoc/>
            public override bool IsDone => true;
            /// <summary>Returns at once.</summary>
            /// <inheritdoc/>
            public override void Wait() { }
        }

        /// <summary>A pending done when its task completes.</summary>
        sealed class TaskPending : Pending
        {
            readonly Task task;        // the work
            /// <summary>Wraps a task.</summary>
            public TaskPending(Task task) { this.task = task; }
            /// <summary>True when the task completed, faulted, or was cancelled.</summary>
            /// <inheritdoc/>
            public override bool IsDone => task.IsCompleted;
            /// <summary>Blocks until the task completes; its exception is not thrown here.</summary>
            /// <inheritdoc/>
            public override void Wait()
            {
                // The task's own exception is the brain's or mutator's business: they turn it into a failure.
                try { task.Wait(); } catch (AggregateException) { }
            }
        }

        /// <summary>A pending done when all its parts are.</summary>
        sealed class AllPending : Pending
        {
            readonly List<Pending> parts;      // a copy: the caller may reuse its list
            /// <summary>Waits for a copy of these parts.</summary>
            public AllPending(IReadOnlyList<Pending> parts) { this.parts = new List<Pending>(parts); }
            /// <summary>True when every part is done; null parts count as done.</summary>
            /// <inheritdoc/>
            public override bool IsDone
            {
                get
                {
                    foreach (var p in parts) if (p != null && !p.IsDone) return false;
                    return true;
                }
            }
            /// <summary>Waits for each part in turn.</summary>
            /// <inheritdoc/>
            public override void Wait()
            {
                foreach (var p in parts) p?.Wait();
            }
        }
    }
}
