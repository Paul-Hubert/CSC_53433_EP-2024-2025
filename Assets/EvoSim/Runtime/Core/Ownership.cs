using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Who owns which module (ARCH-05, ARCH-06): the nearest owner above it; disabled means absent.</summary>
    /// <remarks>The World and the species use these rules when they collect their modules; editor tools use them to show
    /// the same thing.</remarks>
    public static class Ownership
    {
        /// <summary>Enabled components on active GameObjects whose nearest Species is this one, in hierarchy order.</summary>
        /// <typeparam name="T">The kind of component to collect, e.g. <see cref="SpeciesModule"/>.</typeparam>
        /// <param name="owner">The species.</param>
        /// <returns>A new list in depth-first hierarchy order; components under a nested species are left out.</returns>
        public static List<T> Owned<T>(Species owner) where T : Component
        {
            var result = new List<T>();
            foreach (var m in owner.GetComponentsInChildren<T>(false))
                if (IsEnabled(m) && NearestSpecies(m) == owner) result.Add(m);
            return result;
        }

        /// <summary>True for an enabled component on an active GameObject (ARCH-06).</summary>
        /// <param name="c">The component, or null.</param>
        /// <returns>True when it is present for the World: not null, its GameObject active in the hierarchy, and enabled
        /// (components without an enabled switch count as enabled).</returns>
        public static bool IsEnabled(Component c) =>
            c != null && c.gameObject.activeInHierarchy && (!(c is Behaviour b) || b.enabled);

        /// <summary>The Species on this GameObject or the nearest one above it (enabled or not), or null.</summary>
        /// <param name="c">The component.</param>
        /// <returns>The species that owns it, or null when it sits under no species.</returns>
        public static Species NearestSpecies(Component c) => c.GetComponentInParent<Species>(true);

        /// <summary>The species strictly above this one, or null (a nested species is an error, SPEC-05).</summary>
        /// <param name="s">The species.</param>
        /// <returns>The nearest species above its GameObject, or null.</returns>
        public static Species SpeciesAbove(Species s) =>
            s.transform.parent != null ? s.transform.parent.GetComponentInParent<Species>(true) : null;

        /// <summary>
        /// True when the nearest action on or above the gene is disabled: switching an action off takes its gene with it
        /// (ARCH-06, GENE-05), out of the genome, the prompt and the signature.
        /// </summary>
        /// <param name="gene">The gene.</param>
        /// <param name="owner">The gene's species; actions of other species don't count.</param>
        /// <returns>True when the nearest action of the species on or above the gene is disabled.</returns>
        public static bool OnDisabledAction(Gene gene, Species owner)
        {
            for (var t = gene.transform; t != null; t = t.parent)
            {
                bool disabled = false;
                foreach (var a in t.GetComponents<AnimalAction>())
                {
                    if (NearestSpecies(a) != owner) continue;
                    if (IsEnabled(a)) return false;
                    disabled = true;
                }
                if (disabled) return true;
                if (t.GetComponent<Species>() != null) break;
            }
            return false;
        }

        /// <summary>The nearest enabled action on the gene's GameObject or above it, within the same species (GENE-05).</summary>
        /// <param name="gene">The gene.</param>
        /// <param name="owner">The gene's species; the search stops at its GameObject.</param>
        /// <returns>The action the gene steers, or null when there is none (the gene is then free).</returns>
        public static AnimalAction NearestAction(Gene gene, Species owner)
        {
            for (var t = gene.transform; t != null; t = t.parent)
            {
                foreach (var a in t.GetComponents<AnimalAction>())
                    if (IsEnabled(a) && NearestSpecies(a) == owner) return a;
                if (t.GetComponent<Species>() != null) break;
            }
            return null;
        }
    }
}
