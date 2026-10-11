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
    /// <remarks>
    /// <para>Place one World at the root of a simulation. Everything below it belongs to it (ARCH-05): the species, the
    /// tick phases and the services. A World inside another World is an error (V-16). Modules never set themselves up
    /// in Awake, Start or OnEnable: the World calls them, always in the same order (ARCH-04).</para>
    /// <para><see cref="Initialize"/> (called by <see cref="Play"/>, or in Awake when <see cref="StartOnPlay"/> is on)
    /// first runs <see cref="Prepare"/>: random streams and event log from the seed, species found, world modules found
    /// and bound, services initialised (the ground first), each species' modules collected, genes bound, every
    /// <see cref="SpeciesModule.Declare"/> then every <see cref="SpeciesModule.Initialize"/>, phases initialised, founder
    /// pools registered, the food web derived, signatures and prompts built, then validation. When no error was found it
    /// makes the views, calls every <see cref="WorldModule.Begin"/>, and spawns the founders (POP-04).</para>
    /// <para>A tick runs the phases in order (TICK-01). Before each phase the World asks its
    /// <see cref="TickPhase.WaitsFor"/>; when the answer is not ready, <see cref="EvoSim.WaitMode.Responsive"/> returns
    /// from <see cref="Advance"/> and resumes the same phase on a later call, and <see cref="EvoSim.WaitMode.Freeze"/>
    /// blocks on <see cref="Pending.Wait"/>. Both give the same events hash (RAND-11). Stop conditions are checked only
    /// between ticks, so a stopped run's files are complete (RAND-20).</para>
    /// <para>The class is split in several files: World.Animals.cs (founders, newcomers, distance), World.Deaths.cs,
    /// World.FoodWeb.cs, World.Species.cs (species added during a run), World.Space.cs, World.Views.cs and
    /// World.Checks.cs.</para>
    /// </remarks>
    /// <example>
    /// Running a World from code, as a test or a batch run does:
    /// <code>
    /// world.Seed = 7;
    /// world.WaitMode = WaitMode.Freeze;
    /// if (!world.Initialize()) { Debug.LogError(world.LastReport); return; }
    /// world.Advance(500);
    /// world.Stop("done");
    /// Debug.Log(world.Events.ShortHash);
    /// </code>
    /// A module finds what it needs from its World in Initialize (ARCH-08):
    /// <code>
    /// public override void Initialize() => food = World.Service&lt;FoodGrid&gt;("Grass");
    /// </code>
    /// </example>
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

        readonly List<Species> species = new List<Species>();                 // backs AllSpecies
        readonly List<TickPhase> phases = new List<TickPhase>();
        readonly List<WorldService> services = new List<WorldService>();
        readonly List<WorldModule> modules = new List<WorldModule>();         // every world module, phases and services, in hierarchy order
        readonly HashSet<string> usedSpeciesIds = new HashSet<string>();      // every species id given in this run, never reused (SPEC-32)
        ProfilerMarker[] markers = Array.Empty<ProfilerMarker>();              // one profiler marker per phase, same index
        TickContext context;
        int nextPhase;                                                         // the phase that runs next; 0 between ticks
        int nextAnimalId;                                                      // the next id NextAnimalId gives (ANIM-02)
        int runForRemaining = -1;                                              // ticks left of a RunFor; -1 = no limit
        RunState runMode = RunState.Paused;                                    // what the run controls asked: Running, Paused or Stepping
        // The run is over; a tick waits (Responsive); the main thread is blocked (Freeze); a stop was requested.
        bool stopped, waiting, blocked, stopAtBoundary;
        string pendingStopReason;                                              // the reason RequestStop gave, used at the next boundary
        int waitCountedFor = -1;                                               // the tick and phase whose wait WaitCount counted last
        float realTimeCarry;                                                   // ticks owed by the RealTime clock, fractions included
        readonly Stopwatch wallClock = new Stopwatch();                       // wall-clock time since Initialize, for the wall-clock limit

        /// <summary>
        /// Sizes of a species' observation space (its number of possible observations) above which validation warns
        /// and fails (SENSE-05, V-50).
        /// </summary>
        public const double ObservationSpaceWarning = 100000, ObservationSpaceError = 10000000;

        // ---- Public state ----

        /// <summary>
        /// The current tick: the number of ticks completed since the run started (0 during the first tick).
        /// Simulation time is counted in ticks, never in seconds (SPACE-10).
        /// </summary>
        public int Tick { get; private set; }
        /// <summary>The run seed: every random stream derives from it (RAND-02). A change takes effect at the next Initialize.</summary>
        public int Seed { get => seed; set => seed = value; }
        /// <summary>The seed of the map's streams (terrain, cover, initial food): its own seed when set, else the run seed
        /// (RAND-04).</summary>
        public int WorldSeed => separateWorldSeed ? worldSeed : seed;
        /// <summary>Gives the map its own seed (RAND-04), so many runs can share one map while everything else varies.</summary>
        /// <param name="value">The map's seed; it takes effect at the next Initialize.</param>
        public void SetWorldSeed(int value) { separateWorldSeed = true; worldSeed = value; }
        /// <summary>
        /// How a tick waits for brain answers and mutations: Freeze blocks the main thread, Responsive returns and resumes
        /// later (SPACE-14). Both give the same events hash (RAND-11).
        /// </summary>
        public WaitMode WaitMode { get => waitMode; set => waitMode = value; }
        /// <summary>How fast a running World advances. Results never depend on it (SPACE-11).</summary>
        public RunSpeed RunSpeed { get => runSpeed; set => runSpeed = value; }
        /// <summary>Ticks per FixedUpdate with the PerFixedUpdate speed; at least 1.</summary>
        public int TicksPerFixedUpdate { get => ticksPerFixedUpdate; set => ticksPerFixedUpdate = Mathf.Max(1, value); }
        /// <summary>Ticks per second with the RealTime speed; at least 0.1.</summary>
        public float RealTimeTicksPerSecond { get => realTimeTicksPerSecond; set => realTimeTicksPerSecond = Mathf.Max(0.1f, value); }
        /// <summary>Ticks between two decisions of one animal (DEC-01); at least 1. Reference 4.</summary>
        public int DecisionPeriod { get => decisionPeriod; set => decisionPeriod = Mathf.Max(1, value); }
        /// <summary>Most keys the decision memo keeps (DEC-30); 0 = no limit. A change takes effect at the next Initialize.</summary>
        public int MemoCapacity { get => memoCapacity; set => memoCapacity = Mathf.Max(0, value); }
        /// <summary>
        /// The sampling temperature τ: each probability p becomes p^(1/τ), renormalised, before an action is drawn (DEC-20).
        /// 1 keeps the brain's probabilities; at least 0.01.
        /// </summary>
        public float SamplingTemperature { get => samplingTemperature; set => samplingTemperature = Mathf.Max(0.01f, value); }
        /// <summary>The style of the situation texts of every species (SENSE-11).</summary>
        public TextStyle TextStyle { get => textStyle; set => textStyle = value; }
        /// <summary>The brain of the species that name none (DEC-13), or null.</summary>
        public Brain DefaultBrain { get => defaultBrain; set => defaultBrain = value; }
        /// <summary>True to initialise and run when Play mode starts. A launcher turns it off in <see cref="Configuring"/>.</summary>
        public bool StartOnPlay { get => startOnPlay; set => startOnPlay = value; }
        /// <summary>Stops the run after this many ticks; 0 = no limit.</summary>
        public int TickLimit { get => tickLimit; set => tickLimit = Mathf.Max(0, value); }
        /// <summary>A file path: the run stops at the next tick boundary once this file exists. Empty = off; never null.</summary>
        public string StopFile { get => stopFile; set => stopFile = value ?? ""; }
        /// <summary>True to stop the run at the first tick boundary where a species has no animal left.</summary>
        public bool StopOnExtinction { get => stopOnExtinction; set => stopOnExtinction = value; }
        /// <summary>Stops the run after this many minutes of wall-clock time since Initialize; 0 = no limit.</summary>
        public float WallClockMinutes { get => wallClockMinutes; set => wallClockMinutes = Mathf.Max(0, value); }

        /// <summary>The run's named random streams (RAND-02): every draw of the simulation comes from one. New at each
        /// Initialize.</summary>
        public RandomStreams Random { get; private set; }
        /// <summary>The run's event log and its running hash (RAND-10). New at each Initialize.</summary>
        public EventLog Events { get; private set; }
        /// <summary>True once Initialize succeeded; false before it, and after a validation error.</summary>
        public bool IsInitialized { get; private set; }
        /// <summary>The validation report of the last Initialize, or null before the first one.</summary>
        public ValidationReport LastReport { get; private set; }
        /// <summary>Why the run stopped ("tick limit", "stop file", "extinction of prey"…), or null while it runs.</summary>
        public string StopReason { get; private set; }
        /// <summary>Calls to Advance since Initialize. Test fakes count their delays in these calls (30 §2).</summary>
        public int AdvanceCalls { get; private set; }
        /// <summary>Times a phase made a tick wait since Initialize, once per tick and phase (scenario S14's wait counter).</summary>
        public int WaitCount { get; private set; }
        /// <summary>The phase the tick is waiting for, or null when it is not waiting.</summary>
        public TickPhase WaitingFor => waiting || blocked ? phases[nextPhase] : null;
        /// <summary>The index in <see cref="Phases"/> of the phase that runs next in the current tick (0 = between ticks).</summary>
        public int NextPhaseIndex => nextPhase;
        /// <summary>The context every phase receives, the same object for the whole run. Tests use it to run one phase by hand.</summary>
        public TickContext Context => context;

        /// <summary>
        /// The state of the run for the run controls and the editor: Idle before Initialize, Stopped, Blocked or Waiting
        /// when that holds, else what the controls asked (Running, Paused, Stepping).
        /// </summary>
        public RunState State =>
            !IsInitialized ? RunState.Idle : stopped ? RunState.Stopped : blocked ? RunState.Blocked :
            waiting ? RunState.Waiting : runMode;

        /// <summary>The enabled species of this World in hierarchy order, then those added during the run (SPEC-30).</summary>
        public IReadOnlyList<Species> AllSpecies => species;
        /// <summary>The tick phases in the order they run every tick: their hierarchy order (TICK-01).</summary>
        public IReadOnlyList<TickPhase> Phases => phases;
        /// <summary>The enabled services under the World, in hierarchy order.</summary>
        public IReadOnlyList<WorldService> Services => services;

        /// <summary>The first enabled service of that type, in hierarchy order.</summary>
        /// <typeparam name="T">The kind of service to find: a service class or an interface services implement.</typeparam>
        /// <returns>The service, or null when the World has none of that type.</returns>
        /// <remarks>Call it in a module's Initialize and keep the result: services are found by role, never by a scene
        /// reference (ARCH-08).</remarks>
        public T Service<T>() where T : class
        {
            foreach (var s in services) if (s is T t) return t;
            return null;
        }

        /// <summary>The service of that type with this name (its GameObject's name), ARCH-08.</summary>
        /// <typeparam name="T">The kind of service to find.</typeparam>
        /// <param name="name">The <see cref="WorldService.ServiceName"/> to match, e.g. <c>"Grass"</c> for a layer.</param>
        /// <returns>The first matching service in hierarchy order, or null.</returns>
        public T Service<T>(string name) where T : class
        {
            foreach (var s in services) if (s is T t && s.ServiceName == name) return t;
            return null;
        }

        /// <summary>Every enabled service of that type, in hierarchy order.</summary>
        /// <typeparam name="T">The kind of service to find: a service class or an interface.</typeparam>
        /// <returns>The matching services; empty when there is none.</returns>
        public IEnumerable<T> ServicesOf<T>() where T : class
        {
            foreach (var s in services) if (s is T t) yield return t;
        }

        /// <summary>The first phase of that type, or null.</summary>
        /// <typeparam name="T">The kind of phase to find.</typeparam>
        /// <returns>The first matching phase in tick order, or null when the World has none.</returns>
        public T Phase<T>() where T : TickPhase
        {
            foreach (var p in phases) if (p is T t) return t;
            return null;
        }

        /// <summary>A species by id, or null.</summary>
        /// <param name="id">The species' id (SPEC-01), as files and stream names use it.</param>
        /// <returns>The species with that id, or null when there is none.</returns>
        public Species FindSpecies(string id)
        {
            foreach (var s in species) if (s.Id == id) return s;
            return null;
        }

        /// <summary>A species by id or display name, or null.</summary>
        /// <param name="idOrName">An id, or a display name when no species has that id.</param>
        /// <returns>The species found, ids first, or null.</returns>
        public Species FindSpeciesByName(string idOrName)
        {
            foreach (var s in species) if (s.Id == idOrName) return s;
            foreach (var s in species) if (s.DisplayName == idOrName) return s;
            return null;
        }

        /// <summary>The next animal id: unique in the run, increasing, never reused (ANIM-02).</summary>
        /// <returns>An id no animal of this run had. <see cref="Species.CreateAnimal"/> takes one for each animal.</returns>
        public int NextAnimalId() => nextAnimalId++;

        // ---- Unity entry points: they only drive (ARCH-04) ----

        /// <summary>
        /// Heard by every World in Awake, before it starts on Play: a launcher (a player's command line, a test) sets the
        /// seed, speed or brain here, or turns StartOnPlay off. It runs before anything of the run exists.
        /// </summary>
        /// <remarks>
        /// Raised in Play mode only. The event is static: whoever subscribes must unsubscribe when done. See
        /// <see cref="PlayerLauncher"/> for the player's command line.
        /// </remarks>
        public static event Action<World> Configuring;

        /// <summary>
        /// How far behind a RealTime run may fall, in seconds of ticks (at least one tick): it falls behind while it waits,
        /// and catching up more at once would make the views jump.
        /// </summary>
        const float MaxRealTimeDebtSeconds = 0.1f;

        /// <summary>
        /// Unity's Awake: raises <see cref="Configuring"/>, then initialises and starts the run when StartOnPlay is on
        /// (Play mode only).
        /// </summary>
        void Awake()
        {
            if (Application.isPlaying) Configuring?.Invoke(this);
            if (startOnPlay && Application.isPlaying && !IsInitialized)
            {
                if (Initialize()) runMode = RunState.Running;
            }
        }

        /// <summary>Unity's FixedUpdate: runs TicksPerFixedUpdate ticks with the PerFixedUpdate speed. It only drives the loop
        /// (SPACE-10).</summary>
        void FixedUpdate()
        {
            if (!IsInitialized || stopped || runSpeed != RunSpeed.PerFixedUpdate) return;
            Drive(ticksPerFixedUpdate);
        }

        /// <summary>
        /// Unity's Update: drives the Fast speed (ticks until the frame budget is spent) and the RealTime speed (the
        /// ticks the clock owes).
        /// </summary>
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
                realTimeCarry = Mathf.Min(realTimeCarry, Mathf.Max(1f, realTimeTicksPerSecond * MaxRealTimeDebtSeconds));
                int n = Mathf.FloorToInt(realTimeCarry);
                if (n > 0) { realTimeCarry -= Drive(n); realTimeCarry = Mathf.Max(0, realTimeCarry); }
            }
        }

        /// <summary>Unity's LateUpdate: places the views. Visuals only: it never writes to the run (SPACE-12).</summary>
        void LateUpdate()
        {
            if (IsInitialized) SyncViews();               // visuals only (SPACE-12)
        }

        /// <summary>Leaving Play mode, or disabling the World, ends the run like any stop: its files are complete (RAND-20,
        /// OUT-03).</summary>
        void OnDisable()
        {
            if (!IsInitialized || stopped) return;
            Stop(nextPhase == 0 && !waiting ? "world disabled" : $"world disabled during {phases[nextPhase].PhaseName} of tick {Tick}");
        }

        /// <summary>
        /// Advances up to <paramref name="ticks"/> ticks as the run controls allow (paused, one step, a RunFor limit) and
        /// returns the ticks done.
        /// </summary>
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

        /// <summary>Starts or resumes the run, initialising the World first when needed; clears any RunFor limit.</summary>
        public void Play() { if (IsInitialized || Initialize()) { runForRemaining = -1; runMode = RunState.Running; } }
        /// <summary>Stops advancing until Resume, Play, Step or RunFor. A tick that is waiting stays where it is.</summary>
        public void Pause() => runMode = RunState.Paused;
        /// <summary>Continues a paused run.</summary>
        public void Resume() => runMode = RunState.Running;
        /// <summary>Runs one tick, then pauses. Does nothing before Initialize.</summary>
        public void Step() { if (IsInitialized) runMode = RunState.Stepping; }
        /// <summary>Runs that many more ticks, then pauses. Does nothing before Initialize.</summary>
        /// <param name="ticks">The ticks to run; a negative number counts as 0.</param>
        public void RunFor(int ticks) { if (IsInitialized) { runForRemaining = Math.Max(0, ticks); runMode = RunState.Running; } }

        /// <summary>Stops at the next tick boundary (a button, a stop file).</summary>
        /// <param name="reason">
        /// Why the run stops; <see cref="StopReason"/> and every <see cref="WorldModule.OnRunStopped"/> receive it.
        /// </param>
        /// <remarks>Between ticks it stops at once; inside a tick (a phase is waiting) it stops when that tick ends.</remarks>
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
        /// <param name="reason">
        /// Why the run stops; <see cref="StopReason"/> and every <see cref="WorldModule.OnRunStopped"/> receive it.
        /// </param>
        /// <remarks>
        /// Calls every world module's <see cref="WorldModule.OnRunStopped"/> once, in hierarchy order; an exception
        /// there is logged and the other modules still hear the stop. Does nothing when the run already stopped or was
        /// never initialised. Use <see cref="RequestStop"/> from anywhere else.
        /// </remarks>
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
        /// <returns>True when the World is ready to advance (paused); false when validation found an error, which is
        /// logged and kept in <see cref="LastReport"/>.</returns>
        /// <remarks>
        /// Runs <see cref="Prepare"/> (steps 1–8 of 20 §4), then makes the views (step 9), calls every world module's
        /// <see cref="WorldModule.Begin"/> in hierarchy order, and spawns the founders at tick 0 (step 10, POP-04).
        /// Calling it again starts a new run from the seed.
        /// </remarks>
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
        /// <param name="report">Receives every validation message (errors, warnings, information).</param>
        /// <returns>True when the report holds no error. It stops early at the first step that found one.</returns>
        /// <remarks>
        /// It resets the run state (tick 0, new random streams and event log from the seed) and leaves the World not
        /// initialised: <see cref="Initialize"/> calls it and then starts the run.
        /// </remarks>
        public bool Prepare(ValidationReport report)
        {
            // A fresh run: ids, streams and the events hash start again from the seed.
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
            // The !(x >= limit) form also catches NaN, which every comparison rejects.
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

        /// <summary>A fix that adds a phase of type T on a new GameObject named phaseName, beside the last phase (or under the
        /// World).</summary>
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

        /// <summary>
        /// V-18: services found by name must have unique names, and layer names must not be reserved stream names
        /// (ARCH-08, RAND-02).
        /// </summary>
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

        // Partial methods: each is implemented in another World.*.cs file, and does nothing where none is.

        /// <summary>Clears what the other parts keep between runs: alleles, food web, space, decisions (World.Animals.cs).</summary>
        partial void ResetSystems();
        /// <summary>Registers every founder allele before the run, so founders get the same ids in every run (GENE-21).</summary>
        partial void RegisterFounderPools();
        /// <summary>Step 7: derives the food web from the diets (SPEC-12, World.FoodWeb.cs).</summary>
        partial void DeriveRelations();
        /// <summary>Step 9: makes the view pools from each species' Body (20 §6, World.Views.cs).</summary>
        partial void CreateViews();
        /// <summary>Places every view, once per frame (World.Views.cs).</summary>
        partial void SyncViews();
        /// <summary>Step 10: creates the founders at tick 0 (POP-04, World.Animals.cs).</summary>
        partial void SpawnFoundersCore();
        /// <summary>Runs at the start of every tick, before its first phase (World.Animals.cs).</summary>
        partial void BeforeTick();
        /// <summary>Runs at the end of every tick, before <see cref="Tick"/> grows. No part implements it at present.</summary>
        partial void AfterTick();

        /// <summary>Step 10 of the set-up: the founders (POP-04).</summary>
        void SpawnFounders() => SpawnFoundersCore();

        // ---- The tick loop (SPACE-13, SPACE-14, TICK-01…07) ----

        /// <summary>
        /// Runs up to maxTicks ticks and returns how many completed. In Responsive mode it returns early when a
        /// phase must wait, and the same tick continues at that phase on a later call; in Freeze mode it blocks.
        /// </summary>
        /// <param name="maxTicks">The most ticks to run in this call.</param>
        /// <returns>The ticks completed in this call: fewer than <paramref name="maxTicks"/> when a phase waits
        /// (Responsive), when the run stops, or 0 once it stopped.</returns>
        /// <remarks>
        /// <para>Before running a phase it asks the phase's <see cref="TickPhase.WaitsFor"/>. A Pending that is not done
        /// counts once in <see cref="WaitCount"/>; then Responsive returns, and Freeze calls <see cref="Pending.Wait"/>.
        /// Stop conditions are checked between ticks only (RAND-20).</para>
        /// <para>An exception in a phase stops the run (its reason names the phase) and is thrown again.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The World is not initialised.</exception>
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
                    // This phase of this tick: a wait counts once, however many Advance calls it lasts.
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

        /// <summary>Ends a tick: back to the first phase, AfterTick, the tick count grows, then the stop conditions.</summary>
        void EndTick()
        {
            nextPhase = 0;
            AfterTick();
            Tick++;
            CheckStopAtBoundary();
        }

        /// <summary>Stop conditions, checked between ticks (RAND-20). Stops the run when one holds; true when the run is stopped.</summary>
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
