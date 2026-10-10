using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvoSim
{
    /// <summary>Work a phase waits for: brain answers, mutations (TICK-03, SPACE-13).</summary>
    public abstract class Pending
    {
        /// <summary>True once every answer is in.</summary>
        public abstract bool IsDone { get; }

        /// <summary>Blocks the calling thread until done (the Freeze wait mode, SPACE-14).</summary>
        public abstract void Wait();

        /// <summary>A pending that is already done.</summary>
        public static Pending Done() => new DonePending();

        /// <summary>Done when the task completes.</summary>
        public static Pending Of(Task task) => task == null ? Done() : new TaskPending(task);

        /// <summary>Done when all of them are done; null entries count as done.</summary>
        public static Pending All(IReadOnlyList<Pending> parts)
        {
            if (parts == null || parts.Count == 0) return null;
            if (parts.Count == 1) return parts[0];
            return new AllPending(parts);
        }

        sealed class DonePending : Pending
        {
            public override bool IsDone => true;
            public override void Wait() { }
        }

        sealed class TaskPending : Pending
        {
            readonly Task task;
            public TaskPending(Task task) { this.task = task; }
            public override bool IsDone => task.IsCompleted;
            public override void Wait()
            {
                // The task's own exception is the brain's or mutator's business: they turn it into a failure.
                try { task.Wait(); } catch (AggregateException) { }
            }
        }

        sealed class AllPending : Pending
        {
            readonly List<Pending> parts;
            public AllPending(IReadOnlyList<Pending> parts) { this.parts = new List<Pending>(parts); }
            public override bool IsDone
            {
                get
                {
                    foreach (var p in parts) if (p != null && !p.IsDone) return false;
                    return true;
                }
            }
            public override void Wait()
            {
                foreach (var p in parts) p?.Wait();
            }
        }
    }
}
