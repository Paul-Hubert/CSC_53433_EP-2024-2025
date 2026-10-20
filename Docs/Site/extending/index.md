---
uid: extend-index
---

# Extending EvoSim

Everything in EvoSim is a component you can add, remove, replace or subclass: senses, actions, genes, stats, death rules,
mutation operators, brains, tick phases, food layers, cover, ground. The reference modules are written exactly like
yours: they derive from the same public base classes and use no hidden shortcut. Any of them is a working example of its
kind, and any of them can be replaced by your own.

## The workflow

1. **Write a class.** *Assets ▸ Create ▸ EvoSim ▸ Script ▸* writes a template for a **Sense**, an **Action**, a **Gene
   kind**, a **Stat**, a **Phase**, a **Brain** or a **Mutation operator** into `Assets/Student`, together with a test
   in `Assets/Student/Tests`. For other kinds, derive from the base class named in the table below.
2. **Place the component.** A module under a species belongs to that species. A phase or a service sits under the World,
   outside any species. Order matters only among modules of the same kind: the n-th action found, depth first, is
   action n. See [Species modules](species-modules.md) and [Phases, services and world hooks](world-modules.md).
3. **Press Play.** The World discovers your component, sets it up in its fixed order, and validates the tree. If
   something is missing, a message names the object to fix.
4. **Test it.** Your sense, action, stat, layer, mutation operator, phase or locomotion runs through the conformance
   suite of its kind: *Window ▸ General ▸ Test Runner*, EditMode. A brain gets a test to copy instead. See
   [Testing your module](testing-your-module.md).

## I want to…

| I want to… | Use | Code? | Page |
|---|---|---|---|
| tell the brain something new about the situation | a <xref:EvoSim.Sense> (distance bands: <xref:EvoSim.BandedSense>; raycasts: <xref:EvoSim.BatchedSense>) | yes | [A sense](sense.md) |
| give animals a new behaviour to choose | an <xref:EvoSim.AnimalAction>, with a text gene to steer it | yes | [An action](action.md) |
| make something happen when an animal reaches its target | an <xref:EvoSim.IInteraction> | yes | [An action](action.md#interactions) |
| change how a species moves (slopes, mud, paths) | a <xref:EvoSim.Locomotion> subclass | yes | [A locomotion](locomotion.md) |
| add a stat (thirst, fear, temperature) | a <xref:EvoSim.SpeciesModule> that declares it | yes | [Stats, costs and death rules](stats-and-death.md) |
| change what living costs per tick | <xref:EvoSim.Metabolism> (settings, or a subclass) | maybe | [Stats, costs and death rules](stats-and-death.md) |
| add a cause of death | a <xref:EvoSim.DeathRule> | yes | [Stats, costs and death rules](stats-and-death.md) |
| give a species a temperament that is not tied to one action | a free <xref:EvoSim.TextGene> | no | [Genes](genes.md) |
| let evolution tune a number (stamina, speed, vision) | a <xref:EvoSim.NumberGene> and a <xref:EvoSim.GaussianMutation> | no | [Genes](genes.md) |
| invent a new kind of gene | a <xref:EvoSim.Gene> subclass | yes | [Genes](genes.md) |
| change how gene sentences mutate | a mutation deck, or a <xref:EvoSim.MutationOperator> | maybe | [Mutation](mutation.md) |
| change how parents' genes mix, litter sizes, who mates | <xref:EvoSim.Crossover>, <xref:EvoSim.Litter>, <xref:EvoSim.MatingRule> | maybe | [Reproduction and population](reproduction.md) |
| lay eggs that hatch later, cap or prop up a population | <xref:EvoSim.Incubation>, <xref:EvoSim.CapRule>, <xref:EvoSim.FloorRule> | no | [Reproduction and population](reproduction.md) |
| decide at other moments, or stagger decisions | a <xref:EvoSim.DecisionSchedule> on the species | no | [A brain](brain.md#decision-timing) |
| decide with rules, a small network or another model | a <xref:EvoSim.Brain> | yes | [A brain](brain.md) |
| use a model behind a web API, or another mutator server | an <xref:EvoSim.HttpBrain>, a <xref:EvoSim.MutatorClient> | yes | [An HTTP brain or mutator server](http-brains.md) |
| run something every tick (seasons, weather, events) | a <xref:EvoSim.TickPhase> | yes | [Phases, services and world hooks](world-modules.md) |
| share state or a query between modules | a <xref:EvoSim.WorldService> | yes | [Phases, services and world hooks](world-modules.md) |
| react to every birth and death | `OnBorn` and `OnDied` on any module | yes | [Phases, services and world hooks](world-modules.md#hooks) |
| set worlds up from code before they start | the <xref:EvoSim.World.Configuring> event | yes | [Phases, services and world hooks](world-modules.md#hooks) |
| add a food source (berries, water) | a <xref:EvoSim.ResourceLayer> | yes | [Food, cover, entities and ground](environment.md) |
| add hiding places | a <xref:EvoSim.Cover> | yes | [Food, cover, entities and ground](environment.md) |
| add objects that live in the world (fruit, nests) | an <xref:EvoSim.EntitySystem%601> of your <xref:EvoSim.Entity> | yes | [Food, cover, entities and ground](environment.md) |
| make something edible, or change who eats whom | <xref:EvoSim.Edible> and <xref:EvoSim.Diet> | no | [Food, cover, entities and ground](environment.md#who-eats-what) |
| change what the ground allows | a <xref:EvoSim.Ground> | yes | [Food, cover, entities and ground](environment.md) |
| add a species | duplicate one and change it; subclass <xref:EvoSim.Species> only for hooks | no | [Species modules](species-modules.md#species) |
| record more numbers or events | <xref:EvoSim.IStatsColumns>, <xref:EvoSim.IEventSink>, `World.RecordDeath` | yes | [Recording and statistics](recording.md) |
| colour or animate the animals | an <xref:EvoSim.AnimalView> on a body prefab | yes | [Views](views.md) |
| say that your module needs another one | `[RequiresModule(typeof(…))]` | yes | [Species modules](species-modules.md#dependencies) |
| catch configuration mistakes before Play | `Validate(ValidationReport)` and a <xref:EvoSim.ValidationFix> | yes | [Species modules](species-modules.md#validation) |
| add your own editor window or inspector | Unity's editor API, reading the World | yes | [The editor](editor.md) |

Every abstract and virtual member of every public type is listed in [Every extension point](extension-points.md),
generated from the code.

## Five rules every module keeps

1. **Draw random numbers only from the stream you are given** (a <xref:EvoSim.RandomStream>), never from
   `UnityEngine.Random`, `System.Random` or the clock. Otherwise a seed no longer replays the run (RAND-01).
2. **Never decide by iterating a `Dictionary` or a `HashSet`**: their order is not fixed. Iterate lists (RAND-05).
3. **Keep settings in the module and state in the animal.** One module serves every animal of its species. What
   belongs to one animal goes in its stats, through the ids your module declared.
4. **Each kind of module does its own job.** Senses only read. Actions only say where to go and what to do on
   arrival; the act phase moves the animal. Phases change the world. Views only display (SPACE-12).
5. **Set up in `Initialize`, not in `Awake` or `Start`.** The World calls its modules in a fixed order (ARCH-04).

> [!TIP]
> The fastest way in is to rewrite a reference module from its stub: [the teaching path](exercises.md). The
> [samples](samples.md) (thirst, water, seasons, slopes…) show complete extensions working together.
> [Worked example: alarm calls](worked-example-alarm-calls.md) follows one student change, a sense, an action, a gene
> and a world service, from the idea to a tested scene.
