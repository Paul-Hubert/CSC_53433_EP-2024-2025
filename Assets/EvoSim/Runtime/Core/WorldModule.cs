using UnityEngine;

namespace EvoSim
{
    /// <summary>A component under the World (not under a species): a phase or a service.</summary>
    /// <remarks>
    /// <para>Do not derive from it directly: derive from <see cref="TickPhase"/> for a step of every tick, or from
    /// <see cref="WorldService"/> for something phases and modules use. Place the component on a GameObject under the
    /// World and outside every species (one under a species is ignored, V-15). A disabled component or an inactive
    /// GameObject is absent (ARCH-06).</para>
    /// <para>The World calls the hooks in this order, and in hierarchy order among modules:
    /// <list type="bullet">
    /// <item><description><see cref="Initialize"/>: services first (the ground before the others), before the species'
    /// modules exist; phases later, after every species module is initialised (20 §4).</description></item>
    /// <item><description><see cref="Validate"/>: after every Initialize, in the editor and at the start of a run; again
    /// when a species is added during the run.</description></item>
    /// <item><description><see cref="Begin"/>: once, when the run starts, before the founders.</description></item>
    /// <item><description><see cref="OnBorn"/>, <see cref="OnDied"/>, <see cref="OnSpeciesAdded"/>: during the run,
    /// founders included.</description></item>
    /// <item><description><see cref="OnRunStopped"/>: once, at the end.</description></item>
    /// </list></para>
    /// <para>Never set things up in Awake, Start or OnEnable: Unity doesn't guarantee their order (ARCH-04). Keep no
    /// static state (ARCH-07): several Worlds may run side by side.</para>
    /// </remarks>
    /// <example>
    /// A service that counts births and reports them when the run ends:
    /// <code>
    /// public class BirthCounter : WorldService
    /// {
    ///     public int Born { get; private set; }
    ///     public override void Initialize() => Born = 0;
    ///     public override void OnBorn(Animal a) => Born++;
    ///     public override void OnRunStopped(string reason) => Debug.Log($"{Born} births ({reason})");
    /// }
    /// </code>
    /// </example>
    public abstract class WorldModule : MonoBehaviour
    {
        /// <summary>The World this module belongs to; null until the World found it (step 2 of 20 §4).</summary>
        public World World { get; private set; }

        /// <summary>Called by the World in a fixed order, before any animal exists (ARCH-04).</summary>
        /// <remarks>
        /// Look up what you need here, by role or name (<see cref="World.Service{T}()"/>, <see cref="World.Phase{T}"/>),
        /// and reset your own state: the same object starts every run of the World. A service is initialised before the
        /// species' modules; a phase after them.
        /// </remarks>
        public virtual void Initialize() { }

        /// <summary>Report configuration problems (editor, and at the start of a run).</summary>
        /// <param name="report">Add one message per problem, naming its validator id (V-…) and the object at fault.
        /// An error stops the run from starting.</param>
        /// <remarks>Only read: validation must not change the scene. Offer a change as a <see cref="ValidationFix"/>.</remarks>
        public virtual void Validate(ValidationReport report) { }

        /// <summary>
        /// Called once after every module is initialised and validated, before founders spawn, in hierarchy order:
        /// place initial content here (the first food items), when it depends on other services (cover).
        /// </summary>
        /// <remarks>Not called when the editor only validates. Draw random numbers from a named stream, e.g.
        /// <c>World.Random.Get("world/" + ServiceName)</c> for the map (RAND-04).</remarks>
        public virtual void Begin() { }

        /// <summary>Called when a species is added during the run (SPEC-30), after it is set up and its event recorded.</summary>
        /// <param name="species">The new species; its founders are not spawned yet.</param>
        public virtual void OnSpeciesAdded(Species species) { }

        /// <summary>
        /// An animal of any species joined the living (a founder, a baby, an immigrant), after its species' modules heard it.
        /// </summary>
        /// <param name="a">The new animal, its stats set and already among its species' animals.</param>
        public virtual void OnBorn(Animal a) { }

        /// <summary>An animal of any species died, after its species' modules heard it (ANIM-42).</summary>
        /// <param name="a">The animal, already marked gone; it leaves its species' list at the end of the phase.</param>
        /// <param name="cause">The cause of death, e.g. <c>"killed"</c>, <c>"starvation"</c>, <c>"migrated"</c>.</param>
        public virtual void OnDied(Animal a, string cause) { }

        /// <summary>Called once when the run stops, whatever the reason (RAND-20).</summary>
        /// <param name="reason">Why it stopped, as <see cref="World.StopReason"/> says: <c>"tick limit"</c>,
        /// <c>"stop file"</c>, <c>"world disabled"</c>…</param>
        /// <remarks>Write and close your files here (the run recorder does). An exception is logged and the other
        /// modules still hear the stop.</remarks>
        public virtual void OnRunStopped(string reason) { }

        /// <summary>Sets <see cref="World"/>; called by the World when it finds this module.</summary>
        internal void Bind(World world) => World = world;
    }
}
