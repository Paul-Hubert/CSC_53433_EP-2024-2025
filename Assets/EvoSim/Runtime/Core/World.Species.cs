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
        /// <param name="template">The species to clone, with all its modules; usually a species of this World.</param>
        /// <param name="name">The wanted id and display name; <c>name-2</c>, <c>name-3</c>… when it is taken.</param>
        /// <param name="parent">The species it descends from (<see cref="Species.Parent"/>); null = the template.</param>
        /// <param name="founders">Founders to spawn at once, at random walkable positions.</param>
        /// <returns>The new species, set up, validated, and in <see cref="AllSpecies"/>.</returns>
        /// <remarks>
        /// The new species goes through the same set-up and checks as at Initialize. Then the World records a
        /// <c>species_created</c> event and calls every world module's <see cref="WorldModule.OnSpeciesAdded"/>
        /// before the founders spawn.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">The World is not initialised, or the new species fails
        /// validation (the clone is then destroyed).</exception>
        public Species AddSpecies(Species template, string name, Species parent = null, int founders = 0)
        {
            if (!IsInitialized) throw new System.InvalidOperationException("Initialize the World before adding species.");
            string id = UnusedName(name);                       // a new id and a new name (SPEC-30, V-07), never reused (SPEC-32)
            usedSpeciesIds.Add(id);
            var go = Instantiate(template.gameObject, transform);
            go.name = id;
            var s = go.GetComponent<Species>();
            s.SetNames(id, id);
            s.Parent = parent != null ? parent : template;
            var report = new ValidationReport();
            s.Discover(this, id, report);
            s.DeclareAll();
            s.InitializeModules();
            species.Add(s);
            RebuildFoodWeb();
            s.Sign();
            s.Prompt = PromptWriter.Build(s);

            ValidateSpecies(s, report);                          // the checks of Prepare (EDIT-01), before anything is recorded
            foreach (var sm in s.Modules) sm.Validate(report);
            foreach (var m in modules) m.Validate(report);       // phases and services see the new species (V-10, V-11…)
            ValidateCompanions(s, report);
            foreach (var msg in report.Messages)
                if (msg.Severity == Severity.Warning) Debug.LogWarning("EvoSim: " + msg, msg.Object);
            if (report.HasErrors)
            {
                species.Remove(s);
                RebuildFoodWeb();
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
                var errors = new System.Text.StringBuilder();
                foreach (var msg in report.Messages) if (msg.Severity == Severity.Error) errors.Append("\n").Append(msg);
                throw new System.InvalidOperationException($"The species '{id}' can't be added:{errors}");
            }

            foreach (var g in s.Genes)
                foreach (var f in g.FounderPool) Alleles.Register(g, f.Value, f.Origin);
            Events.Record(new SimEvent("species_created", Tick, -1, id).With("name", id).With("parent_species", s.Parent.Id));
            foreach (var m in modules) m.OnSpeciesAdded(s);    // e.g. a service that keeps something per species
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

        /// <summary>
        /// The name itself if no species uses it as an id or display name (and no species ever had that id), else
        /// name-2, name-3…
        /// </summary>
        string UnusedName(string name)
        {
            bool Taken(string n)
            {
                if (usedSpeciesIds.Contains(n)) return true;
                foreach (var other in species) if (other.Id == n || other.DisplayName == n) return true;
                return false;
            }
            string id = name;
            for (int n = 2; Taken(id); n++) id = name + "-" + n;
            return id;
        }
    }
}
