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
            pendings.Clear();
            foreach (var e in due)
                foreach (var m in e.Mutations)
                {
                    if (m.Job.IsDone) continue;
                    if (m.Job.Pending != null) pendings.Add(m.Job.Pending);               // an operator's own asynchronous work (MUT-32)
                    else waiting.Add(m.Job);                                              // the mutator service's
                }
            var service = World.Service<MutatorService>();
            if (waiting.Count > 0 && service != null) pendings.Add(service.PendingFor(waiting));
            return Pending.All(pendings);
        }

        readonly List<Pending> pendings = new List<Pending>();

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
                if (!m.Job.IsDone || !m.Job.Succeeded)                                     // MUT-04: the inherited allele stays
                {
                    if (!m.Job.IsDone) World.Mutations.Reject("not finished");
                    World.Mutations.Failures++;
                    continue;
                }
                var gene = s.Genes[m.Locus];
                if (gene.Check(m.Job.Result) != null)                                       // the gene's allowed values hold for every operator
                {
                    World.Mutations.Reject("not allowed");
                    World.Mutations.Failures++;
                    continue;
                }
                var allele = World.Alleles.Register(gene, m.Job.Result, "mutant", m.Parent.Id, m.Job.Operator, m.Job.Model, m.Job.Seed);
                if (allele == m.Parent)                                                  // e.g. a clamp at the range's edge: unchanged is a failure (MUT-04)
                {
                    World.Mutations.Reject("unchanged");
                    World.Mutations.Failures++;
                    continue;
                }
                egg.Alleles[m.Locus] = allele;
                World.Mutations.Successes++;
                var entry = new SortedDictionary<string, object>(System.StringComparer.Ordinal)
                {
                    { "locus", gene.LocusId }, { "parent", m.Parent.Id }, { "child", allele.Id },
                };
                if (m.Job.Operator != null && m.Job.Operator.StartsWith("llm#") && int.TryParse(m.Job.Operator.Substring(4), out int deckIndex))
                    entry["prompt"] = deckIndex;
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
            if (breed > me) report.Error("V-08", this, "Hatch must come after Breed.", PlaceAfter(World.Phase<BreedPhase>()));
        }
    }
}
