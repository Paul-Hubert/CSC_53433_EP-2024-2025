using System;

namespace EvoSim.Testing
{
    /// <summary>
    /// The Lab 1 reference species and world as ready-made recipes for tests (04 §5, 08 §7, 09 §3, 14 §2): prey (eat, flee,
    /// hide, follow, rest, mate) and predator (hunt, follow, rest, mate) with the prototype's values, founder pools,
    /// contrast pairs and prompt texts.
    /// </summary>
    /// <remarks>
    /// <para>Most members are extension methods on <see cref="WorldBuilder"/> and <see cref="SpeciesSetup"/>, so they chain
    /// with the builder's own calls. From the smallest to the largest:</para>
    /// <list type="bullet">
    /// <item><description><see cref="PreyBody"/>, <see cref="PreySenses"/>, <see cref="PreyActionSet"/> (and the predator's):
    /// one part of a reference species;</description></item>
    /// <item><description><see cref="Ecology"/>: ground, food, cover, carcasses, eggs and both species, with no phase and no
    /// brain;</description></item>
    /// <item><description><see cref="Lab1"/>: the whole Lab 1 small world (S03), ready to run.</description></item>
    /// </list>
    /// <para><see cref="Choose"/>, <see cref="StatOf"/> and <see cref="SetStat"/> help set up one animal after
    /// <see cref="Place.Animal"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var w = New(5, name: "hooks").Lab1(prey: 20, predators: 3).Build();
    /// w.Advance(800);
    /// var c = w.FindSpecies("prey").Counters;
    /// Assert.Greater(c.Births + c.DeathsTotal, 0, "something happened");
    /// </code>
    /// </example>
    public static class Lab
    {
        /// <summary>The reference prey's action names, in action order.</summary>
        public static readonly string[] PreyActions = { "eat", "flee", "hide", "follow", "rest", "mate" };
        /// <summary>The reference predator's action names, in action order.</summary>
        public static readonly string[] PredatorActions = { "hunt", "follow", "rest", "mate" };
        /// <summary>The V2 sentences of the stamina level sense, for low, medium and high (SENSE-11).</summary>
        public static readonly string[] StaminaWords = { "I am out of breath.", "I am getting tired.", "I am rested." };

        // ---- 09 §3: founder pools and contrast pairs (drafts awaiting the owner's review) ----
        /// <summary>The founder sentences of the prey's eat gene (09 §3).</summary>
        public static readonly string[] PreyEat = { "Eat whenever food is close.", "Only look for food when energy is low.", "Always finish eating before doing anything else.", "Eat quickly, then move on." };
        /// <summary>The founder sentences of the prey's flee gene (09 §3).</summary>
        public static readonly string[] PreyFlee = { "Run from any predator you see.", "Flee only when a predator is very close.", "Stay calm unless danger is right next to you.", "Run away from anything that attacks you." };
        /// <summary>The founder sentences of the prey's hide gene (09 §3).</summary>
        public static readonly string[] PreyHide = { "Hide when a predator is close.", "Stay in cover when danger is near.", "Hide only when you are tired.", "Leave cover to find food when hungry." };
        /// <summary>The founder sentences of the prey's follow gene (09 §3).</summary>
        public static readonly string[] PreyFollow = { "Stay close to other animals.", "Follow others when you are lost or hungry.", "Keep your distance from other animals.", "Follow the strongest animal nearby." };
        /// <summary>The founder sentences of the prey's rest gene (09 §3).</summary>
        public static readonly string[] PreyRest = { "Rest when you are tired.", "Never stop moving.", "Rest only when you feel safe.", "Save energy by resting when food is far." };
        /// <summary>The founder sentences of the prey's mate gene (09 §3).</summary>
        public static readonly string[] PreyMate = { "Look for a partner when energy is high.", "Mate with any nearby adult.", "Mate only when food is plentiful.", "Seek a partner before growing old." };
        /// <summary>The founder sentences of the predator's hunt gene (09 §3).</summary>
        public static readonly string[] PredatorHunt = { "Chase any prey you see.", "Hunt only when you are hungry.", "Attack only when prey is close.", "Keep chasing until the prey is caught." };
        /// <summary>The founder sentences of the predator's follow gene (09 §3).</summary>
        public static readonly string[] PredatorFollow = { "Stay close to other predators.", "Hunt as a pack.", "Keep away from other predators.", "Follow others when no prey is in sight." };
        /// <summary>The founder sentences of the predator's rest gene (09 §3).</summary>
        public static readonly string[] PredatorRest = { "Rest when your belly is full.", "Never stop moving.", "Lie still and let prey come to you.", "Rest when no prey is in sight." };
        /// <summary>The founder sentences of the predator's mate gene (09 §3).</summary>
        public static readonly string[] PredatorMate = { "Look for a mate when well fed.", "Mate with any nearby adult.", "Hunt first, mate later.", "Seek a partner before growing old." };

