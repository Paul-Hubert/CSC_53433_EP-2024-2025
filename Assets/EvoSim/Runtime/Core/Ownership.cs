using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>Who owns which module (ARCH-05, ARCH-06): the nearest owner above it; disabled means absent.</summary>
    public static class Ownership
    {
        /// <summary>Enabled components on active GameObjects whose nearest Species is this one, in hierarchy order.</summary>
        public static List<T> Owned<T>(Species owner) where T : Component
        {
            var result = new List<T>();
            foreach (var m in owner.GetComponentsInChildren<T>(false))
                if (IsEnabled(m) && NearestSpecies(m) == owner) result.Add(m);
            return result;
        }

        /// <summary>True for an enabled component on an active GameObject (ARCH-06).</summary>
        public static bool IsEnabled(Component c) =>
            c != null && c.gameObject.activeInHierarchy && (!(c is Behaviour b) || b.enabled);

        /// <summary>The Species on this GameObject or the nearest one above it (enabled or not), or null.</summary>
        public static Species NearestSpecies(Component c) => c.GetComponentInParent<Species>(true);

        /// <summary>The species strictly above this one, or null (a nested species is an error, SPEC-05).</summary>
        public static Species SpeciesAbove(Species s) =>
            s.transform.parent != null ? s.transform.parent.GetComponentInParent<Species>(true) : null;

        /// <summary>The nearest enabled action on the gene's GameObject or above it, within the same species (GENE-05).</summary>
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
