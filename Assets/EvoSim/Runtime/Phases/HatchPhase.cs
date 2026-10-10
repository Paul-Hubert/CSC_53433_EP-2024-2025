using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Eggs due this tick hatch, in conception order (REPRO-22), once their genome is complete: the tick waits for the
    /// mutator if its answers aren't in (MUT-32, REPRO-24). Each baby starts with the child energy, full stamina, age 0,
    /// at its first parent's position (REPRO-12), and a birth event records its litter and mutations (MUT-03).
    /// </summary>
    public class HatchPhase : TickPhase
    {
        readonly List<Egg> due = new List<Egg>();
        readonly List<MutationJob> waiting = new List<MutationJob>();

        public override Pending WaitsFor(TickContext t)
        {
            var eggs = World.Service<EggSystem>();
            if (eggs == null) return null;
            eggs.Due(t.Tick, due);
            waiting.Clear();
            foreach (var e in due)
                foreach (var m in e.Mutations)
                    if (!m.Job.IsDone) waiting.Add(m.Job);
            if (waiting.Count == 0) return null;
            var service = World.Service<MutatorService>();
            return service != null ? service.PendingFor(waiting) : null;
        }

        public override void Run(TickContext t)
        {
            World.Service<MutatorService>()?.Pump();                                    // later rounds go out even when no egg is due
            var eggs = World.Service<EggSystem>();
            if (eggs == null) return;
            eggs.Due(t.Tick, due);
            foreach (var egg in due) Hatch(t, eggs, egg);
        }

        void Hatch(TickContext t, EggSystem eggs, Egg egg)
        {
            var s = egg.Species;
            var mutations = new List<object>();
            foreach (var m in egg.Mutations)
            {
                if (!m.Job.IsDone || !m.Job.Succeeded) { World.Mutations.Failures += m.Job.IsDone ? 1 : 0; continue; }   // MUT-04
                var gene = s.Genes[m.Locus];
                var allele = World.Alleles.Register(gene, m.Job.Result, "mutant", m.Parent.Id, m.Job.Operator, m.Job.Model, m.Job.Seed);
                egg.Alleles[m.Locus] = allele;
                World.Mutations.Successes++;
                var entry = new SortedDictionary<string, object>(System.StringComparer.Ordinal)
                {
                    { "locus", gene.LocusId }, { "parent", m.Parent.Id }, { "child", allele.Id },
                };
                if (m.Job.Operator != null && m.Job.Operator.StartsWith("llm#")) entry["prompt"] = int.Parse(m.Job.Operator.Substring(4));
                else entry["operator"] = m.Job.Operator;
                if (allele.Kind == AlleleKind.Text) entry["text"] = allele.Text; else entry["value"] = allele.Number;
                mutations.Add(entry);
            }

            var genome = new Genome(s, egg.Alleles);
            var baby = s.CreateAnimal("birth", egg.Position, egg.Heading, genome, egg.Generation, egg.Parents);
            var energy = s.Module<Energy>();
            var litter = s.Module<Litter>();
            if (energy != null && litter != null) baby[energy.Value] = litter.ChildEnergy;
            s.Add(baby);
            s.Counters.Births++;
            s.Counters.Hatched++;
            if (baby.Generation > s.Counters.MaxGeneration) s.Counters.MaxGeneration = baby.Generation;
            var e = new SimEvent("birth", t.Tick, baby.Id, s.Id)
                .With("parents", new List<int>(egg.Parents))
                .With("gen", baby.Generation)
                .With("genome", World.GenomeIds(genome))
                .With("litter", egg.LitterSize)
                .With("mutations", mutations);
            if (egg.HatchTick > egg.ConceivedTick) e.With("laid", egg.ConceivedTick);
            World.Events.Record(e);
            eggs.Hatched(egg);
        }

        public override void Validate(ValidationReport report)
        {
            var phases = World.Phases;
            int me = -1, breed = -1;
            for (int i = 0; i < phases.Count; i++)
            {
                if (phases[i] == this) me = i;
                if (phases[i] is BreedPhase) breed = i;
            }
            if (breed > me) report.Error("V-08", this, "Hatch must come after Breed.");
        }
    }
}
