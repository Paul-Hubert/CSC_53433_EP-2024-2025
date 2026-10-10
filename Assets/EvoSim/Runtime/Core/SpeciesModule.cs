using UnityEngine;

namespace EvoSim
{
    /// <summary>A component under a species: a gene, a sense, an action, a stat, a rule...</summary>
    public abstract class SpeciesModule : MonoBehaviour
    {
        public Species Species { get; private set; }
        public World World => Species != null ? Species.World : null;

        /// <summary>Declare the stats and traits this module needs (before any animal exists).</summary>
        public virtual void Declare(SpeciesBuilder b) { }

        /// <summary>Look up other modules, layers and species (after every module declared).</summary>
        public virtual void Initialize() { }

        public virtual void Validate(ValidationReport report) { }

        /// <summary>Lines this module adds to the prompt's rules (PROMPT-01, PROMPT-03).</summary>
        public virtual void WritePromptRules(PromptWriter w) { }

        /// <summary>
        /// An animal of this species joined the living (a founder, a baby, an immigrant), its stats set. Keep what you
        /// need in the animal's stats, not in the module: modules hold settings only.
        /// </summary>
        public virtual void OnBorn(Animal a) { }

        /// <summary>An animal of this species died and its death was recorded (ANIM-42); it leaves the list at the end of the phase.</summary>
        public virtual void OnDied(Animal a, string cause) { }

        internal void Bind(Species species) => Species = species;
    }
}
