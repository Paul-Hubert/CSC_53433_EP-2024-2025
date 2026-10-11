using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// How a species moves (MOVE-01): it turns each animal's intent into movement, and whatever it does is the tick's
    /// result. Inherit it for terrain, NavMesh or physics movement (MOVE-07). Speeds are traits (MOVE-03).
    /// </summary>
    /// <remarks>
    /// <para>Each species has exactly one locomotion (MOVE-01, V-13); place it anywhere under the species. In the act
    /// phase, after the animal's action planned its <see cref="Intent"/>, the phase calls <see cref="MoveAll"/> for the
    /// animals whose intent moves; the default calls <see cref="Move"/> for each one with its stamina budget. Then the
    /// metabolism charges the meters <see cref="Move"/> reported, plus <see cref="ExtraCost"/> (MOVE-06, ANIM-20). No
    /// other module assumes the intent was followed exactly.</para>
    /// <para>What an override must keep:</para>
    /// <list type="bullet">
    /// <item><description>change only the moved animal's <see cref="Animal.Position"/>;</description></item>
    /// <item><description>never end on non-walkable ground or outside the world (MOVE-02): use <see cref="Ground.SlideTo"/>
    /// or <see cref="Ground.Clear"/>. The act phase moves an animal back, with a warning, if it does;</description></item>
    /// <item><description>never move more than the stamina budget (ANIM-21), and read speeds from the animal's traits,
    /// never from the settings (ANIM-16);</description></item>
    /// <item><description>return the meters actually moved, measured with <see cref="MoveContext.Distance"/>
    /// (MOVE-06);</description></item>
    /// <item><description>draw random numbers only from <see cref="MoveContext.Stream"/>.</description></item>
    /// </list>
    /// <para>The sample <c>SlopeLocomotion</c> (22 §14) derives from <see cref="KinematicLocomotion"/> instead, to refuse
    /// steep ground and charge for climbing.</para>
    /// </remarks>
    /// <example>
    /// A straight-line locomotion, the reference <see cref="KinematicLocomotion"/> without its random sidestep:
    /// <code>
    /// public class StraightLocomotion : Locomotion
    /// {
    ///     public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
    ///     {
    ///         if (intent.IsStay) return 0f;
    ///         float speed = a.Trait(intent.Run ? RunSpeed : WalkSpeed);
    ///         float d = Mathf.Min(speed, Mathf.Min(staminaBudget, intent.MaxDistance));
    ///         var from = a.Position;
    ///         a.Position = c.Ground.SlideTo(from, from + intent.Direction * d);    // never onto non-walkable ground
    ///         return c.Distance(from, a.Position);
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Locomotion : SpeciesModule
    {
        /// <summary>The walking speed setting, in meters per tick: the default of the <c>speed.walk</c> trait (MOVE-03).</summary>
        [SerializeField, Min(0f), Tooltip("Walking speed, in meters per tick (a trait; reference 1).")]
        protected float walkSpeed = 1f;
        /// <summary>The running speed setting, in meters per tick: the default of the <c>speed.run</c> trait (MOVE-03).</summary>
        [SerializeField, Min(0f), Tooltip("Running speed, in meters per tick (a trait; reference prey 1, predators 2).")]
        protected float runSpeed = 1f;
        /// <summary>The probability per tick that a wandering animal turns (MOVE-05).</summary>
        [SerializeField, Range(0f, 1f), Tooltip("Probability per tick that a wandering animal turns (reference 0.25; MOVE-05).")]
        protected float wanderTurnP = 0.25f;
        /// <summary>The locomotion's rule line in the prompt, with placeholders; empty adds no line (PROMPT-03).</summary>
        [SerializeField, TextArea(2, 4), Tooltip("Rule line in the prompt; {walkSpeed}, {other.Module.field} placeholders (PROMPT-03).")]
        protected string promptRule = "";

        /// <summary>
        /// The walking speed trait, <c>speed.walk</c>, in meters per tick: read it with <c>a.Trait(WalkSpeed)</c>
        /// (ANIM-16).
        /// </summary>
        public TraitId WalkSpeed { get; private set; }
        /// <summary>
        /// The running speed trait, <c>speed.run</c>, in meters per tick: read it with <c>a.Trait(RunSpeed)</c>
        /// (ANIM-16).
        /// </summary>
        public TraitId RunSpeed { get; private set; }
        /// <summary>
        /// The probability per tick that a wandering animal turns (MOVE-05; reference 0.25), read by
        /// <see cref="ActContext.Wander"/>.
        /// </summary>
        public float WanderTurnP => wanderTurnP;
        /// <summary>The walking speed setting of the Inspector, in meters per tick (the trait's default).</summary>
        public float WalkSpeedSetting => walkSpeed;
        /// <summary>The running speed setting of the Inspector, in meters per tick (the trait's default).</summary>
        public float RunSpeedSetting => runSpeed;

        /// <summary>Declares the speed traits <c>speed.walk</c> and <c>speed.run</c>, from 0 to 100 meters per tick.</summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b)
        {
            WalkSpeed = b.DeclareTrait("speed.walk", walkSpeed, 0f, 100f);
            RunSpeed = b.DeclareTrait("speed.run", runSpeed, 0f, 100f);
        }

        /// <summary>Move one animal this tick; return the meters actually moved (MOVE-06).</summary>
        /// <param name="a">The animal to move: set its <see cref="Animal.Position"/>.</param>
        /// <param name="intent">
        /// What its action wants: direction, walk or run, maximum distance. The act phase only passes intents that move,
        /// but return 0 for a stay all the same.
        /// </param>
        /// <param name="staminaBudget">The most it may move this tick, in meters (ANIM-21); infinity without a stamina module.</param>
        /// <param name="c">The ground, the world distance and the random streams.</param>
        /// <returns>The meters actually moved, measured with <see cref="MoveContext.Distance"/>; 0 when the animal didn't move.</returns>
        public abstract float Move(Animal a, Intent intent, float staminaBudget, MoveContext c);

        /// <summary>Extra energy this movement costs (slopes, mud). None in the reference (MOVE-06).</summary>
        /// <param name="a">The animal that moved.</param>
        /// <param name="from">Where it was before moving.</param>
        /// <param name="to">Where it is after moving.</param>
        /// <returns>Energy points to pay, 0 or more; a negative value counts as 0.</returns>
        /// <remarks>
        /// The act phase calls it after the animal moved more than 1 mm and passes it to <see cref="Metabolism.Settle"/>,
        /// which pays it on top of the tick's cost (ANIM-20).
        /// </remarks>
        public virtual float ExtraCost(Animal a, Vector3 from, Vector3 to) => 0f;

        /// <summary>The act phase calls this for a group; override it to move many animals at once (ARCH-12).</summary>
        /// <param name="group">
        /// Animals of this species whose intents move: one animal in the sequential act orders, all of them in the
        /// simultaneous one (ACT-30).
        /// </param>
        /// <param name="intents">Their intents, at the same index.</param>
        /// <param name="c">The move context.</param>
        /// <param name="moved">Where to write the meters each animal moved, at the same index.</param>
        /// <remarks>An override must give the same result as calling <see cref="Move"/> for each animal in order (20 §10).</remarks>
        public virtual void MoveAll(IReadOnlyList<Animal> group, IReadOnlyList<Intent> intents, MoveContext c, Span<float> moved)
        {
            for (int i = 0; i < group.Count; i++)
                moved[i] = Move(group[i], intents[i], c.StaminaBudget(group[i]), c);
        }

        /// <summary>Adds the locomotion's rule line, its placeholders such as <c>{walkSpeed}</c> filled (PROMPT-03).</summary>
        /// <inheritdoc/>
        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        /// <summary>Sets the speed settings from code (WorldBuilder, scenarios), before the species declares its traits.</summary>
        /// <param name="walk">Walking speed, in meters per tick.</param>
        /// <param name="run">Running speed, in meters per tick.</param>
        public void SetSpeeds(float walk, float run)
        {
            walkSpeed = walk;
            runSpeed = run;
        }

        /// <summary>Sets the probability per tick that a wandering animal turns.</summary>
        /// <param name="p">A probability; clamped to [0, 1].</param>
        public void SetWanderTurnP(float p) => wanderTurnP = Mathf.Clamp01(p);
        /// <summary>Sets the prompt rule line from code (WorldBuilder, tests).</summary>
        /// <param name="text">The new line; empty adds no line.</param>
        public void SetPromptRule(string text) => promptRule = text;

        /// <summary>Reports a turn probability outside [0, 1] (V-35), with a fix that clamps it.</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (!(wanderTurnP >= 0f && wanderTurnP <= 1f))
                report.Error("V-35", this, "The wander turn probability must be in [0, 1].", new ValidationFix("Clamp", () => SetWanderTurnP(float.IsNaN(wanderTurnP) ? 0.25f : wanderTurnP)));
        }
    }
}
