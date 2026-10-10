using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Profiling;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The root of a simulation (01 §3): finds its species, phases and services in its subtree, initialises them
    /// in a fixed order (ARCH-04, 20 §4) and runs ticks. FixedUpdate only drives the loop; time is Tick (SPACE-10).
    /// </summary>
    [DisallowMultipleComponent]
    public partial class World : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField, Tooltip("The run seed: every random stream derives from it (RAND-02).")]
        int seed = 1234;
        [SerializeField, Tooltip("Use a separate seed for the map: terrain, cover, initial food (RAND-04).")]
        bool separateWorldSeed;
        [SerializeField, Tooltip("The map's own seed, when separateWorldSeed is on (RAND-04).")]
        int worldSeed = 1234;
        [SerializeField, Tooltip("Freeze blocks the main thread while waiting; Responsive keeps the editor drawing (SPACE-14).")]
        WaitMode waitMode = WaitMode.Responsive;
        [SerializeField, Tooltip("Ticks per FixedUpdate, as many as fit in a frame budget, or a fixed number per second.")]
        RunSpeed runSpeed = RunSpeed.PerFixedUpdate;
        [SerializeField, Min(1), Tooltip("Ticks per FixedUpdate (PerFixedUpdate speed).")]
        int ticksPerFixedUpdate = 1;
        [SerializeField, Min(1), Tooltip("Milliseconds of ticks per frame (Fast speed).")]
        float fastBudgetMs = 10f;
        [SerializeField, Min(0.1f), Tooltip("Ticks per second (RealTime speed).")]
        float realTimeTicksPerSecond = 5f;
        [SerializeField, Tooltip("Initialize and run when Play mode starts.")]
        bool startOnPlay = true;

        [Header("Decisions")]
        [SerializeField, Min(1), Tooltip("Ticks between two decisions (DEC-01), in ticks.")]
        int decisionPeriod = 4;
        [SerializeField, Min(0.01f), Tooltip("τ: probabilities p^(1/τ), renormalised, before drawing (DEC-20).")]
        float samplingTemperature = 1f;
        [SerializeField, Min(0), Tooltip("Most keys the decision memo keeps (DEC-30); beyond, queries go to the brain (and its cache) again. 0 = no limit. Reference 262 144 (about 10 MB).")]
        int memoCapacity = 262144;
        [SerializeField, Tooltip("Situation text style for every species (SENSE-11).")]
        TextStyle textStyle = TextStyle.V1;
        [SerializeField, Tooltip("The brain of species that don't name one (DEC-13).")]
        Brain defaultBrain;

        [Header("Stop conditions (0 = off)")]
        [SerializeField, Min(0), Tooltip("Stop after this many ticks.")]
        int tickLimit;
        [SerializeField, Min(0), Tooltip("Stop after this many minutes of wall-clock time.")]
        float wallClockMinutes;
        [SerializeField, Tooltip("Stop at the next tick boundary when this file exists.")]
        string stopFile = "";
        [SerializeField, Tooltip("Stop when a species has no animal left.")]
        bool stopOnExtinction;

        readonly List<Species> species = new List<Species>();
        readonly List<TickPhase> phases = new List<TickPhase>();
        readonly List<WorldService> services = new List<WorldService>();
        readonly List<WorldModule> modules = new List<WorldModule>();
        readonly HashSet<string> usedSpeciesIds = new HashSet<string>();
        ProfilerMarker[] markers = Array.Empty<ProfilerMarker>();
        TickContext context;
        int nextPhase;
        int nextAnimalId;
        int runForRemaining = -1;
        RunState runMode = RunState.Paused;
        bool stopped, waiting, blocked, stopAtBoundary;
        string pendingStopReason;
        int waitCountedFor = -1;
        float realTimeCarry;
        readonly Stopwatch wallClock = new Stopwatch();

        /// <summary>Observation spaces above these are a warning and an error (SENSE-05, V-50).</summary>
        public const double ObservationSpaceWarning = 100000, ObservationSpaceError = 10000000;

        // ---- Public state ----

        public int Tick { get; private set; }
        public int Seed { get => seed; set => seed = value; }
        public int WorldSeed => separateWorldSeed ? worldSeed : seed;
        /// <summary>Gives the map its own seed (RAND-04).</summary>
        public void SetWorldSeed(int value) { separateWorldSeed = true; worldSeed = value; }
        public WaitMode WaitMode { get => waitMode; set => waitMode = value; }
        public RunSpeed RunSpeed { get => runSpeed; set => runSpeed = value; }
        public int TicksPerFixedUpdate { get => ticksPerFixedUpdate; set => ticksPerFixedUpdate = Mathf.Max(1, value); }
        public int DecisionPeriod { get => decisionPeriod; set => decisionPeriod = Mathf.Max(1, value); }
        public int MemoCapacity { get => memoCapacity; set => memoCapacity = Mathf.Max(0, value); }
        public float SamplingTemperature { get => samplingTemperature; set => samplingTemperature = Mathf.Max(0.01f, value); }
        public TextStyle TextStyle { get => textStyle; set => textStyle = value; }
        public Brain DefaultBrain { get => defaultBrain; set => defaultBrain = value; }
        public bool StartOnPlay { get => startOnPlay; set => startOnPlay = value; }
        public int TickLimit { get => tickLimit; set => tickLimit = Mathf.Max(0, value); }
        public string StopFile { get => stopFile; set => stopFile = value ?? ""; }
        public bool StopOnExtinction { get => stopOnExtinction; set => stopOnExtinction = value; }
        public float WallClockMinutes { get => wallClockMinutes; set => wallClockMinutes = Mathf.Max(0, value); }

        public RandomStreams Random { get; private set; }
        public EventLog Events { get; private set; }
        public bool IsInitialized { get; private set; }
        public ValidationReport LastReport { get; private set; }
        public string StopReason { get; private set; }
        /// <summary>Calls to Advance so far; fakes count delays in them (30 §2).</summary>
        public int AdvanceCalls { get; private set; }
        /// <summary>Times a tick had to wait for a phase (S14's wait counter).</summary>
        public int WaitCount { get; private set; }
        /// <summary>The phase the tick is waiting for, or null.</summary>
        public TickPhase WaitingFor => waiting || blocked ? phases[nextPhase] : null;
        /// <summary>The phase that runs next in the current tick (0 = between ticks).</summary>
        public int NextPhaseIndex => nextPhase;
        public TickContext Context => context;

        public RunState State =>
            !IsInitialized ? RunState.Idle : stopped ? RunState.Stopped : blocked ? RunState.Blocked :
            waiting ? RunState.Waiting : runMode;

        public IReadOnlyList<Species> AllSpecies => species;
        public IReadOnlyList<TickPhase> Phases => phases;
        public IReadOnlyList<WorldService> Services => services;

        /// <summary>The first enabled service of that type, in hierarchy order.</summary>
        public T Service<T>() where T : class
        {
            foreach (var s in services) if (s is T t) return t;
            return null;
        }

        /// <summary>The service of that type with this name (its GameObject's name), ARCH-08.</summary>
        public T Service<T>(string name) where T : class
        {
            foreach (var s in services) if (s is T t && s.ServiceName == name) return t;
            return null;
        }

        /// <summary>Every enabled service of that type, in hierarchy order.</summary>
        public IEnumerable<T> ServicesOf<T>() where T : class
        {
            foreach (var s in services) if (s is T t) yield return t;
        }

        /// <summary>The first phase of that type, or null.</summary>
        public T Phase<T>() where T : TickPhase
        {
            foreach (var p in phases) if (p is T t) return t;
            return null;
        }

        /// <summary>A species by id, or null.</summary>
        public Species FindSpecies(string id)
        {
            foreach (var s in species) if (s.Id == id) return s;
            return null;
        }

        /// <summary>A species by id or display name, or null.</summary>
        public Species FindSpeciesByName(string idOrName)
        {
            foreach (var s in species) if (s.Id == idOrName) return s;
            foreach (var s in species) if (s.DisplayName == idOrName) return s;
            return null;
        }

        /// <summary>The next animal id: unique in the run, increasing, never reused (ANIM-02).</summary>
        public int NextAnimalId() => nextAnimalId++;

        // ---- Unity entry points: they only drive (ARCH-04) ----

        void Awake()
        {
            if (startOnPlay && Application.isPlaying && !IsInitialized)
            {
                if (Initialize()) runMode = RunState.Running;
            }
        }

        void FixedUpdate()
        {
            if (!IsInitialized || stopped || runSpeed != RunSpeed.PerFixedUpdate) return;
            Drive(ticksPerFixedUpdate);
        }

        void Update()
        {
            if (!IsInitialized || stopped) return;
            if (runSpeed == RunSpeed.Fast)
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalMilliseconds < fastBudgetMs && !stopped && !waiting)
                    if (Drive(1) == 0) break;
            }
            else if (runSpeed == RunSpeed.RealTime && (runMode == RunState.Running || runMode == RunState.Stepping))
            {
                realTimeCarry += realTimeTicksPerSecond * Time.unscaledDeltaTime;
                int n = Mathf.FloorToInt(realTimeCarry);
                if (n > 0) { realTimeCarry -= Drive(n); realTimeCarry = Mathf.Max(0, realTimeCarry); }
            }
        }

        void LateUpdate()
        {
            if (IsInitialized) SyncViews();               // visuals only (SPACE-12)
        }

        /// <summary>Leaving Play mode, or disabling the World, ends the run like any stop: its files are complete (RAND-20, OUT-03).</summary>
        void OnDisable()
        {
            if (!IsInitialized || stopped) return;
            Stop(nextPhase == 0 && !waiting ? "world disabled" : $"world disabled during {phases[nextPhase].PhaseName} of tick {Tick}");
        }

        /// <summary>Advances according to the run controls; returns the ticks done.</summary>
        int Drive(int ticks)
        {
            if (runMode == RunState.Paused) return 0;
            if (runMode == RunState.Stepping) ticks = 1;
            if (runForRemaining >= 0) ticks = Math.Min(ticks, runForRemaining);
            if (ticks <= 0) { runMode = RunState.Paused; return 0; }
            int done = Advance(ticks);
            if (runForRemaining >= 0)
            {
                runForRemaining -= done;
                if (runForRemaining <= 0) { runForRemaining = -1; runMode = RunState.Paused; }
            }
            if (runMode == RunState.Stepping && done > 0) runMode = RunState.Paused;
            return done;
        }

        // ---- Run controls (02 §4) ----

        public void Play() { if (IsInitialized || Initialize()) { runForRemaining = -1; runMode = RunState.Running; } }
        public void Pause() => runMode = RunState.Paused;
        public void Resume() => runMode = RunState.Running;
        public void Step() { if (IsInitialized) runMode = RunState.Stepping; }
        public void RunFor(int ticks) { if (IsInitialized) { runForRemaining = Math.Max(0, ticks); runMode = RunState.Running; } }

        /// <summary>Stops at the next tick boundary (a button, a stop file).</summary>
        public void RequestStop(string reason)
        {
            if (stopped) return;
            if (nextPhase == 0 && !waiting) Stop(reason);
            else { stopAtBoundary = true; pendingStopReason = reason; }
        }

        /// <summary>
        /// Stops now. Called at a tick boundary, or inside a tick before anything changed the world
        /// (a strict brain failure in the Choose actions phase), so the outputs stay consistent (RAND-20).
        /// </summary>
        public void Stop(string reason)
        {
            if (stopped || !IsInitialized) return;
            stopped = true;
            waiting = false;
            StopReason = reason;
            wallClock.Stop();
            foreach (var m in modules)
            {
                try { m.OnRunStopped(reason); }
                catch (Exception e) { UnityEngine.Debug.LogException(e, m); }
            }
        }

        // ---- Initialisation (20 §4) ----

        /// <summary>Discovery, set-up, validation, founders. Returns false and keeps the report when an error is found.</summary>
        public bool Initialize()
        {
            var report = new ValidationReport();
            bool ok = Prepare(report);
            LastReport = report;
            if (!ok)
            {
                IsInitialized = false;
                foreach (var m in report.Messages)
                    if (m.Severity == Severity.Error) UnityEngine.Debug.LogError("EvoSim: " + m, m.Object);
                return false;
            }
            IsInitialized = true;
            stopped = false;
            StopReason = null;
            runMode = RunState.Paused;
            CreateViews();                                       // step 9
            foreach (var m in modules) m.Begin();               // initial content (food), after every module is ready
            SpawnFounders();                                     // step 10 (POP-04)
            wallClock.Restart();
            return true;
        }

        /// <summary>
        /// Steps 1–8 of 20 §4: find species and modules, initialise services, collect and bind species modules,
        /// declare and initialise, derive the food web, validate. No animal is created. The editor validates with it.
        /// </summary>
        public bool Prepare(ValidationReport report)
        {
            IsInitialized = false;
            Tick = 0;
            nextPhase = 0;
            nextAnimalId = 0;
            stopped = waiting = blocked = stopAtBoundary = false;
            AdvanceCalls = 0;
            WaitCount = 0;
            waitCountedFor = -1;
            Random = new RandomStreams(seed, WorldSeed);
            Events = new EventLog(seed, WorldSeed);
            context = new TickContext(this);
            ResetSystems();

            // 0. A World inside a World: both would own and tick the same species (ARCH-05, V-16).
            var outer = transform.parent != null ? transform.parent.GetComponentInParent<World>(true) : null;
            if (outer != null) report.Error("V-16", this, $"The World '{name}' is inside the World '{outer.name}': each World owns everything below it (ARCH-05).");
            foreach (var inner in GetComponentsInChildren<World>(true))
                if (inner != this) report.Error("V-16", inner, $"The World '{inner.name}' is inside the World '{name}': each World owns everything below it (ARCH-05).");
            if (report.HasErrors) return false;

            // 1. Species: a species inside a species is an error, enabled or not (SPEC-05, V-01).
            species.Clear();
            foreach (var s in GetComponentsInChildren<Species>(true))
            {
                var above = Ownership.SpeciesAbove(s);
                if (above != null)
                {
                    var nested = s;
                    report.Error("V-01", s, $"Species '{s.DisplayName}' is inside species '{above.DisplayName}' (SPEC-05).",
                                 new ValidationFix("Move it up", () => nested.transform.SetParent(transform, true)));
                    continue;
                }
                if (Ownership.IsEnabled(s)) species.Add(s);
            }

            // 2. World modules: under the World but not under a species, in hierarchy order (TICK-01).
            phases.Clear(); services.Clear(); modules.Clear();
            foreach (var m in GetComponentsInChildren<WorldModule>(false))
            {
                if (!Ownership.IsEnabled(m)) continue;
                if (Ownership.NearestSpecies(m) != null)
                {
                    report.Warning("V-15", m, $"{m.GetType().Name} is a world module under a species; it is ignored.");
                    continue;
                }
                m.Bind(this);
                modules.Add(m);
                if (m is TickPhase p) phases.Add(p);
                else if (m is WorldService svc) services.Add(svc);
            }
            foreach (var sm in GetComponentsInChildren<SpeciesModule>(false))
                if (Ownership.IsEnabled(sm) && Ownership.NearestSpecies(sm) == null)
                    report.Error("V-02", sm, $"{sm.GetType().Name} has no species above it.");
            if (report.HasErrors) return false;

            markers = new ProfilerMarker[phases.Count];
            for (int i = 0; i < phases.Count; i++) markers[i] = new ProfilerMarker("EvoSim." + phases[i].PhaseName);

            // 3. Services, in hierarchy order; the ground first, since layers are laid on it.
            ground = Service<Ground>();
            if (ground != null) ground.Initialize();
            foreach (var svc in services) if (svc != ground) svc.Initialize();
            Queries.Bind(ServicesOf<Cover>());                 // several covers combine: hidden by any (ENV-11)

            // 4–5. Species modules, ownership, gene binding, orders. Ids are unique (V-07).
            usedSpeciesIds.Clear();
            var names = new HashSet<string>();
            foreach (var s in species)
            {
                string sid = s.RequestedId;
                var sp = s;
                var rename = new ValidationFix("Rename", () => sp.SetNames("", sp.DisplayName + " 2"));
                if (!usedSpeciesIds.Add(sid))
                    report.Error("V-07", s, $"Two species have the id '{sid}'.", rename);
                if (!names.Add(s.DisplayName))
                    report.Error("V-07", s, $"Two species have the display name '{s.DisplayName}'.", rename);
                if (sid.IndexOf('.') >= 0)
                    report.Error("V-07", s, $"The species id '{sid}' contains '.', which separates species and gene in locus ids (GENE-01).");
                foreach (var other in species)
                    if (other != s && other.DisplayName == sid && other.RequestedId != sid)
                        report.Error("V-07", s, $"'{sid}' is the id of '{s.DisplayName}' and the display name of another species: diets and senses naming it are ambiguous.", rename);
                s.Discover(this, sid, report);
            }
            if (report.HasErrors) return false;

            // 6. Declare, then initialise every module; then the phases.
            foreach (var s in species) s.DeclareAll();
            foreach (var s in species) s.InitializeModules();
            foreach (var p in phases) p.Initialize();
            RegisterFounderPools();                              // frozen pools: same ids in every run (GENE-21)

            // 7. The food web and animal sets; then each species' prompt template (PROMPT-01).
            DeriveRelations();
            foreach (var s in species) { s.Sign(); s.Prompt = PromptWriter.Build(s); }

            // 8. Validation (EDIT-01).
            ValidateCore(report);
            foreach (var m in modules) m.Validate(report);
            foreach (var s in species)
                foreach (var sm in s.Modules) sm.Validate(report);
            foreach (var s in species) ValidateCompanions(s, report);
            ValidateSecrets(report);
            return !report.HasErrors;
        }

        /// <summary>The checks that belong to no single module (21 §2).</summary>
        void ValidateCore(ValidationReport report)
        {
            ValidateSettings(report);
            foreach (var s in species) ValidateSpecies(s, report);
            ValidatePhaseSet(report);
            ValidateServiceNames(report);
            ValidateAcrossSpecies(report);
        }

        /// <summary>V-35: run settings outside their range, e.g. set by a scenario override, which skips the inspector's limits.</summary>
        void ValidateSettings(ValidationReport report)
        {
            void Check(bool bad, string what, Action clamp)
            {
                if (bad) report.Error("V-35", this, $"{what} (an override can bypass the inspector's limit).", new ValidationFix("Clamp", clamp));
            }
            Check(decisionPeriod < 1, $"The decision period is {decisionPeriod}: it must be at least 1 tick (DEC-01)", () => DecisionPeriod = decisionPeriod);
            Check(!(samplingTemperature >= 0.01f), $"The sampling temperature is {samplingTemperature}: it must be at least 0.01 (DEC-20)", () => SamplingTemperature = float.IsNaN(samplingTemperature) ? 1f : samplingTemperature);
            Check(ticksPerFixedUpdate < 1, $"Ticks per FixedUpdate is {ticksPerFixedUpdate}: it must be at least 1", () => TicksPerFixedUpdate = ticksPerFixedUpdate);
            Check(memoCapacity < 0, $"The memo capacity is {memoCapacity}: it must be 0 (no limit) or more", () => MemoCapacity = memoCapacity);
            Check(tickLimit < 0, $"The tick limit is {tickLimit}: it must be 0 (off) or more", () => TickLimit = tickLimit);
            Check(!(wallClockMinutes >= 0f), $"The wall-clock limit is {wallClockMinutes}: it must be 0 (off) or more", () => WallClockMinutes = 0f);
            Check(!(fastBudgetMs > 0f), $"The Fast speed budget is {fastBudgetMs} ms: it must be more than 0", () => fastBudgetMs = 10f);
            Check(!(realTimeTicksPerSecond >= 0.1f), $"Real-time speed is {realTimeTicksPerSecond} ticks per second: it must be at least 0.1", () => realTimeTicksPerSecond = 0.1f);
        }

        /// <summary>The checks of one species (21 §2); also run on a species added during the run.</summary>
        void ValidateSpecies(Species s, ValidationReport report)
        {
            var sp = s;
            if (s.Actions.Count == 0)
                report.Error("V-04", s, $"Species '{s.DisplayName}' has no action (CORE-04).", new ValidationFix("Add Rest", () =>
                {
                    var go = new GameObject(nameof(RestAction).Replace("Action", "").ToLowerInvariant());
                    go.transform.SetParent(sp.transform, false);
                    go.AddComponent<RestAction>();
                }));
            var seen = new HashSet<string>();
            foreach (var a in s.Actions)
            {
                var action = a;
                if (!seen.Add(a.Name))
                    report.Error("V-03", a, $"Two actions of '{s.DisplayName}' are named '{a.Name}' (ACT-01).",
                                 new ValidationFix("Rename", () => action.gameObject.name = action.Name + " 2"));
                if (a.Gene == null)
                    report.Warning("V-05", a, $"Action '{a.Name}' has no text gene: the brain can't be steered for it.",
                                   new ValidationFix("Add a text gene", () => action.gameObject.AddComponent<TextGene>()));   // its pool: the neutral allele
            }
            var labels = new HashSet<string>();
            foreach (var g in s.Genes)
                if (!labels.Add(g.Label)) report.Error("V-14", g, $"Two genes of '{s.DisplayName}' have the label '{g.Label}': locus ids must be unique (GENE-01).");
            var senseLabels = new HashSet<string>();
            foreach (var sense in s.Senses)
                if (!senseLabels.Add(sense.Label))
                    report.Error("V-17", sense, $"Two senses of '{s.DisplayName}' have the label '{sense.Label}': the brain can't tell them apart (SENSE-10).");
            foreach (var problem in s.Declarations.Problems) report.Add(problem.Code, problem.Severity, problem.Module != null ? (UnityEngine.Object)problem.Module : s, problem.Text);
            if (!string.IsNullOrEmpty(s.AcceptedSignature) && s.AcceptedSignature != s.Signature)
                report.Warning("V-12", s, $"The signature of '{s.DisplayName}' changed: results and cached answers change (SPEC-03).",
                               new ValidationFix("Accept", () => sp.AcceptSignature()));
            double space = s.ObservationSpace;                       // SENSE-05
            if (space > ObservationSpaceError)
                report.Error("V-50", s, $"'{s.DisplayName}' has {space:N0} possible observations: above {ObservationSpaceError:N0}.");
            else if (space > ObservationSpaceWarning)
                report.Warning("V-50", s, $"'{s.DisplayName}' has {space:N0} possible observations: above {ObservationSpaceWarning:N0}, the memo and cache will rarely help.");
            foreach (var problem in s.Prompt.Problems)
                report.Error("V-53", s, $"Prompt of '{s.DisplayName}': {problem}.");
            if (s.Id == RandomStreams.WorldStream)
                report.Error("V-07", s, "The species id 'world' is reserved for the map's random streams.");
        }

        /// <summary>V-09: a core phase missing, so part of the life cycle never happens.</summary>
        void ValidatePhaseSet(ValidationReport report)
        {
            if (species.Count == 0) return;
            if (Phase<DeathPhase>() == null)
                report.Warning("V-09", this, "There is no Deaths phase: nobody dies.", AddPhaseFix<DeathPhase>("Deaths"));
            if (Phase<BreedPhase>() != null && Phase<HatchPhase>() == null)
                report.Warning("V-09", Phase<BreedPhase>(), "There is a Breed phase and no Hatch phase: eggs are laid and never hatch.", AddPhaseFix<HatchPhase>("Hatch"));
            if (Service<RunRecorder>() != null && Phase<RecordPhase>() == null)
                report.Warning("V-09", Service<RunRecorder>(), "There is a run recorder and no Record phase: no statistics rows, no periodic flush.", AddPhaseFix<RecordPhase>("Record"));
        }

        ValidationFix AddPhaseFix<T>(string phaseName) where T : TickPhase
        {
            var parent = phases.Count > 0 ? phases[phases.Count - 1].transform.parent : transform;
            var world = this;
            return new ValidationFix("Add it", () =>
            {
                var go = new GameObject(phaseName);
                go.transform.SetParent(parent != null ? parent : world.transform, false);
                go.AddComponent<T>();
            });
        }

        /// <summary>V-18: services found by name must have unique names, and layer names must not be reserved stream names (ARCH-08, RAND-02).</summary>
        void ValidateServiceNames(ValidationReport report)
        {
            for (int i = 0; i < services.Count; i++)
            {
                var a = services[i];
                if (a is ResourceLayer layer)
                {
                    string n = layer.ServiceName;
                    if (n == RandomStreams.WorldStream || n == RandomStreams.ActOrderStream || n.IndexOf('/') >= 0)
                        report.Error("V-18", layer, $"The layer name '{n}' is reserved for random streams (RAND-02): rename it.");
                }
                for (int j = 0; j < i; j++)
                {
                    var b = services[j];
                    bool byName = (a is ResourceLayer && b is ResourceLayer) || (a is Brain && b is Brain);      // the services found by name
                    if (byName && a.ServiceName == b.ServiceName)
                        report.Error("V-18", a, $"Two {(a is ResourceLayer ? "layers" : "brains")} are named '{a.ServiceName}': " +
                                                "look-ups by name reach only the first, and they would share random streams (ARCH-08, RAND-02).",
                                     new ValidationFix("Rename", () => a.gameObject.name = a.ServiceName + " 2"));
                }
            }
        }

        partial void ResetSystems();
        partial void RegisterFounderPools();
        partial void DeriveRelations();
        partial void CreateViews();
        partial void SyncViews();
        partial void SpawnFoundersCore();
        partial void BeforeTick();
        partial void AfterTick();

        void SpawnFounders() => SpawnFoundersCore();

        // ---- The tick loop (SPACE-13, SPACE-14, TICK-01…07) ----

        /// <summary>
        /// Runs up to maxTicks ticks and returns how many completed. In Responsive mode it returns early when a
        /// phase must wait, and the same tick continues at that phase on a later call; in Freeze mode it blocks.
        /// </summary>
        public int Advance(int maxTicks)
        {
            if (!IsInitialized) throw new InvalidOperationException("Initialize the World before advancing it.");
            if (stopped) return 0;
            AdvanceCalls++;
            int done = 0;
            while (done < maxTicks && !stopped)
            {
                if (nextPhase == 0 && !waiting)
                {
                    if (CheckStopAtBoundary()) break;
                    BeforeTick();
                }
                if (phases.Count == 0) { EndTick(); done++; continue; }

                var phase = phases[nextPhase];
                var pending = phase.WaitsFor(context);
                if (pending != null && !pending.IsDone)
                {
                    int key = Tick * 64 + nextPhase;
                    if (waitCountedFor != key) { WaitCount++; waitCountedFor = key; }
                    if (waitMode == WaitMode.Responsive) { waiting = true; return done; }   // Unity keeps rendering
                    blocked = true;
                    try { pending.Wait(); }                                              // Freeze: block until answered
                    finally { blocked = false; }
                }
                waiting = false;
                if (stopped) break;

                using (markers[nextPhase].Auto())
                {
                    try { phase.Run(context); }
                    catch (Exception e)
                    {
                        Stop("exception in " + phase.PhaseName + ": " + e.Message);
                        throw;
                    }
                }
                if (stopped) break;
                if (++nextPhase == phases.Count) { EndTick(); done++; }
            }
            return done;
        }

        void EndTick()
        {
            nextPhase = 0;
            AfterTick();
            Tick++;
            CheckStopAtBoundary();
        }

        /// <summary>Stop conditions, checked between ticks (RAND-20).</summary>
        bool CheckStopAtBoundary()
        {
            if (stopped) return true;
            string reason = null;
            if (stopAtBoundary) reason = pendingStopReason ?? "stop requested";
            else if (tickLimit > 0 && Tick >= tickLimit) reason = "tick limit";
            else if (wallClockMinutes > 0 && wallClock.Elapsed.TotalMinutes >= wallClockMinutes) reason = "wall-clock limit";
            else if (!string.IsNullOrEmpty(stopFile) && System.IO.File.Exists(stopFile)) reason = "stop file";
            else if (stopOnExtinction)
                foreach (var s in species)
                    if (s.Animals.Count == 0) { reason = "extinction of " + s.Id; break; }
            if (reason == null) return false;
            Stop(reason);
            return true;
        }
    }
}
