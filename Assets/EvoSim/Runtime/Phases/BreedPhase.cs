using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// After all animals acted (TICK-06): each ready animal that chose mate and hasn't bred this period breeds with the
    /// nearest ready partner within reach that hasn't bred either — one partner's choice is enough (REPRO-02, REPRO-03).
    /// A litter is conceived (REPRO-10, REPRO-11): one egg per baby with its own crossover and its mutations drawn now
    /// (GENE-32, MUT-30); the LLM mutations are sent as one batch at the end (MUT-31).
    /// </summary>
    /// <remarks>
    /// <para>Fifth phase of the reference tick, after Act and before Hatch. Species without a <see cref="MatingRule"/>, or a
    /// World without an <see cref="EggSystem"/>, don't breed. Seekers are taken in id order (REPRO-03); a partner that
    /// breeds is marked, so it doesn't breed again this decision period.</para>
    /// <para>Random streams (RAND-03): the litter size and each baby's heading come from the species' <c>litter</c> stream;
    /// crossover and mutation choices from its <c>mutation</c> stream, drawn now, at conception, so they don't depend on
    /// when the mutator answers. Under a cap in Block mode, a litter is cut to the room left (POP-01).</para>
    /// </remarks>
    public class BreedPhase : TickPhase
    {
        readonly List<Animal> seekers = new List<Animal>();     // this species' animals that want to breed now, in id order

        /// <summary>Conceives the litters of every species, then sends the mutator's requests as one batch.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            var eggs = World.Service<EggSystem>();
            foreach (var s in World.AllSpecies)
            {
                var rule = s.Module<MatingRule>();
                if (rule == null || eggs == null) continue;
                seekers.Clear();
                foreach (var a in s.Animals)                                            // by id (REPRO-03)
                    if (!a.IsGone && !a.Killed && !a.BredThisPeriod && a.CurrentAction != null && a.CurrentAction.Mates && rule.IsReady(a))
                        seekers.Add(a);
                foreach (var seeker in seekers)
                {
                    if (seeker.BredThisPeriod) continue;                                 // already chosen as a partner
                    if (rule.Sexual)
                    {
                        var partner = NearestPartner(seeker, rule);
                        if (partner != null) Conceive(t, s, eggs, seeker, partner);
                    }
                    else Conceive(t, s, eggs, seeker, null);                              // REPRO-04
                }
            }
            World.Service<MutatorService>()?.Flush();
        }

        /// <summary>The nearest ready animal of the species within reach that hasn't bred (ties by id), or null (REPRO-02, REPRO-03).</summary>
        Animal NearestPartner(Animal seeker, MatingRule rule)
        {
            Animal best = null;
            float bestD = float.PositiveInfinity;
            foreach (var p in seeker.Species.Animals)
            {
                if (p == seeker || p.IsGone || p.Killed || p.BredThisPeriod || !rule.IsReady(p)) continue;
                float d = World.Distance(seeker.Position, p.Position);
                if (d > rule.Reach + Units.Epsilon) continue;
                if (d < bestD - 1e-6f || (System.Math.Abs(d - bestD) <= 1e-6f && p.Id < best.Id)) { best = p; bestD = d; }
            }
            return best;
        }

        /// <summary>
        /// Conceives a litter (REPRO-10, REPRO-11): draws its size, makes the parents pay the child energy, and lays one egg per
        /// baby with its own crossover and drawn mutations. <paramref name="second"/> is null for asexual breeding.
        /// </summary>
        void Conceive(TickContext t, Species s, EggSystem eggs, Animal first, Animal second)
        {
            var litter = s.Module<Litter>();
            var energy = s.Module<Energy>();
            var cap = s.Module<CapRule>();
            var parents = second != null ? new[] { first, second } : new[] { first };
            var energies = new float[parents.Length];
            for (int i = 0; i < parents.Length; i++) energies[i] = energy != null ? parents[i][energy.Value] : 0f;

            var litterRng = t.Stream(s, "litter");
            int k = litter != null ? litter.Size(litterRng, energies) : 1;
            if (cap != null && cap.Rule == CapRule.Mode.Block)
            {
                int left = cap.Cap - Living(s);                                          // POP-01 block
                if (left <= 0) return;
                k = System.Math.Min(k, left);
            }
            float childEnergy = litter != null ? litter.ChildEnergy : 0f;
            float share = childEnergy / parents.Length;
            int generation = 0;
            foreach (var p in parents)
            {
                energy?.Pay(p, share * k);                                               // REPRO-11
                p.Offspring += k;
                p.BredThisPeriod = true;
                generation = System.Math.Max(generation, p.Generation + 1);
            }
            var ids = new int[parents.Length];
            for (int i = 0; i < parents.Length; i++) ids[i] = parents[i].Id;

            var mutationRng = t.Stream(s, "mutation");
            var crossover = s.Module<Crossover>();
            int incubation = s.Module<Incubation>()?.Ticks ?? 0;
            for (int b = 0; b < k; b++)
            {
                var alleles = second != null
                    ? (crossover != null ? crossover.Cross(first.Genome, second.Genome, mutationRng) : first.Genome.ToArray())
                    : (crossover != null ? crossover.Copy(first.Genome) : first.Genome.ToArray());
                var egg = new Egg
                {
                    Species = s, Parents = ids, Generation = generation, Alleles = alleles,
                    ConceivedTick = t.Tick, HatchTick = t.Tick + incubation, LitterSize = k,
                    Heading = litterRng.Range(0f, 360f), Position = first.Position,
                };
                DrawMutations(World, s, egg, mutationRng);
                eggs.Lay(egg);
                s.Counters.EggsLaid++;
            }
        }

        /// <summary>Which loci of an egg mutate and how, drawn now in locus order (MUT-01, MUT-02, MUT-30).</summary>
        /// <remarks>
        /// For each gene: one draw against its operator's rate (0 under the no-mutation control), then the operator starts its
        /// job, which may finish later (an LLM mutation). A gene with no operator, or whose allele may not mutate (MUT-22),
        /// is skipped. Tests call it on hand-made eggs.
        /// </remarks>
        /// <param name="world">The World, for the controls and the mutation counters.</param>
        /// <param name="s">The egg's species.</param>
        /// <param name="egg">The egg, its inherited alleles set; its mutation list receives the jobs.</param>
        /// <param name="rng">The species' <c>mutation</c> stream (RAND-03).</param>
        public static void DrawMutations(World world, Species s, Egg egg, RandomStream rng)
        {
            for (int i = 0; i < s.Genes.Count; i++)
            {
                var gene = s.Genes[i];
                var op = OperatorFor(s, gene);
                if (op == null) continue;
                float rate = world.NoMutation ? 0f : op.Rate;                            // C2 NO-MUT
                if (!rng.Chance(rate)) continue;
                if (!gene.MayMutate(egg.Alleles[i])) continue;                           // MUT-22
                world.Mutations.Attempts++;
                egg.Mutations.Add(new EggMutation { Locus = i, Parent = egg.Alleles[i], Operator = op, Job = op.StartMutation(gene, egg.Alleles[i], rng) });
            }
        }

        /// <summary>The operator configured for the gene, else the species' default for its kind (MUT-02).</summary>
        /// <remarks>The first operator, in module order, that accepts the gene and lists it in <see cref="MutationOperator.OnlyThese"/>
        /// wins; otherwise the first that accepts it and lists no gene at all.</remarks>
        /// <param name="s">The gene's species.</param>
        /// <param name="gene">The gene to mutate.</param>
        /// <returns>The operator to use, or null when none accepts the gene (V-48).</returns>
        public static MutationOperator OperatorFor(Species s, Gene gene)
        {
            MutationOperator fallback = null;
            foreach (var op in s.ModulesOf<MutationOperator>())
            {
                if (!op.Accepts(gene)) continue;
                if (op.OnlyThese.Count == 0) { fallback ??= op; continue; }
                foreach (var g in op.OnlyThese) if (g == gene) return op;
            }
            return fallback;
        }

        /// <summary>How many animals of the species are alive and not killed this tick.</summary>
        static int Living(Species s)
        {
            int n = 0;
            foreach (var a in s.Animals) if (!a.IsGone && !a.Killed) n++;
            return n;
        }

        /// <summary>A fix that adds a module on a new child of the species.</summary>
        internal static ValidationFix AddModule<T>(Species s, string childName) where T : SpeciesModule => new ValidationFix($"Add {typeof(T).Name}", () =>
        {
            var go = new UnityEngine.GameObject(childName);
            go.transform.SetParent(s.transform, false);
            go.AddComponent<T>();
        });

        /// <summary>
        /// For each breeding species, reports a missing egg system (V-20), a missing litter or crossover (V-26), and genes no
        /// mutation operator accepts (V-48).
        /// </summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            foreach (var s in World.AllSpecies)
            {
                if (s.Module<MatingRule>() == null) continue;
                if (World.Service<EggSystem>() == null) report.Error("V-20", this, $"'{s.DisplayName}' breeds but the world has no EggSystem.");
                if (s.Module<Litter>() == null)
                    report.Warning("V-26", s, $"'{s.DisplayName}' breeds without a Litter: one baby, no energy cost.", AddModule<Litter>(s, "Litter"));
                if (s.Module<Crossover>() == null && s.Module<MatingRule>().Sexual)
                    report.Warning("V-26", s, $"'{s.DisplayName}' has no crossover: babies copy the first parent.", AddModule<UniformCrossover>(s, "Crossover"));
                foreach (var g in s.Genes)
                    if (OperatorFor(s, g) == null)
                        report.Warning("V-48", g, $"No mutation operator accepts the gene '{g.Label}'.",
                                       g.Kind == AlleleKind.Number ? AddModule<GaussianMutation>(s, "Mutation (gauss)") : null);
            }
        }
    }
}
