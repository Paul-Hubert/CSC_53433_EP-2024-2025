using UnityEngine;

namespace EvoSim
{
    /// <summary>A component under the World (not under a species): a phase or a service.</summary>
    public abstract class WorldModule : MonoBehaviour
    {
        public World World { get; private set; }

        /// <summary>Called by the World in a fixed order, before any animal exists (ARCH-04).</summary>
        public virtual void Initialize() { }

        /// <summary>Report configuration problems (editor, and at the start of a run).</summary>
        public virtual void Validate(ValidationReport report) { }

        /// <summary>
        /// Called once after every module is initialised and validated, before founders spawn, in hierarchy order:
        /// place initial content here (the first food items), when it depends on other services (cover).
        /// </summary>
        public virtual void Begin() { }

        /// <summary>Called once when the run stops, whatever the reason (RAND-20).</summary>
        public virtual void OnRunStopped(string reason) { }

        internal void Bind(World world) => World = world;
    }
}