        // ---- 08 §7: prompt texts with placeholders (PROMPT-03) ----
        // Line breaks follow the prototype's prompts (prototype/prompts/teacher_v5.md, predator_v3.md), which JEV was trained on.
        /// <summary>
        /// The prey's locomotion rule line; its placeholders read the predator's speed and both stamina maxima (PROMPT-03).
        /// </summary>
        public const string PreyLocomotionRule = "Animals move {walkSpeed} meter per step. Predators run {predator.Locomotion.runSpeed} meters per step when they hunt,\n" +
                                                 "but they have {predator.Stamina.max} stamina against an animal's {Stamina.max}, so a long chase tires them first.";
        /// <summary>
        /// The predator's locomotion rule line; its placeholders read the prey's speed and both stamina maxima (PROMPT-03).
        /// </summary>
        public const string PredatorLocomotionRule = "Predators run {runSpeed} meters per step when they hunt and walk {walkSpeed} meter otherwise.\n" +
                                                     "Prey animals move {prey.Locomotion.walkSpeed} meter per step but have {prey.Stamina.max} stamina against a predator's {Stamina.max}.";
        /// <summary>The predator's search rule line: what happens when the chosen action has nothing in sight (ACT-04).</summary>
        public const string PredatorSearchRule = "If the chosen action has nothing to act on in sight (no prey, carcass, other\n" +
                                                 "predator or ready partner), the predator searches the surroundings instead.";
        /// <summary>The predator's stamina rule line (ANIM-21).</summary>
        public const string PredatorStaminaRule = "Every meter moved costs stamina. Standing still brings it back, which costs some\n" +
                                                  "energy until stamina is full. Without stamina a predator cannot move.";
        /// <summary>The first line of the predator's prompt (08 §7).</summary>
        public const string PredatorHeader = "You decide what a predator does next in a simple world.";
        /// <summary>The line before the predator's genes block in its prompt (08 §7).</summary>
        public const string PredatorGenesIntro = "This predator's instincts (its genes). They define its personality: follow them\n" +
                                                 "even when they seem unwise. Instincts that are meaningless have no effect.";

        /// <summary>
        /// Adds the reference prey's body: edible by strike (60 energy, a carcass of 2 portions of 30), the search rule,
        /// kinematic locomotion (walk 1 m per tick), energy (max 100, start 60), stamina, metabolism, a diet of grass, the
        /// mating rule, starvation and old age (1 500 ticks).
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <param name="stamina">The maximum stamina, in meters (reference prey 60).</param>
        /// <param name="run">The running speed, in meters per tick (reference prey 1).</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup PreyBody(this SpeciesSetup s, float stamina = 60f, float run = 1f)
        {
            return s.On<Edible>(e => e.Configure(EatMethod.Strike, 60f, 2, 30f, 100))
                .On<SearchRule>()
                .Module<KinematicLocomotion>(l => l.SetSpeeds(1f, run), "Locomotion")
                .Module<Energy>(e => e.Configure(100f, 60f))
                .Module<Stamina>(m => m.Configure(stamina, 2f, 0.3f))
                .Module<Metabolism>(m => m.Configure(0.7f, 0.5f, 0.2f))
                .Module<Diet>(d => d.Set("Grass"))
                .Module<MatingRule>(m => m.Configure(50f, 150f))
                .Module<Starvation>()
                .Module<OldAge>(o => o.SetMaxAge(1500));
        }

