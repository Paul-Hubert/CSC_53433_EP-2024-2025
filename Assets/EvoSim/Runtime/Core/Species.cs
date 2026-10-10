using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A kind of animal (SPEC-01, SPEC-02). Configure it with child components; subclass it only for hooks.</summary>
    [DisallowMultipleComponent]
    public class Species : MonoBehaviour
    {
        [SerializeField, Tooltip("Stable id used in files and keys (SPEC-01). Empty = the display name.")]
        string id = "";
        [SerializeField, Tooltip("What the brain reads in situation texts and prompts (SPEC-01, SPEC-20).")]
        string displayName = "species";
        [SerializeField, Tooltip("The brain that decides for this species; empty = the World's default (DEC-13).")]
        Brain brain;
        [SerializeField, Min(0), Tooltip("Founders at tick 0 (POP-04), in animals.")]
        int initialPopulation = 24;
        [SerializeField, TextArea(2, 6), Tooltip("First part of the prompt (PROMPT-01).")]
        string promptHeader = "You decide what a wild animal does next in a simple world.";
        [SerializeField, TextArea(2, 6), Tooltip("The line before the genes block (PROMPT-01).")]
        string genesIntro = "This animal's instincts (its genes). They define its personality: follow them\n" +
                            "even when they seem unwise. Instincts that are meaningless have no effect.";
        [SerializeField, Tooltip("Optional frozen prompt with {genes}, {situation} and {ask} (PROMPT-04).")]
        TextAsset frozenPrompt;
        [SerializeField, Tooltip("The signature accepted last (SPEC-03, V-12). Empty = not accepted yet.")]
        string acceptedSignature = "";

        readonly List<Animal> animals = new List<Animal>();
        readonly List<SpeciesModule> modules = new List<SpeciesModule>();
        readonly Dictionary<System.Type, object> moduleOf = new Dictionary<System.Type, object>();   // Module<T> look-ups, per type
        readonly List<AnimalAction> actions = new List<AnimalAction>();
        readonly List<Gene> genes = new List<Gene>();
        readonly List<Sense> senses = new List<Sense>();

        public string Id { get; private set; }
        public string DisplayName => displayName;
        public World World { get; private set; }
        /// <summary>The species' own brain, or the World's default (DEC-13).</summary>
        public Brain Brain => brain != null ? brain : World != null ? World.DefaultBrain : null;
        public Brain OwnBrain => brain;
        public int InitialPopulation => initialPopulation;
        public string PromptHeader => promptHeader;
        public string GenesIntro => genesIntro;
        public TextAsset FrozenPrompt => frozenPrompt;
        public string AcceptedSignature => acceptedSignature;

        public IReadOnlyList<AnimalAction> Actions => actions;
        public IReadOnlyList<Gene> Genes => genes;
        public IReadOnlyList<Sense> Senses => senses;
        /// <summary>Every module of this species, in hierarchy order.</summary>
        public IReadOnlyList<SpeciesModule> Modules => modules;
        /// <summary>Living animals, in creation (id) order.</summary>
        public IReadOnlyList<Animal> Animals => animals;
        /// <summary>The prompt template built from the modules (PROMPT-01), with its id (PROMPT-05).</summary>
        public PromptTemplate Prompt { get; internal set; }
        /// <summary>Builds the prompt template again, e.g. after a phase changed a rule line during the run (PROMPT-01).</summary>
        public void RebuildPrompt() => Prompt = PromptWriter.Build(this);

        /// <summary>The order and set of actions, genes and senses (SPEC-03).</summary>
        public string Signature { get; private set; }
        /// <summary>The declared stats and traits (after Declare).</summary>
        public SpeciesBuilder Declarations { get; private set; }
        /// <summary>The species this one was cloned from at run time (SPEC-31), or null.</summary>
        public Species Parent { get; internal set; }
        /// <summary>True for this species and every species cloned from it during the run (SPEC-31).</summary>
        public bool DescendsFrom(Species ancestor)
        {
            for (var s = this; s != null; s = s.Parent)
                if (s == ancestor) return true;
            return false;
        }

        /// <summary>Counters for the statistics and the summary.</summary>
        public SpeciesCounters Counters { get; private set; }

        /// <summary>The one module of this type (a Diet, a Litter, a CapRule…), or null.</summary>
        public T Module<T>() where T : class
        {
            if (moduleOf.TryGetValue(typeof(T), out var known)) return (T)known;
            T found = null;
            foreach (var m in modules) if (m is T t) { found = t; break; }
            moduleOf[typeof(T)] = found;                                           // the act phase asks several times per animal
            return found;
        }

        /// <summary>Every module of this type, in hierarchy order.</summary>
        public IEnumerable<T> ModulesOf<T>() where T : class
        {
            foreach (var m in modules) if (m is T t) yield return t;
        }

        /// <summary>An action by name, or null.</summary>
        public AnimalAction FindAction(string actionName)
        {
            foreach (var a in actions) if (a.Name == actionName) return a;
            return null;
        }

        /// <summary>A sense by label, or null.</summary>
        public Sense FindSense(string senseLabel)
        {
            foreach (var s in senses) if (s.Label == senseLabel) return s;
            return null;
        }

        // Hooks for subclasses. Most species never need them.
        protected internal virtual void OnBorn(Animal a) { }
        protected internal virtual void OnDied(Animal a, string cause) { }

        /// <summary>Sets the display name and id from code (WorldBuilder, AddSpecies).</summary>
        public void SetNames(string speciesId, string name)
        {
            id = speciesId ?? "";
            displayName = name;
        }

        public void SetBrain(Brain b) => brain = b;
        public void SetInitialPopulation(int n) => initialPopulation = Mathf.Max(0, n);
        public void SetPromptHeader(string header) => promptHeader = header;
        public void SetGenesIntro(string intro) => genesIntro = intro;
        public void SetFrozenPrompt(TextAsset asset) => frozenPrompt = asset;
        /// <summary>Accepts the current signature (V-12's fix).</summary>
        public void AcceptSignature() => acceptedSignature = Signature ?? "";

        /// <summary>The id this species asks for: its id field, else its display name.</summary>
        public string RequestedId => string.IsNullOrEmpty(id) ? displayName : id;

        // ---- Set-up, called by the World in a fixed order (20 §4) ----

        /// <summary>Steps 4–5: collect owned modules, bind genes, build the orders.</summary>
        internal void Discover(World world, string assignedId, ValidationReport report)
        {
            World = world;
            Id = assignedId;
            animals.Clear();
            Counters = new SpeciesCounters();
            modules.Clear(); actions.Clear(); genes.Clear(); senses.Clear(); moduleOf.Clear();
            foreach (var m in Ownership.Owned<SpeciesModule>(this))
                if (!(m is Gene g0) || !Ownership.OnDisabledAction(g0, this)) modules.Add(m);
            foreach (var m in modules)
            {
                m.Bind(this);
                if (m is AnimalAction a) { a.Index = actions.Count; a.Gene = null; actions.Add(a); }
                else if (m is Gene g) { g.Locus = genes.Count; g.Action = null; genes.Add(g); }
                else if (m is Sense s) { s.Index = senses.Count; senses.Add(s); }
            }
            BindGenes(report);
        }

        void BindGenes(ValidationReport report)
        {
            foreach (var g in genes)
            {
                if (!(g is TextGene tg) || tg.IsFree) continue;
                var action = Ownership.NearestAction(tg, this);       // on its GameObject or above it (GENE-05)
                if (action == null)
                {
                    report?.Warning("V-06", tg, $"Text gene '{tg.Label}' is neither under an action nor marked free; it is treated as free.",
                                    new ValidationFix("Mark free", () => tg.Configure(tg.Founders, tg.HasNeutral, tg.Neutral, true, tg.ContrastPro, tg.ContrastAnti)));
                    continue;
                }
                if (action.Gene != null)
                {
                    report?.Warning("V-06", tg, $"Action '{action.Name}' already has the gene '{action.Gene.Label}'; '{tg.Label}' is treated as free.");
                    continue;
                }
                action.Gene = tg;
                tg.Action = action;
            }
        }

        /// <summary>Step 6a: every module declares its stats and traits.</summary>
        internal void DeclareAll()
        {
            Declarations = new SpeciesBuilder(this);
            foreach (var m in modules)
            {
                Declarations.Current = m;
                m.Declare(Declarations);
            }
            Declarations.Current = null;
        }

        /// <summary>Step 6b: every module looks up what it needs (ARCH-08).</summary>
        internal void InitializeModules()
        {
            foreach (var m in modules) m.Initialize();
            Counters.Resize(actions.Count);
        }

        /// <summary>Step 7, after the food web: sense labels derived from it are final (SPEC-03, SPEC-20).</summary>
        internal void Sign() => Signature = ComputeSignature();

        /// <summary>The signature: a hash of the action, gene and sense orders and tokens (SPEC-03).</summary>
        public string ComputeSignature()
        {
            var sb = new StringBuilder("actions:");
            foreach (var a in actions) sb.Append(a.Name).Append(',');
            sb.Append("|genes:");
            foreach (var g in genes) sb.Append(g.Label).Append('[').Append(g.Kind).Append("],");
            sb.Append("|senses:");
            foreach (var s in senses)
            {
                sb.Append(s.Label).Append('(');
                var tokens = s.Tokens;
                if (tokens != null) foreach (var t in tokens) sb.Append(t).Append(' ');
                sb.Append("),");
            }
            return Hashing.Sha256Hex(sb.ToString());
        }

        // ---- Animals ----

        /// <summary>
        /// Creates an animal: traits at their defaults, then the genome expressed into them (ANIM-15),
        /// then stats at their start values. The caller records the event. <paramref name="atCreation"/> may still set
        /// any trait (ANIM-17), before the stats start from them.
        /// </summary>
        public Animal CreateAnimal(string origin, Vector3 position, float heading, Genome genome, int generation, int[] parents,
                                   System.Action<Animal> atCreation = null)
        {
            if (genome != null && genome.Species != this)
                throw new System.ArgumentException($"A {genome.Species?.Id} genome can't make a {Id} animal (GENE-06).");
            var d = Declarations;
            var a = new Animal(World.NextAnimalId(), this, d.Stats.Count, d.Traits.Count)
            {
                Origin = origin,
                Position = position,
                PreviousPosition = position,
                Heading = heading,
                Genome = genome,
                Generation = generation,
                Parents = parents ?? System.Array.Empty<int>(),
                BornTick = World.Tick,
                Age = 0,
            };
            var traits = a.RawTraits;
            foreach (var t in d.Traits) traits[t.Id.Index] = t.Id.Default;
            if (genome != null)
            {
                var expressed = World.ExpressionGenomeFor(a) ?? genome;   // the shuffled control may express another genome (CTRL-01)
                var e = new Expression(a, d);
                for (int i = 0; i < genes.Count; i++) genes[i].Express(expressed[i].Value, e);
            }
            atCreation?.Invoke(a);
            var stats = a.RawStats;
            foreach (var s in d.Stats)
            {
                float v = s.Start.Trait >= 0 ? traits[s.Start.Trait] : s.Start.Value;
                stats[s.Id.Index] = s.Id.Clamp(v, traits);
            }
            a.Created = true;                                       // traits are fixed from now on, unless changeable (ANIM-17)
            return a;
        }

        /// <summary>Adds a created animal to the living ones and calls OnBorn.</summary>
        public void Add(Animal a)
        {
            animals.Add(a);
            World.OnAnimalAdded(a);
            OnBorn(a);
            World.Born(a);
        }

        /// <summary>Drops the animals marked gone, keeping creation order (a student's own death phase calls it too).</summary>
        public void RemoveGone()
        {
            int w = 0;
            for (int r = 0; r < animals.Count; r++)
                if (!animals[r].IsGone) animals[w++] = animals[r];
            if (w < animals.Count) animals.RemoveRange(w, animals.Count - w);
        }

        /// <summary>Reads every sense of this species for one animal, in sense order (SENSE-02, SENSE-03).</summary>
        public Observation Observe(Animal a, SenseContext s)
        {
            var tokens = new int[senses.Count];
            for (int i = 0; i < senses.Count; i++) tokens[i] = senses[i].Read(a, s);
            return new Observation(tokens);
        }

        /// <summary>The situation text: each sense's fragment, in sense order, joined by spaces (SENSE-10).</summary>
        public string Describe(Observation o, TextStyle style)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < senses.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(senses[i].Write(o[i], style));
            }
            return sb.ToString();
        }

        /// <summary>The number of possible observations: the product of the senses' token counts (SENSE-05).</summary>
        public double ObservationSpace
        {
            get
            {
                double n = 1;
                foreach (var s in senses) n *= System.Math.Max(1, s.Tokens?.Count ?? 1);
                return n;
            }
        }
    }
}
