using UnityEngine;

namespace EvoSim
{
    // Species added during a run (SPEC-30…32).
    public partial class World
    {
        /// <summary>
        /// A new species during a run (SPEC-30): a clone of the template under this World, with a new id (never reused,
        /// SPEC-32), its own population, streams and allele namespace. By default it inherits the parent's relations
        /// both ways (SPEC-31): it eats what the parent eats, and whoever hunted the parent hunts it.
        /// </summary>
        public Species AddSpecies(Species template, string name, Species parent = null, int founders = 0)
        {
            if (!IsInitialized) throw new System.InvalidOperationException("Initialize the World before adding species.");
            var go = Instantiate(template.gameObject, transform);
            go.name = name;
            var s = go.GetComponent<Species>();
            string id = name;
            for (int n = 2; usedSpeciesIds.Contains(id); n++) id = name + "-" + n;
            usedSpeciesIds.Add(id);
            s.SetNames(id, name);
            s.Parent = parent != null ? parent : template;
            var report = new ValidationReport();
            s.Discover(this, id, report);
            s.DeclareAll();
            s.InitializeModules();
            species.Add(s);
            foreach (var g in s.Genes)
                foreach (var f in g.FounderPool) Alleles.Register(g, f.Value, f.Origin);
            RebuildFoodWeb();
            s.Prompt = PromptWriter.Build(s);
            Events.Record(new SimEvent("species_created", Tick, -1, id).With("name", name).With("parent_species", s.Parent.Id));
            OnSpeciesAdded(s);
            var rng = Random.For(s, "founders");
            for (int i = 0; i < founders; i++)
            {
                var genome = FounderGenome(s, rng);
                var a = s.CreateAnimal("founder", RandomPosition(rng), rng.Range(0f, 360f), genome, 0, null);
                s.Add(a);
                s.Counters.Founders++;
                Events.Record(new SimEvent("founder", Tick, a.Id, s.Id).With("genome", GenomeIds(genome)));
            }
            return s;
        }

        partial void OnSpeciesAdded(Species s);
    }
}
