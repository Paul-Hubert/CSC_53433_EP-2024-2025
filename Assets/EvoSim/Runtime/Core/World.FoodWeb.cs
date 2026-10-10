using System.Collections.Generic;

namespace EvoSim
{
    // The food web: threats and prey derived from the diets and edible components (SPEC-10…13), never configured twice.
    public partial class World
    {
        readonly Dictionary<Species, List<Species>> preyOf = new Dictionary<Species, List<Species>>();
        readonly Dictionary<Species, List<Species>> threatsOf = new Dictionary<Species, List<Species>>();
        readonly Dictionary<Species, List<Species>> kinOf = new Dictionary<Species, List<Species>>();
        readonly List<Species> noSpecies = new List<Species>();

        /// <summary>Step 7 of 20 §4: who strikes whom, from the diets; threats are the reverse (SPEC-12).</summary>
        partial void DeriveRelations()
        {
            preyOf.Clear(); threatsOf.Clear(); kinOf.Clear();
            foreach (var s in species)
            {
                preyOf[s] = new List<Species>();
                threatsOf[s] = new List<Species>();
                kinOf[s] = new List<Species> { s };
            }
            foreach (var hunter in species)                    // species order: the lists are ordered (RAND-05)
            {
                var diet = hunter.Module<Diet>();
                if (diet == null) continue;
                foreach (var target in species)
                    if (diet.Striking(target) != null)
                    {
                        preyOf[hunter].Add(target);
                        threatsOf[target].Add(hunter);
                    }
            }
        }

        /// <summary>The species whose diet strikes this one (SPEC-12); a cannibal is among its own threats (SPEC-11).</summary>
        public IReadOnlyList<Species> ThreatsOf(Species s) => threatsOf.TryGetValue(s, out var l) ? l : noSpecies;

        /// <summary>The species this one strikes.</summary>
        public IReadOnlyList<Species> PreyOf(Species s) => preyOf.TryGetValue(s, out var l) ? l : noSpecies;

        /// <summary>Whether <paramref name="hunter"/> is a threat of <paramref name="target"/>.</summary>
        public bool IsThreat(Species hunter, Species target) =>
            threatsOf.TryGetValue(target, out var l) && l.Contains(hunter);

        /// <summary>The species of an animal set for a species (SPEC-12); Listed uses the given list.</summary>
        public IReadOnlyList<Species> Resolve(Species s, AnimalSet set, IReadOnlyList<Species> listed = null)
        {
            switch (set)
            {
                case AnimalSet.Threats: return ThreatsOf(s);
                case AnimalSet.Prey: return PreyOf(s);
                case AnimalSet.Kin: return kinOf.TryGetValue(s, out var k) ? k : noSpecies;
                default: return listed ?? noSpecies;
            }
        }

        /// <summary>Re-derives the food web after a species was added during a run (SPEC-30, SPEC-31).</summary>
        public void RebuildFoodWeb()
        {
            foreach (var s in species)
                foreach (var d in s.ModulesOf<Diet>()) d.Resolve();
            DeriveRelations();
        }
    }
}
