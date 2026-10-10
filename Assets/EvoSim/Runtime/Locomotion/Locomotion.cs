using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// How a species moves (MOVE-01): it turns each animal's intent into movement, and whatever it does is the tick's
    /// result. Inherit it for terrain, NavMesh or physics movement (MOVE-07). Speeds are traits (MOVE-03).
    /// </summary>
    public abstract class Locomotion : SpeciesModule
    {
        [SerializeField, Min(0f), Tooltip("Walking speed, in meters per tick (a trait; reference 1).")]
        protected float walkSpeed = 1f;
        [SerializeField, Min(0f), Tooltip("Running speed, in meters per tick (a trait; reference prey 1, predators 2).")]
        protected float runSpeed = 1f;
        [SerializeField, Range(0f, 1f), Tooltip("Probability per tick that a wandering animal turns (reference 0.25; MOVE-05).")]
        protected float wanderTurnP = 0.25f;
        [SerializeField, TextArea(2, 4), Tooltip("Rule line in the prompt; {walkSpeed}, {other.Module.field} placeholders (PROMPT-03).")]
        protected string promptRule = "";

        public TraitId WalkSpeed { get; private set; }
        public TraitId RunSpeed { get; private set; }
        public float WanderTurnP => wanderTurnP;
        public float WalkSpeedSetting => walkSpeed;
        public float RunSpeedSetting => runSpeed;

        public override void Declare(SpeciesBuilder b)
        {
            WalkSpeed = b.DeclareTrait("speed.walk", walkSpeed, 0f, 100f);
            RunSpeed = b.DeclareTrait("speed.run", runSpeed, 0f, 100f);
        }

        /// <summary>Move one animal this tick; return the meters actually moved (MOVE-06).</summary>
        public abstract float Move(Animal a, Intent intent, float staminaBudget, MoveContext c);

        /// <summary>Extra energy this movement costs (slopes, mud). None in the reference (MOVE-06).</summary>
        public virtual float ExtraCost(Animal a, Vector3 from, Vector3 to) => 0f;

        /// <summary>The act phase calls this for a group; override it to move many animals at once (ARCH-12).</summary>
        public virtual void MoveAll(IReadOnlyList<Animal> group, IReadOnlyList<Intent> intents, MoveContext c, Span<float> moved)
        {
            for (int i = 0; i < group.Count; i++)
                moved[i] = Move(group[i], intents[i], c.StaminaBudget(group[i]), c);
        }

        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        public void SetSpeeds(float walk, float run)
        {
            walkSpeed = walk;
            runSpeed = run;
        }

        public void SetWanderTurnP(float p) => wanderTurnP = Mathf.Clamp01(p);
        public void SetPromptRule(string text) => promptRule = text;

        public override void Validate(ValidationReport report)
        {
            if (!(wanderTurnP >= 0f && wanderTurnP <= 1f))
                report.Error("V-35", this, "The wander turn probability must be in [0, 1].", new ValidationFix("Clamp", () => SetWanderTurnP(float.IsNaN(wanderTurnP) ? 0.25f : wanderTurnP)));
        }
    }
}
