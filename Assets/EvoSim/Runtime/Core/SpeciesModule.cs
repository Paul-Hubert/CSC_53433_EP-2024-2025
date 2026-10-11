using UnityEngine;

namespace EvoSim
{
    /// <summary>A component under a species: a gene, a sense, an action, a stat, a rule...</summary>
    /// <remarks>
    /// <para>Derive from it (or from a module kind: <see cref="Sense"/>, <see cref="AnimalAction"/>, <see cref="Gene"/>,
    /// <see cref="DeathRule"/>…) and place the component anywhere under a <see cref="EvoSim.Species"/>: it belongs to the
    /// nearest species above it (ARCH-05). Folders such as <c>Senses</c> or <c>Life</c> are only for order: within a kind,
    /// depth-first order is the module's order. A disabled component or an inactive GameObject is absent (ARCH-06).</para>
    /// <para>The World calls, in this order: <see cref="Declare"/> on every module of the species, then
    /// <see cref="Initialize"/> on every module, then <see cref="WritePromptRules"/> while the prompt is built, then
    /// <see cref="Validate"/>; during the run, <see cref="OnBorn"/> and <see cref="OnDied"/>.</para>
    /// <para>A module holds settings only: one module serves every animal of its species, so keep each animal's state in
    /// the animal, as a stat or a trait it declares (ANIM-10). Name the modules it needs with
    /// <see cref="RequiresModuleAttribute"/>.</para>
    /// </remarks>
    /// <example>
    /// Recipe 22 §1, a stat module:
    /// <code>
    /// public class Thirst : SpeciesModule
    /// {
    ///     [SerializeField] float perTick = 0.5f, deadly = 100f;
    ///     public StatId Value { get; private set; }
    ///     public float PerTick => perTick;
    ///     public float Deadly => deadly;
    ///
    ///     public override void Declare(SpeciesBuilder b) =>
    ///         Value = b.DeclareStat("thirst", StatStart.Fixed(0f), 0f, deadly);
    ///
    ///     public override void WritePromptRules(PromptWriter w) =>
    ///         w.Rule($"Thirst grows every step; an animal dies of thirst at {deadly}. Drinking resets it.");
    /// }
    /// </code>
    /// </example>
    public abstract class SpeciesModule : MonoBehaviour
    {
        /// <summary>The species this module belongs to; null until the World found it (steps 4–5 of 20 §4).</summary>
        public Species Species { get; private set; }
        /// <summary>The World of its species, or null before the World found it.</summary>
        public World World => Species != null ? Species.World : null;

        /// <summary>Declare the stats and traits this module needs (before any animal exists).</summary>
        /// <param name="b">The species' declarations: call <see cref="SpeciesBuilder.DeclareStat"/> and
        /// <see cref="SpeciesBuilder.DeclareTrait"/>, and keep the ids they return.</param>
        /// <remarks>Called before any module's Initialize: don't look up other modules here. Two modules may declare the
        /// same name: they get the same id, and the first declaration counts (V-38 if they differ).</remarks>
        public virtual void Declare(SpeciesBuilder b) { }

        /// <summary>Look up other modules, layers and species (after every module declared).</summary>
        /// <remarks>
        /// Find things by role or name (ARCH-08): <c>Species.Module&lt;Energy&gt;()</c>, <c>World.Service&lt;Ground&gt;()</c>,
        /// <c>Species.Declarations.FindTrait("vision")</c>. Reset your own state here: the same object starts every run.
        /// </remarks>
        public virtual void Initialize() { }

        /// <summary>Reports this module's configuration problems: in the editor, and at the start of a run.</summary>
        /// <param name="report">Add one message per problem, naming its validator id (V-…) and the object at fault.
        /// An error stops the run from starting.</param>
        /// <remarks>Called after every Initialize and after the food web is derived; only read, never change the scene.</remarks>
        public virtual void Validate(ValidationReport report) { }

        /// <summary>Lines this module adds to the prompt's rules (PROMPT-01, PROMPT-03).</summary>
        /// <param name="w">The species' prompt writer: add a line with <c>w.Rule("…")</c>.</param>
        /// <remarks>Called while the species' prompt template is built, in module order. The lines go into the prompt's
        /// id, its cache keys and the species' results, so keep them stable.</remarks>
        public virtual void WritePromptRules(PromptWriter w) { }

        /// <summary>
        /// An animal of this species joined the living (a founder, a baby, an immigrant), its stats set. Keep what you
        /// need in the animal's stats, not in the module: modules hold settings only.
        /// </summary>
        /// <param name="a">The new animal, already among its species' animals.</param>
        /// <remarks>Called after the species' own <see cref="EvoSim.Species.OnBorn"/> and before the world modules hear it.</remarks>
        public virtual void OnBorn(Animal a) { }

        /// <summary>An animal of this species died and its death was recorded (ANIM-42); it leaves the list at the end of the
        /// phase.</summary>
        /// <param name="a">The animal, already marked gone.</param>
        /// <param name="cause">The cause of death, e.g. <c>"killed"</c>, <c>"starvation"</c>, <c>"old_age"</c>, <c>"migrated"</c>.</param>
        /// <remarks>Called after the species' own <see cref="EvoSim.Species.OnDied"/> and before the world modules hear it.</remarks>
        public virtual void OnDied(Animal a, string cause) { }

        /// <summary>Sets <see cref="Species"/>; called by the species when it collects its modules.</summary>
        internal void Bind(Species species) => Species = species;
    }
}
