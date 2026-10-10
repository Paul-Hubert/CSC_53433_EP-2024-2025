using System;

namespace EvoSim.Testing
{
    /// <summary>
    /// The Lab 1 reference species as module sets for tests (04 §5, 08 §7, 09 §3, 14 §2): prey (eat, flee, hide,
    /// follow, rest, mate) and predator (hunt, follow, rest, mate) with the prototype's values, founder pools,
    /// contrast pairs and prompt texts.
    /// </summary>
    public static class Lab
    {
        public static readonly string[] PreyActions = { "eat", "flee", "hide", "follow", "rest", "mate" };
        public static readonly string[] PredatorActions = { "hunt", "follow", "rest", "mate" };
        public static readonly string[] StaminaWords = { "I am out of breath.", "I am getting tired.", "I am rested." };

        // ---- 09 §3: founder pools and contrast pairs (drafts awaiting the owner's review) ----
        public static readonly string[] PreyEat = { "Eat whenever food is close.", "Only look for food when energy is low.", "Always finish eating before doing anything else.", "Eat quickly, then move on." };
        public static readonly string[] PreyFlee = { "Run from any predator you see.", "Flee only when a predator is very close.", "Stay calm unless danger is right next to you.", "Run away from anything that attacks you." };
        public static readonly string[] PreyHide = { "Hide when a predator is close.", "Stay in cover when danger is near.", "Hide only when you are tired.", "Leave cover to find food when hungry." };
        public static readonly string[] PreyFollow = { "Stay close to other animals.", "Follow others when you are lost or hungry.", "Keep your distance from other animals.", "Follow the strongest animal nearby." };
        public static readonly string[] PreyRest = { "Rest when you are tired.", "Never stop moving.", "Rest only when you feel safe.", "Save energy by resting when food is far." };
        public static readonly string[] PreyMate = { "Look for a partner when energy is high.", "Mate with any nearby adult.", "Mate only when food is plentiful.", "Seek a partner before growing old." };
        public static readonly string[] PredatorHunt = { "Chase any prey you see.", "Hunt only when you are hungry.", "Attack only when prey is close.", "Keep chasing until the prey is caught." };
        public static readonly string[] PredatorFollow = { "Stay close to other predators.", "Hunt as a pack.", "Keep away from other predators.", "Follow others when no prey is in sight." };
        public static readonly string[] PredatorRest = { "Rest when your belly is full.", "Never stop moving.", "Lie still and let prey come to you.", "Rest when no prey is in sight." };
        public static readonly string[] PredatorMate = { "Look for a mate when well fed.", "Mate with any nearby adult.", "Hunt first, mate later.", "Seek a partner before growing old." };

        // ---- 08 §7: prompt texts with placeholders (PROMPT-03) ----
        // Line breaks follow the prototype's prompts (prototype/prompts/teacher_v5.md, predator_v3.md), which JEV was trained on.
        public const string PreyLocomotionRule = "Animals move {walkSpeed} meter per step. Predators run {predator.Locomotion.runSpeed} meters per step when they hunt,\n" +
                                                 "but they have {predator.Stamina.max} stamina against an animal's {Stamina.max}, so a long chase tires them first.";
        public const string PredatorLocomotionRule = "Predators run {runSpeed} meters per step when they hunt and walk {walkSpeed} meter otherwise.\n" +
                                                     "Prey animals move {prey.Locomotion.walkSpeed} meter per step but have {prey.Stamina.max} stamina against a predator's {Stamina.max}.";
        public const string PredatorSearchRule = "If the chosen action has nothing to act on in sight (no prey, carcass, other\n" +
                                                 "predator or ready partner), the predator searches the surroundings instead.";
        public const string PredatorStaminaRule = "Every meter moved costs stamina. Standing still brings it back, which costs some\n" +
                                                  "energy until stamina is full. Without stamina a predator cannot move.";
        public const string PredatorHeader = "You decide what a predator does next in a simple world.";
        public const string PredatorGenesIntro = "This predator's instincts (its genes). They define its personality: follow them\n" +
                                                 "even when they seem unwise. Instincts that are meaningless have no effect.";