        /// <summary>
        /// Adds the reference predator's body: its prompt header, the search rule, kinematic locomotion (walk 1, run 2 m per
        /// tick), energy, stamina 30, metabolism, a diet that strikes the prey and scavenges its carcasses, digestion, the
        /// mating rule, starvation and old age.
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <param name="preyName">The species it hunts and whose carcasses it eats.</param>
        /// <param name="killChance">The probability that a strike kills, 0 to 1 (reference 0.5).</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup PredatorBody(this SpeciesSetup s, string preyName = "prey", float killChance = 0.5f)
        {
            s.Species.SetPromptHeader(PredatorHeader);
            s.Species.SetGenesIntro(PredatorGenesIntro);
            return s.On<SearchRule>(r => r.SetPromptRule(PredatorSearchRule))
                .Module<KinematicLocomotion>(l => l.SetSpeeds(1f, 2f), "Locomotion")
                .Module<Energy>(e => e.Configure(100f, 60f))
                .Module<Stamina>(m => { m.Configure(30f, 2f, 0.3f); m.SetPromptRule(PredatorStaminaRule); })
                .Module<Metabolism>(m => m.Configure(0.7f, 0.5f, 0.2f))
                .Module<Diet>(d => { d.Set(preyName, "carcass:" + preyName); d.SetKillChance(killChance); })
                .Module<Digestion>(d => d.Configure(50, 60f))
                .Module<MatingRule>(m => m.Configure(50f, 150f))
                .Module<Starvation>()
                .Module<OldAge>(o => o.SetMaxAge(1500));
        }

        /// <summary>
        /// Adds the prey's six actions in order (<see cref="PreyActions"/>), each with its text gene, founder pool and contrast
        /// pair (09 §3).
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup PreyActionSet(this SpeciesSetup s) => s
            .Action<EatAction>("eat", founders: PreyEat).Gene("eat", "Always eat, whatever happens.", "Never eat unless starving.")
            .Action<FleeAction>("flee", founders: PreyFlee).Gene("flee", "Always run away, whatever happens.", "Never run away from anything.")
            .Action<HideAction>("hide", founders: PreyHide).Gene("hide", "Always hide, whatever happens.", "Never hide in cover.")
            .Action<FollowAction>("follow", founders: PreyFollow).Gene("follow", "Always follow other animals.", "Never go near other animals.")
            .Action<RestAction>("rest", founders: PreyRest).Gene("rest", "Always rest and stay still.", "Never rest, keep moving.")
            .Action<MateAction>("mate", founders: PreyMate).Gene("mate", "Always try to mate.", "Never mate.");

        /// <summary>Adds the predator's four actions in order (<see cref="PredatorActions"/>), with their genes (09 §3).</summary>
        /// <param name="s">The species being set up.</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup PredatorActionSet(this SpeciesSetup s) => s
            .Action<HuntAction>("hunt", founders: PredatorHunt).Gene("hunt", "Always hunt, whatever happens.", "Never hunt unless starving.")
            .Action<FollowAction>("follow", a => a.SetDescription("move toward the nearest other predator"), PredatorFollow)
                .Gene("follow", "Always follow other predators.", "Never go near other predators.")
            .Action<RestAction>("rest", founders: PredatorRest).Gene("rest", "Always rest and stay still.", "Never rest, keep moving.")
            .Action<MateAction>("mate", a => a.SetDescription("walk to the nearest ready partner (another predator) in sight and breed with it"), PredatorMate)
                .Gene("mate", "Always try to mate.", "Never mate.");

        /// <summary>
        /// Sets the contrast pair of an action's gene: two extreme sentences used only to test brains (GENE-23),
        /// keeping its founders and neutral allele.
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <param name="action">The action's name; its GameObject carries the gene.</param>
        /// <param name="pro">The sentence that pushes toward the action ("Always eat, whatever happens.").</param>
        /// <param name="anti">The sentence that pushes away from it ("Never eat unless starving.").</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup Gene(this SpeciesSetup s, string action, string pro, string anti)
        {
            var g = s.Find<TextGene>(action);
            g.Configure(g.Founders, g.HasNeutral, g.Neutral, g.IsFree, pro, anti);
            return s;
        }

