using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>A cause of death checked in the death phase (ANIM-40): return the cause's name, or null to live on.</summary>
    /// <remarks>
    /// <para>Derive from it, implement <see cref="CauseOfDeath"/>, list every cause it can give in <see cref="Causes"/>,
    /// and place the component under a species. Each tick the death phase first removes killed animals and ages every
    /// living animal by one tick (ANIM-35); then it asks the species' death rules, in module order, through
    /// <see cref="CheckAll"/>. The first rule that names a cause for an animal decides: later rules are not asked about
    /// it. The phase records the death and removes the animal; the rule only names the cause.</para>
    /// <para>A cause is a short lower-case name, such as <c>starvation</c>: it appears in death events and as the
    /// statistics column <c>deaths_&lt;cause&gt;</c>. Draw random numbers only from the species' streams.</para>
    /// </remarks>
    /// <example>
    /// The sample <c>DiesOfThirst</c> (22 §1): thirst grows in the death phase, and kills at a threshold.
    /// <code>
    /// public class DiesOfThirst : DeathRule
    /// {
    ///     static readonly IReadOnlyList&lt;string&gt; causes = System.Array.AsReadOnly(new[] { "thirst" });
    ///     Thirst thirst;
    ///
    ///     public override IEnumerable&lt;string&gt; Causes =&gt; causes;
    ///     public override void Initialize() =&gt; thirst = Species.Module&lt;Thirst&gt;();
    ///
    ///     public override string CauseOfDeath(Animal a)
    ///     {
    ///         if (thirst == null) return null;
    ///         a[thirst.Value] += thirst.PerTick;                       // ages with the animal, in the death phase
    ///         return a[thirst.Value] &gt;= thirst.Deadly ? "thirst" : null;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class DeathRule : SpeciesModule
    {
        /// <summary>The causes this rule can give, for the statistics columns (deaths_&lt;cause&gt;).</summary>
        /// <remarks>Empty by default: list every name <see cref="CauseOfDeath"/> may return.</remarks>
        public virtual IEnumerable<string> Causes => System.Array.Empty<string>();

        /// <summary>The cause of death for this animal now, or null.</summary>
        /// <param name="a">A living animal of the species, already aged by this tick.</param>
        /// <returns>The cause's name, such as <c>"starvation"</c>, or null to live on.</returns>
        public abstract string CauseOfDeath(Animal a);

        /// <summary>The batch entry point (20 §10): writes each animal's cause, or null, into causes.</summary>
        /// <param name="animals">The species' living animals.</param>
        /// <param name="causes">
        /// One slot per animal, at the same index (the array may be longer). A slot that already holds a cause, from an
        /// earlier rule, is kept: the default asks <see cref="CauseOfDeath"/> only for the others.
        /// </param>
        /// <remarks>Override it only to check many animals at once; it must give the causes the default would give.</remarks>
        public virtual void CheckAll(IReadOnlyList<Animal> animals, string[] causes)
        {
            for (int i = 0; i < animals.Count; i++)
                if (causes[i] == null) causes[i] = CauseOfDeath(animals[i]);
        }
    }
}
