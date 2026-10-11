using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    // The World's animals, genomes and positions: founders, newcomers, the ground and the distance function.
    public partial class World
    {
        [Header("Controls (15 §1)")]
        [SerializeField, Tooltip("C3 SHUFFLED: each decision reads the genes of another living animal of the species (CTRL-01).")]
        bool shuffledGenes;
        [SerializeField, Tooltip("C4 RANDOM-FOUNDERS: founders get control sentences and random numbers in range (CTRL-01).")]
        bool randomFounders;
        [SerializeField, Tooltip("C2 NO-MUT: mutation rate 0 for every species (CTRL-01).")]
        bool noMutation;
        [SerializeField, Tooltip("C7 ASEXUAL: one parent per birth, copy and mutation (CTRL-01, REPRO-04).")]
        bool asexual;
        [SerializeField, Tooltip("Control sentences for C4 and the brain tests (GENE-23).")]
        List<string> controlSentences = new List<string>();

        Ground ground;

        /// <summary>Every allele seen in this run (GENE-10).</summary>
        public AlleleRegistry Alleles { get; private set; }
        /// <summary>The ground service (SPACE-04), or null in a world without one.</summary>
        public Ground Ground => ground;
        /// <summary>
        /// Control C3 SHUFFLED (CTRL-01): true when each decision reads the genes of another living animal of the species,
        /// and number genes are expressed from another animal's genome at birth.
        /// </summary>
        public bool ShuffledGenes { get => shuffledGenes; set => shuffledGenes = value; }
        /// <summary>Control C4 RANDOM-FOUNDERS (CTRL-01): true when founders get control sentences and random numbers in range.</summary>
        public bool RandomFounders { get => randomFounders; set => randomFounders = value; }
        /// <summary>Control C2 NO-MUT (CTRL-01): true when the mutation rate is 0 for every species.</summary>
        public bool NoMutation { get => noMutation; set => noMutation = value; }
        /// <summary>Control C7 ASEXUAL (CTRL-01, REPRO-04): true when each birth has one parent, copied and mutated.</summary>
        public bool Asexual { get => asexual; set => asexual = value; }
        /// <summary>The control sentences for C4 RANDOM-FOUNDERS and the brain tests (GENE-23).</summary>
        public IReadOnlyList<string> ControlSentences => controlSentences;
        /// <summary>Replaces the control sentences (GENE-23).</summary>
        /// <param name="sentences">The new sentences; the list is copied.</param>
        public void SetControlSentences(IEnumerable<string> sentences) => controlSentences = new List<string>(sentences);

        /// <summary>Clears the run state of the World's parts at the start of Prepare: alleles, ground, food web, space,
        /// decisions.</summary>
        partial void ResetSystems()
        {
            Alleles = new AlleleRegistry();
            ground = null;
            ClearRelations();                                    // labels derived before step 7 never see a stale food web
            ResetSpace();
            ResetDecisions();
        }

        /// <summary>Clears the spatial index and entity ids (World.Space.cs).</summary>
        partial void ResetSpace();
        /// <summary>Clears the decisions, the memo and the mutation statistics (World.Space.cs).</summary>
        partial void ResetDecisions();

        /// <summary>The world distance between two points (SPACE-02): the ground's function, horizontal Euclidean by default.</summary>
        /// <param name="a">One point.</param>
        /// <param name="b">The other point.</param>
        /// <returns>The distance in meters, measured on the horizontal plane by default.</returns>
        /// <remarks>Every module measures through it, so a ground that measures path length changes every distance at once.</remarks>
        public float Distance(Vector3 a, Vector3 b)
        {
            if (ground != null) return ground.Distance(a, b);
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Founder pools are registered first and in pool order, so founders get the same ids in every run (GENE-21).</summary>
        partial void RegisterFounderPools()
        {
            foreach (var s in species)
                foreach (var g in s.Genes)
                    foreach (var f in g.FounderPool)
                        Alleles.Register(g, f.Value, f.Origin);
        }

        /// <summary>A founder genome: each locus drawn uniformly from its pool (GENE-20), or the C4 control's alleles.</summary>
        /// <param name="s">The species whose genes make the genome.</param>
        /// <param name="rng">The stream to draw from: the species' <c>founders</c> stream (RAND-03).</param>
        /// <returns>A new genome with one registered allele per gene, in locus order.</returns>
        /// <exception cref="System.InvalidOperationException">A gene has an empty founder pool (V-41), or C4 is on and the
        /// World has no control sentence.</exception>
        public Genome FounderGenome(Species s, RandomStream rng)
        {
            var genes = s.Genes;
            var alleles = new Allele[genes.Count];
            for (int i = 0; i < genes.Count; i++)
            {
                var g = genes[i];
                if (randomFounders) { alleles[i] = RandomFounderAllele(g, rng); continue; }
                var pool = g.FounderPool;
                if (pool.Count == 0) throw new System.InvalidOperationException($"Gene {g.LocusId} has an empty founder pool (V-41).");
                var f = pool[rng.Range(0, pool.Count)];
                alleles[i] = Alleles.Register(g, f.Value, f.Origin);
            }
            return new Genome(s, alleles);
        }

        /// <summary>The C4 control's allele for one gene: a random number in the gene's range, or a random control sentence.</summary>
        Allele RandomFounderAllele(Gene g, RandomStream rng)
        {
            if (g is INumberRange n)                                     // any number gene kind with a range
                return Alleles.Register(g, AlleleValue.OfNumber(rng.Range(n.Min, n.Max)), "control");
            if (controlSentences.Count == 0)
                throw new System.InvalidOperationException("The random-founders control needs control sentences on the World.");
            return Alleles.Register(g, AlleleValue.OfText(controlSentences[rng.Range(0, controlSentences.Count)]), "control");
        }

        /// <summary>A random walkable position (POP-03, POP-04); the origin without a ground.</summary>
        /// <param name="rng">The stream to draw from, e.g. the species' <c>founders</c> stream.</param>
        /// <returns>A walkable point on the ground, or <see cref="Vector3.zero"/> when the World has no ground.</returns>
        public Vector3 RandomPosition(RandomStream rng) => ground != null ? ground.RandomWalkable(rng) : Vector3.zero;

        /// <summary>
        /// C3 SHUFFLED (CTRL-01): number genes are expressed from a random other living animal's genome at birth,
        /// drawn from the sampling stream (CTRL-02). Null when the control is off.
        /// </summary>
        internal Genome ExpressionGenomeFor(Animal newborn)
        {
            if (!shuffledGenes) return null;
            var living = newborn.Species.Animals;
            if (living.Count == 0) return null;
            return living[Random.For(newborn.Species, "sampling").Range(0, living.Count)].Genome;
        }

        /// <summary>Founders at tick 0: founder genomes at random walkable positions, recorded as founders (POP-04).</summary>
        partial void SpawnFoundersCore()
        {
            foreach (var s in species)
            {
                var rng = Random.For(s, "founders");
                for (int i = 0; i < s.InitialPopulation; i++)
                {
                    var genome = FounderGenome(s, rng);
                    var a = s.CreateAnimal("founder", RandomPosition(rng), rng.Range(0f, 360f), genome, 0, null);
                    s.Add(a);
                    s.Counters.Founders++;
                    Events.Record(new SimEvent("founder", Tick, a.Id, s.Id).With("genome", GenomeIds(genome)));
                }
            }
        }

        /// <summary>A newcomer below the floor: founder genome, random walkable position, generation 0 (POP-03).</summary>
        /// <param name="s">The species that gets the newcomer.</param>
        /// <returns>The new animal, already among the living and recorded as an <c>immigrant</c> event.</returns>
        /// <remarks>Called by the floor phase; a student's own phase may call it too. It draws from the species'
        /// <c>founders</c> stream.</remarks>
        public Animal AddNewcomer(Species s)
        {
            var rng = Random.For(s, "founders");
            var genome = FounderGenome(s, rng);
            var a = s.CreateAnimal("immigrant", RandomPosition(rng), rng.Range(0f, 360f), genome, 0, null);
            s.Add(a);
            s.Counters.Immigrants++;
            Events.Record(new SimEvent("immigrant", Tick, a.Id, s.Id).With("genome", GenomeIds(genome)));
            return a;
        }

        /// <summary>The allele ids of a genome, in locus order, for events (OUT-03).</summary>
        /// <param name="g">The genome, or null.</param>
        /// <returns>A new list of allele ids; empty for a null genome.</returns>
        public static List<string> GenomeIds(Genome g)
        {
            var ids = new List<string>(g?.Count ?? 0);
            if (g != null) for (int i = 0; i < g.Count; i++) ids.Add(g[i].Id);
            return ids;
        }

        /// <summary>The start of every tick: each animal's previous position is its current one, for the views.</summary>
        partial void BeforeTick()
        {
            foreach (var s in species)
            {
                var animals = s.Animals;
                for (int i = 0; i < animals.Count; i++) animals[i].PreviousPosition = animals[i].Position;   // for the views
            }
        }
    }
}