        /// <summary>
        /// Adds the prey's seven senses in situation-text order (04 §5): energy and stamina levels, food, predator, cover,
        /// other animal (with readiness), age.
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup PreySenses(this SpeciesSetup s) => s
            .Sense<LevelSense>(l => l.Configure("energy", 30f, 70f), "Energy")
            .Sense<LevelSense>(l => l.Configure("stamina", 20f, 40f, StaminaWords), "Stamina")
            .Sense<NearestResourceSense>(name: "Food")
            .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Threats), "Predator")
            .Sense<NearestCoverSense>(name: "Cover")
            .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Kin, readiness: true), "Animal")
            .Sense<AgeSense>(name: "Age");

        /// <summary>
        /// Adds the predator's six senses in situation-text order (04 §5): energy and stamina levels, prey, carcass, other
        /// predator (with readiness), age.
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup PredatorSenses(this SpeciesSetup s) => s
            .Sense<LevelSense>(l => l.Configure("energy", 30f, 70f), "Energy")
            .Sense<LevelSense>(l => l.Configure("stamina", 10f, 20f, StaminaWords), "Stamina")
            .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Prey), "Prey")
            .Sense<NearestEntitySense>(name: "Carcass")
            .Sense<NearestAnimalSense>(n => { n.Configure(AnimalSet.Kin, readiness: true); n.SetLabel("Other predator"); }, "OtherPredator")
            .Sense<AgeSense>(name: "Age");

        /// <summary>
        /// Adds the eleven reference phases in the order of TICK-04: sense, ask brains, choose actions, act, breed, hatch,
        /// death, migration, environment, floor, record.
        /// </summary>
        /// <param name="b">The world being built.</param>
        /// <param name="order">The act phase's order: species in turn (reference), all mixed, or simultaneous (ACT-30).</param>
        /// <returns>The same builder, to chain more calls.</returns>
        public static WorldBuilder ReferencePhases(this WorldBuilder b, ActOrder order = ActOrder.SpeciesInTurn) => b
            .Phase<SensePhase>().Phase<AskBrainsPhase>().Phase<ChooseActionsPhase>()
            .Phase<ActPhase>(p => p.Order = order)
            .Phase<BreedPhase>().Phase<HatchPhase>()
            .Phase<DeathPhase>().Phase<MigrationPhase>().Phase<EnvironmentPhase>().Phase<FloorPhase>().Phase<RecordPhase>();

        /// <summary>
        /// Adds a run recorder and the live statistics; the recorder writes no files unless asked, so tests leave no folders.
        /// </summary>
        /// <param name="b">The world being built.</param>
        /// <param name="files">True: write the run's files (events, stats, alleles…) to disk.</param>
        /// <param name="root">The folder runs are written under, relative to the project.</param>
        /// <param name="name">The run's folder name, where {world} and {seed} are filled in; empty = world, seed, date and time.</param>
        /// <param name="compatibility">True: write the prototype's file format too (OUT-05).</param>
        /// <returns>The same builder, to chain more calls.</returns>
        public static WorldBuilder Recorder(this WorldBuilder b, bool files = false, string root = "Logs/EvoSim/test-runs", string name = "",
                                            bool compatibility = false) => b
            .Service<RunRecorder>("Recording", r => r.Configure(root, name, compatibility, files))
            .Service<LiveStatistics>("Live statistics");

        /// <summary>
        /// Adds an <see cref="InvariantPhase"/> after the phases added so far: the invariants of 30 §5 are checked at that
        /// point of every tick, and the test fails at the first broken one.
        /// </summary>
        /// <param name="b">The world being built.</param>
        /// <returns>The same builder, to chain more calls.</returns>
        /// <remarks>Call it after <see cref="ReferencePhases"/>, so the checks run at the end of each tick.</remarks>
        public static WorldBuilder CheckInvariants(this WorldBuilder b) => b.Phase<InvariantPhase>();

        /// <summary>
        /// Adds the life rules of 11 with the reference values: litters of 2 to 4 babies of 40 energy, uniform crossover,
        /// incubation, a cap that migrates the excess and a floor that brings newcomers.
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <param name="cap">The most animals the species keeps (POP-01).</param>
        /// <param name="floor">The fewest animals: below it newcomers arrive (POP-03); 0 = none.</param>
        /// <param name="incubation">The ticks an egg incubates before it hatches; 0 = babies are born at once (REPRO-20).</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup LifeRules(this SpeciesSetup s, int cap, int floor, int incubation = 0) => s
            .Module<Litter>(l => l.Configure(2, 4, 40f))
            .Module<UniformCrossover>()
            .Module<Incubation>(i => i.Ticks = incubation)
            .Module<CapRule>(c => c.Configure(cap))
            .Module<FloorRule>(f => f.Floor = floor);

        /// <summary>
        /// Adds the LLM mutation with the reference deck v4 and context line (10 §2); the world needs a mutator, e.g.
        /// <see cref="FakeMutation"/>.
        /// </summary>
        /// <param name="s">The species being set up.</param>
        /// <param name="rate">The probability per gene per baby that the gene mutates (reference 0.03, MUT-01).</param>
        /// <returns>The same setup, to chain more calls.</returns>
        public static SpeciesSetup LlmMutation(this SpeciesSetup s, float rate = 0.03f) => s
            .Module<LlmMutation>(m =>
            {
                m.Configure(Deck, "The sentence below is a rule that a wild animal follows.");
                m.Rate = rate;
            });

        /// <summary>
        /// The reference instruction deck v4 (<c>Assets/EvoSim/Data/Decks/mutate_v4.txt</c>), read from disk as a TextAsset.
        /// </summary>
        public static UnityEngine.TextAsset Deck => new UnityEngine.TextAsset(System.IO.File.ReadAllText(
            System.IO.Path.Combine(UnityEngine.Application.dataPath, "EvoSim", "Data", "Decks", "mutate_v4.txt")));

        /// <summary>
        /// Adds a mutator service whose client is a <see cref="FakeMutator"/>: mutations are answered at once, by no model.
        /// </summary>
        /// <param name="b">The world being built.</param>
        /// <param name="configure">
        /// Called at once with the fake, e.g. to script its <see cref="FakeMutator.Answer"/>; null = it appends "quickly".
        /// </param>
        /// <returns>The same builder, to chain more calls.</returns>
        public static WorldBuilder FakeMutation(this WorldBuilder b, System.Action<FakeMutator> configure = null) => b
            .Service<MutatorService>("Mutator", m => m.Configure("fake", 1.2f, cached: false))
            .Service<FakeMutator>("Fake mutator client", configure);

        /// <summary>
        /// Adds a square world with flat ground, food, cover, carcasses and eggs, and the two reference species with no
        /// founders, no phase and no brain: add those yourself, or use <see cref="Lab1"/>.
        /// </summary>
        /// <param name="b">The world being built.</param>
        /// <param name="size">The side of the square, in meters (reference 48).</param>
        /// <param name="cover">The share of ground in cover, 0 to 1; 0 = no cover layer.</param>
        /// <param name="food">The share of cells holding food at the start, 0 to 1.</param>
        /// <param name="prey">Called with the prey's setup, to add or change modules (e.g. <c>s =&gt; s.Population(10)</c>).</param>
        /// <param name="predator">Called with the predator's setup, likewise.</param>
        /// <param name="regrow">The food's regrowth probability per empty cell per tick; 0 = no regrowth.</param>
        /// <returns>The same builder, to chain more calls.</returns>
        public static WorldBuilder Ecology(this WorldBuilder b, float size = 48f, float cover = 0.2f, float food = 0.1f,
                                           Action<SpeciesSetup> prey = null, Action<SpeciesSetup> predator = null, float regrow = 0.0015f)
        {
            b.Flat(size, size).Food(food, regrow);
            if (cover > 0f) b.Cover(cover);
            b.Carcasses().Service<EggSystem>("Eggs");
            b.Species("prey", s =>
            {
                s.PreyBody().PreySenses().PreyActionSet();
                s.Find<KinematicLocomotion>("Locomotion").SetPromptRule(PreyLocomotionRule);     // names the predator
                prey?.Invoke(s);
            });
            b.Species("predator", s =>
            {
                s.PredatorBody().PredatorSenses().PredatorActionSet();
                s.Find<KinematicLocomotion>("Locomotion").SetPromptRule(PredatorLocomotionRule); // names the prey
                predator?.Invoke(s);
            });
            return b;
        }

        /// <summary>
        /// Adds the Lab 1 small world (S03): 48 × 48 m, 24 prey (cap 40, floor 10), 4 predators (cap 6, floor 3), the
        /// reference phases, LLM mutation answered by a fake mutator, and the random brain as the world's default.
        /// </summary>
        /// <param name="b">The world being built.</param>
        /// <param name="prey">The prey founders.</param>
        /// <param name="predators">The predator founders.</param>
        /// <param name="size">The side of the square world, in meters.</param>
        /// <param name="order">The act phase's order (ACT-30).</param>
        /// <param name="randomBrain">
        /// False: no default brain; the test adds its own with <see cref="WorldBuilder.DefaultBrain{T}"/>.
        /// </param>
        /// <param name="incubation">The ticks eggs incubate; 0 = babies are born at once.</param>
        /// <param name="preyCap">The prey's cap.</param>
        /// <param name="predatorCap">The predators' cap.</param>
        /// <returns>The same builder: call <see cref="WorldBuilder.Build"/> to run it.</returns>
        public static WorldBuilder Lab1(this WorldBuilder b, int prey = 24, int predators = 4, float size = 48f,
                                        ActOrder order = ActOrder.SpeciesInTurn, bool randomBrain = true, int incubation = 0,
                                        int preyCap = 40, int predatorCap = 6)
        {
            b.Ecology(size,
                      prey: s => s.Population(prey).LifeRules(preyCap, 10, incubation).LlmMutation(),
                      predator: s => s.Population(predators).LifeRules(predatorCap, 3, incubation).LlmMutation())
             .ReferencePhases(order).FakeMutation();
            if (randomBrain) b.DefaultBrain<RandomBrain>();
            return b;
        }

        /// <summary>Sets an animal's chosen action by name, as if its brain had chosen it; it runs from the next act phase on.</summary>
        /// <param name="a">The animal.</param>
        /// <param name="actionName">The action's name in the animal's species ("eat", "hunt"…).</param>
        /// <exception cref="ArgumentException">The species has no action of that name.</exception>
        /// <remarks>A world with a brain and the decision phases may choose again at the next decision tick.</remarks>
        public static void Choose(this Animal a, string actionName)
        {
            var action = a.Species.FindAction(actionName) ?? throw new ArgumentException($"{a.Species.Id} has no action {actionName}");
            a.Action = action.Index;
        }

        /// <summary>
        /// Gives an animal a carrier and an overlay by name, as a decision that drew the overlay would (ACT-07): the overlay
        /// acts for its <see cref="AnimalAction.OverlayTicks"/>, counting the next tick.
        /// </summary>
        /// <param name="a">The animal.</param>
        /// <param name="carrier">The name of the action that moves it ("flee", "eat"…).</param>
        /// <param name="overlay">The name of an overlay of its species ("alarm").</param>
        /// <exception cref="ArgumentException">The species has no action of one of those names, or the second is no overlay.</exception>
        public static void Choose(this Animal a, string carrier, string overlay)
        {
            a.Choose(carrier);
            var o = a.Species.FindAction(overlay) ?? throw new ArgumentException($"{a.Species.Id} has no action {overlay}");
            if (!o.Overlay) throw new ArgumentException($"{overlay} is not an overlay (ACT-07)");
            a.Overlay = o.Index;
            a.OverlayLeft = Math.Max(0, o.OverlayTicks);
        }

        /// <summary>Reads one of an animal's stats by name.</summary>
        /// <param name="a">The animal.</param>
        /// <param name="stat">The stat's name, as its module declares it ("energy", "stamina", "thirst"…).</param>
        /// <returns>The stat's value now.</returns>
        public static float StatOf(this Animal a, string stat) => a[a.Species.Declarations.FindStat(stat)];

        /// <summary>Sets one of an animal's stats by name; the value is kept within the stat's range.</summary>
        /// <param name="a">The animal.</param>
        /// <param name="stat">The stat's name ("energy", "stamina"…).</param>
        /// <param name="value">The new value, in the stat's unit.</param>
        public static void SetStat(this Animal a, string stat, float value) => a[a.Species.Declarations.FindStat(stat)] = value;
    }
}