        /// <summary>Locomotion, stats, metabolism, diet, edible, mating and death rules of the reference prey.</summary>
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

        /// <summary>The reference predator: stamina 30, runs 2 m per tick, strikes prey, scavenges prey carcasses.</summary>
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

        /// <summary>The prey's six actions, each with its text gene, founder pool and contrast pair (09 §3).</summary>
        public static SpeciesSetup PreyActionSet(this SpeciesSetup s) => s
            .Action<EatAction>("eat", founders: PreyEat).Gene("eat", "Always eat, whatever happens.", "Never eat unless starving.")
            .Action<FleeAction>("flee", founders: PreyFlee).Gene("flee", "Always run away, whatever happens.", "Never run away from anything.")
            .Action<HideAction>("hide", founders: PreyHide).Gene("hide", "Always hide, whatever happens.", "Never hide in cover.")
            .Action<FollowAction>("follow", founders: PreyFollow).Gene("follow", "Always follow other animals.", "Never go near other animals.")
            .Action<RestAction>("rest", founders: PreyRest).Gene("rest", "Always rest and stay still.", "Never rest, keep moving.")
            .Action<MateAction>("mate", founders: PreyMate).Gene("mate", "Always try to mate.", "Never mate.");

        /// <summary>The predator's four actions with their genes (09 §3).</summary>
        public static SpeciesSetup PredatorActionSet(this SpeciesSetup s) => s
            .Action<HuntAction>("hunt", founders: PredatorHunt).Gene("hunt", "Always hunt, whatever happens.", "Never hunt unless starving.")
            .Action<FollowAction>("follow", a => a.SetDescription("move toward the nearest other predator"), PredatorFollow)
                .Gene("follow", "Always follow other predators.", "Never go near other predators.")
            .Action<RestAction>("rest", founders: PredatorRest).Gene("rest", "Always rest and stay still.", "Never rest, keep moving.")
            .Action<MateAction>("mate", a => a.SetDescription("walk to the nearest ready partner (another predator) in sight and breed with it"), PredatorMate)
                .Gene("mate", "Always try to mate.", "Never mate.");

        /// <summary>Sets the contrast pair of an action's gene.</summary>
        public static SpeciesSetup Gene(this SpeciesSetup s, string action, string pro, string anti)
        {
            var g = s.Find<TextGene>(action);
            g.Configure(g.Founders, g.HasNeutral, g.Neutral, g.IsFree, pro, anti);
            return s;
        }

