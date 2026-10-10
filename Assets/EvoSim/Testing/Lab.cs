using System;

namespace EvoSim.Testing
{
    /// <summary>
    /// The Lab 1 reference species as module sets for tests (04 §5, 14 §2): prey (eat, flee, hide, follow, rest, mate)
    /// and predator (hunt, follow, rest, mate), with the prototype's values. Senses and genes come with their milestones.
    /// </summary>
    public static class Lab
    {
        public static readonly string[] PreyActions = { "eat", "flee", "hide", "follow", "rest", "mate" };
        public static readonly string[] PredatorActions = { "hunt", "follow", "rest", "mate" };

        /// <summary>Stats, metabolism, locomotion, diet, edible, digestion and death rules of the reference prey.</summary>
        public static SpeciesSetup PreyBody(this SpeciesSetup s, float stamina = 60f, float run = 1f)
        {
            return s.On<Edible>(e => e.Configure(EatMethod.Strike, 60f, 2, 30f, 100))
                .Module<Energy>(e => e.Configure(100f, 60f))
                .Module<Stamina>(m => m.Configure(stamina, 2f, 0.3f))
                .Module<Metabolism>(m => m.Configure(0.7f, 0.5f, 0.2f))
                .Module<KinematicLocomotion>(l => l.SetSpeeds(1f, run))
                .Module<Diet>(d => d.Set("Grass"))
                .Module<MatingRule>(m => m.Configure(50f, 150f))
                .Module<Starvation>()
                .Module<OldAge>(o => o.SetMaxAge(1500));
        }

        /// <summary>The reference predator's modules: stamina 30, runs 2 m per tick, strikes prey, scavenges prey carcasses.</summary>
        public static SpeciesSetup PredatorBody(this SpeciesSetup s, string preyName = "prey", float killChance = 0.5f)
        {
            return s.Module<Energy>(e => e.Configure(100f, 60f))
                .Module<Stamina>(m => m.Configure(30f, 2f, 0.3f))
                .Module<Metabolism>(m => m.Configure(0.7f, 0.5f, 0.2f))
                .Module<KinematicLocomotion>(l => l.SetSpeeds(1f, 2f))
                .Module<Diet>(d => { d.Set(preyName, "carcass:" + preyName); d.SetKillChance(killChance); })
                .Module<Digestion>(d => d.Configure(50, 60f))
                .Module<MatingRule>(m => m.Configure(50f, 150f))
                .Module<Starvation>()
                .Module<OldAge>(o => o.SetMaxAge(1500));
        }

        /// <summary>The prey's six actions, each with a text gene.</summary>
        public static SpeciesSetup PreyActionSet(this SpeciesSetup s) => s
            .Action<EatAction>("eat").Action<FleeAction>("flee").Action<HideAction>("hide")
            .Action<FollowAction>("follow").Action<RestAction>("rest").Action<MateAction>("mate");

        /// <summary>The predator's four actions, each with a text gene.</summary>
        public static SpeciesSetup PredatorActionSet(this SpeciesSetup s) => s
            .Action<HuntAction>("hunt").Action<FollowAction>("follow").Action<RestAction>("rest").Action<MateAction>("mate");

        /// <summary>A world with flat ground, food, cover, carcasses, eggs, the given phases, prey and predators.</summary>
        public static WorldBuilder Ecology(this WorldBuilder b, float size = 48f, float cover = 0.2f, float food = 0.1f,
                                           Action<SpeciesSetup> prey = null, Action<SpeciesSetup> predator = null, float regrow = 0.0015f)
        {
            b.Flat(size, size).Food(food, regrow);
            if (cover > 0f) b.Cover(cover);
            b.Carcasses().Service<EggSystem>("Eggs");
            b.Species("prey", s => { s.PreyBody().PreyActionSet(); prey?.Invoke(s); });
            b.Species("predator", s => { s.PredatorBody().PredatorActionSet(); predator?.Invoke(s); });
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
