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
        [SerializeField, Tooltip("Control sentences for C4 and the brain tests (GENE-23).")]
        List<string> controlSentences = new List<string>();

        Ground ground;

        /// <summary>Every allele seen in this run (GENE-10).</summary>
        public AlleleRegistry Alleles { get; private set; }
        /// <summary>The ground service (SPACE-04), or null in a world without one.</summary>
        public Ground Ground => ground;
        public bool ShuffledGenes { get => shuffledGenes; set => shuffledGenes = value; }
        public bool RandomFounders { get => randomFounders; set => randomFounders = value; }
        public IReadOnlyList<string> ControlSentences => controlSentences;
        public void SetControlSentences(IEnumerable<string> sentences) => controlSentences = new List<string>(sentences);

        partial void ResetSystems()
        {
            Alleles = new AlleleRegistry();
            ground = null;
            ResetSpace();
            ResetDecisions();
        }

        partial void ResetSpace();
        partial void ResetDecisions();

        /// <summary>The world distance between two points (SPACE-02): the ground's function, horizontal Euclidean by default.</summary>
        public float Distance(Vector3 a, Vector3 b)
        {
            if (ground != null) return ground.Distance(a, b);
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Founder pools are registered first and in pool order, so founders get the same ids in every run (GENE-21).</summary>
        partial void RegisterFounderPools()
        {
            ground = Service<Ground>();
            foreach (var s in species)
                foreach (var g in s.Genes)
                    foreach (var f in g.FounderPool)
                        Alleles.Register(g, f.Value, f.Origin);
        }

        /// <summary>A founder genome: each locus drawn uniformly from its pool (GENE-20), or the C4 control's alleles.</summary>
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

        Allele RandomFounderAllele(Gene g, RandomStream rng)
        {
            if (g is NumberGene n)
                return Alleles.Register(g, AlleleValue.OfNumber(rng.Range(n.Min, n.Max)), "control");
            if (controlSentences.Count == 0)
                throw new System.InvalidOperationException("The random-founders control needs control sentences on the World.");
            return Alleles.Register(g, AlleleValue.OfText(controlSentences[rng.Range(0, controlSentences.Count)]), "control");
        }

        /// <summary>A random walkable position (POP-03, POP-04); the origin without a ground.</summary>
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
        public static List<string> GenomeIds(Genome g)
        {
            var ids = new List<string>(g?.Count ?? 0);
            if (g != null) for (int i = 0; i < g.Count; i++) ids.Add(g[i].Id);
            return ids;
        }

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