        /// <summary>The prey's seven senses, in situation-text order (04 §5).</summary>
        public static SpeciesSetup PreySenses(this SpeciesSetup s) => s
            .Sense<LevelSense>(l => l.Configure("energy", 30f, 70f), "Energy")
            .Sense<LevelSense>(l => l.Configure("stamina", 20f, 40f, StaminaWords), "Stamina")
            .Sense<NearestResourceSense>(name: "Food")
            .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Threats), "Predator")
            .Sense<NearestCoverSense>(name: "Cover")
            .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Kin, readiness: true), "Animal")
            .Sense<AgeSense>(name: "Age");

        /// <summary>The predator's six senses (04 §5).</summary>
        public static SpeciesSetup PredatorSenses(this SpeciesSetup s) => s
            .Sense<LevelSense>(l => l.Configure("energy", 30f, 70f), "Energy")
            .Sense<LevelSense>(l => l.Configure("stamina", 10f, 20f, StaminaWords), "Stamina")
            .Sense<NearestAnimalSense>(n => n.Configure(AnimalSet.Prey), "Prey")
            .Sense<NearestEntitySense>(name: "Carcass")
            .Sense<NearestAnimalSense>(n => { n.Configure(AnimalSet.Kin, readiness: true); n.SetLabel("Other predator"); }, "OtherPredator")
            .Sense<AgeSense>(name: "Age");

        /// <summary>The reference phases implemented so far, in the order of TICK-04.</summary>
        public static WorldBuilder ReferencePhases(this WorldBuilder b, ActOrder order = ActOrder.SpeciesInTurn) => b
            .Phase<SensePhase>().Phase<AskBrainsPhase>().Phase<ChooseActionsPhase>()
            .Phase<ActPhase>(p => p.Order = order)
            .Phase<BreedPhase>().Phase<HatchPhase>()
            .Phase<DeathPhase>().Phase<MigrationPhase>().Phase<EnvironmentPhase>().Phase<FloorPhase>().Phase<RecordPhase>();

        /// <summary>A run recorder (files off unless asked, so tests leave no folders) and live statistics.</summary>
        public static WorldBuilder Recorder(this WorldBuilder b, bool files = false, string root = "Logs/EvoSim/test-runs", string name = "",
                                            bool compatibility = false) => b
            .Service<RunRecorder>("Recording", r => r.Configure(root, name, compatibility, files))
            .Service<LiveStatistics>("Live statistics");

        /// <summary>The invariants of 30 §5, checked after every tick.</summary>
        public static WorldBuilder CheckInvariants(this WorldBuilder b) => b.Phase<InvariantPhase>();

        /// <summary>Litters, crossover, incubation, cap and floor (11, reference values).</summary>
        public static SpeciesSetup LifeRules(this SpeciesSetup s, int cap, int floor, int incubation = 0) => s
            .Module<Litter>(l => l.Configure(2, 4, 40f))
            .Module<UniformCrossover>()
            .Module<Incubation>(i => i.Ticks = incubation)
            .Module<CapRule>(c => c.Configure(cap))
            .Module<FloorRule>(f => f.Floor = floor);

        /// <summary>The LLM mutation with the reference deck v4 and context line (10 §2).</summary>
        public static SpeciesSetup LlmMutation(this SpeciesSetup s, float rate = 0.03f) => s
            .Module<LlmMutation>(m =>
            {
                m.Configure(Deck, "The sentence below is a rule that a wild animal follows.");
                m.Rate = rate;
            });

        /// <summary>The reference deck v4 (Data/Decks/mutate_v4.txt) as a TextAsset.</summary>
        public static UnityEngine.TextAsset Deck => new UnityEngine.TextAsset(System.IO.File.ReadAllText(
            System.IO.Path.Combine(UnityEngine.Application.dataPath, "EvoSim", "Data", "Decks", "mutate_v4.txt")));

        /// <summary>A mutator service with a fake client (no model).</summary>
        public static WorldBuilder FakeMutation(this WorldBuilder b, System.Action<FakeMutator> configure = null) => b
            .Service<MutatorService>("Mutator", m => m.Configure("fake", 1.2f, cached: false))
            .Service<FakeMutator>("Fake mutator client", configure);

        /// <summary>A world with flat ground, food, cover, carcasses, eggs, prey and predators (no phases, no brain).</summary>
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
        /// The Lab 1 small world (S03): 48 × 48, 24 prey (cap 40, floor 10), 4 predators (cap 6, floor 3), the reference
        /// phases, LLM mutation answered by a fake mutator, and the random brain as the world's default.
        /// </summary>
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

        /// <summary>Sets an animal's chosen action by name.</summary>
        public static void Choose(this Animal a, string actionName)
        {
            var action = a.Species.FindAction(actionName) ?? throw new ArgumentException($"{a.Species.Id} has no action {actionName}");
            a.Action = action.Index;
        }

        /// <summary>A stat by name (energy, stamina…).</summary>
        public static float StatOf(this Animal a, string stat) => a[a.Species.Declarations.FindStat(stat)];

        /// <summary>Sets a stat by name.</summary>
        public static void SetStat(this Animal a, string stat, float value) => a[a.Species.Declarations.FindStat(stat)] = value;
    }
}
