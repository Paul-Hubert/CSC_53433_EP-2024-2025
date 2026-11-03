using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// The animal's own state as numbers for the network (plan M16 §5.4): 7 values plus a one-hot of its previous action,
    /// 12 inputs with 5 actions. The token is the energy level, low, medium or high.
    /// </summary>
    /// <remarks>
    /// <para>Inputs, in order: energy / max; stamina / max; min(age / maturity, 1); ready to mate (0 or 1); on food (a
    /// grazeable item within the graze reach, or a carcass it may eat within the scavenge reach); in cover (hidden by a cover
    /// from at least one threat species, or, for a species with no threat, standing in cover); the previous action, one-hot
    /// over the species' actions (<see cref="Animal.Action"/>, all zeros before its first decision); min(ticks since its
    /// last meal / <see cref="HungerHorizon"/>, 1), from <see cref="HungerClock"/>.</para>
    /// <para>Everything is read from the animal's record and the start-of-tick world, so it keeps no hidden state (CORE-05)
    /// and draws no random number (SENSE-03).</para>
    /// </remarks>
    [RequiresModule(typeof(HungerClock))]
    public class InteroceptionSense : Sense, INeuralSense
    {
        /// <summary>The inputs before the previous-action one-hot, and the one after it.</summary>
        public const int FixedInputs = 7;
        /// <summary>The maturity used when the species has no mating rule, in ticks (the reference, ANIM-36).</summary>
        public const float ReferenceMaturity = 150f;

        static readonly IReadOnlyList<string> tokens = Array.AsReadOnly(new[] { "low", "medium", "high" });

        [SerializeField, Min(1), Tooltip("Ticks without a meal at which the hunger input reaches 1 (plan default 200).")]
        int hungerHorizon = 200;
        [SerializeField, Range(0f, 1f), Tooltip("Energy fraction below which the token says low.")]
        float low = 0.3f;
        [SerializeField, Range(0f, 1f), Tooltip("Energy fraction above which the token says high.")]
        float high = 0.7f;

        Energy energy;
        Stamina stamina;
        MatingRule mating;
        HungerClock clock;

        /// <summary>Ticks without a meal at which the hunger input reaches 1.</summary>
        public int HungerHorizon => hungerHorizon;

        /// <inheritdoc/>
        public int NeuralWidth => FixedInputs + (Species != null ? Species.Actions.Count : 0);
        /// <inheritdoc/>
        public override IReadOnlyList<string> Tokens => tokens;
        /// <inheritdoc/>
        public override bool HasAttachments => true;
        /// <inheritdoc/>
        protected override string DefaultLabel => "Body";

        /// <summary>Sets the hunger horizon from code (templates, tests).</summary>
        /// <param name="ticks">Ticks without a meal at which the input reaches 1.</param>
        /// <returns>This sense.</returns>
        public InteroceptionSense Configure(int ticks)
        {
            hungerHorizon = Math.Max(1, ticks);
            return this;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            energy = Species.Module<Energy>();
            stamina = Species.Module<Stamina>();
            mating = Species.Module<MatingRule>();
            clock = Species.Module<HungerClock>();
        }

        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            if (energy == null || energy.Max <= 0f) return 1;
            float f = a[energy.Value] / energy.Max;
            return f < low ? 0 : f > high ? 2 : 1;
        }

        /// <summary>Writes this animal's inputs, in the order of the remarks.</summary>
        /// <param name="a">The animal that decides.</param>
        /// <param name="values">Where to write; <see cref="NeuralWidth"/> long.</param>
        public void Fill(Animal a, Span<float> values)
        {
            values.Clear();
            var w = World;
            var q = w.Queries;
            values[0] = energy != null && energy.Max > 0f ? Mathf.Clamp01(a[energy.Value] / energy.Max) : 0f;
            if (stamina != null)
            {
                float max = a.Trait(stamina.Max);
                values[1] = max > 0f ? Mathf.Clamp01(a[stamina.Value] / max) : 0f;
            }
            float maturity = mating != null ? a.Trait(mating.Maturity) : ReferenceMaturity;
            values[2] = maturity > 0f ? Mathf.Min(a.Age / maturity, 1f) : 1f;
            values[3] = q.IsReady(a) ? 1f : 0f;
            values[4] = OnFood(a) ? 1f : 0f;
            values[5] = InCover(a) ? 1f : 0f;
            int actions = Species.Actions.Count;
            if (a.Action >= 0 && a.Action < actions) values[6 + a.Action] = 1f;
            float since = clock != null ? a[clock.SinceMeal] : 0f;
            values[6 + actions] = Mathf.Min(since / hungerHorizon, 1f);
        }

        /// <summary>True when a grazeable item or an edible carcass is within reach, so eating needs no step.</summary>
        bool OnFood(Animal a)
        {
            var q = World.Queries;
            var act = World.Phase<ActPhase>();
            float graze = act != null ? act.GrazeReach : 0.5f;
            float scavenge = act != null ? act.ScavengeReach : 1f;
            return q.NearestResource(a, "", graze) != null || q.NearestCarcass(a, scavenge) != null;
        }

        /// <summary>True when a cover hides the animal from a threat species; with no threat, when it stands in cover.</summary>
        bool InCover(Animal a)
        {
            var q = World.Queries;
            var threats = World.ThreatsOf(a.Species);
            if (threats.Count == 0) return q.InCover(a);
            for (int i = 0; i < threats.Count; i++)
                if (q.IsHiddenFrom(a, threats[i])) return true;
            return false;
        }

        /// <inheritdoc/>
        public override object Attachment(Animal a, SenseContext s)
        {
            var values = new float[NeuralWidth];
            Fill(a, values);
            return new NeuralInput(Index, values);
        }

        /// <inheritdoc/>
        public override string Write(int token, TextStyle style) =>
            style == TextStyle.V2 ? (token == 0 ? "I am hungry." : token == 2 ? "I am well fed." : "I am neither hungry nor full.")
                                  : $"{Label}: energy {tokens[token]}.";
    }
}
