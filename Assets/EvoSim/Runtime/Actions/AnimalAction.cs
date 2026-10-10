using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>A behaviour the brain can choose (ACT-01). Each tick it gives an intent and maybe an interaction; it never moves anything itself.</summary>
    public abstract class AnimalAction : SpeciesModule
    {
        [SerializeField, TextArea(1, 4), Tooltip("The action's line in the prompt, after its name (PROMPT-01, PROMPT-07).")]
        protected string description = "";

        /// <summary>The action's name, unique in its species (ACT-01): its GameObject's name.</summary>
        public string Name => gameObject.name;
        /// <summary>The prompt line: the inspector's text, or the action's default when empty.</summary>
        public string Description => string.IsNullOrEmpty(description) ? DefaultDescription : description;

        /// <summary>The reference prompt line of this action (08 §7), used when the inspector field is empty.</summary>
        protected virtual string DefaultDescription => "";

        /// <summary>The text gene bound to this action (GENE-05), or null (V-05).</summary>
        public TextGene Gene { get; internal set; }

        /// <summary>True for an action that chases and strikes animals: senses such as the sample ChasedSense read it.</summary>
        public virtual bool Hunts => false;

        /// <summary>This action's index in the species' action order.</summary>
        public int Index { get; internal set; } = -1;

        /// <summary>Every tick while this action is chosen: say where to go, and what to do on arrival (ACT-02, ACT-03).</summary>
        public abstract void Act(Animal a, ActContext c);

        /// <summary>
        /// The batch entry point (20 §10, ARCH-12): the act phase plans every animal of the group (all chose this action)
        /// through it. The default calls Act for each; an override must give the plans Act would give.
        /// </summary>
        public virtual void ActAll(IReadOnlyList<Animal> group, ActContext c, Span<ActPlan> plans)
        {
            for (int i = 0; i < group.Count; i++)
            {
                c.Begin(group[i]);
                Act(group[i], c);
                plans[i] = c.Plan;
            }
        }

        /// <summary>Directed tests: is this observation relevant for this action (CTRL-10)?</summary>
        public virtual bool IsRelevant(ObservationView o) => true;

        /// <summary>True for actions that rest: the metabolism charges its rest cost when the animal stays still.</summary>
        public virtual bool Rests => false;

        /// <summary>True for actions that seek a partner: the breed phase breeds animals that chose one (REPRO-02).</summary>
        public virtual bool Mates => false;

        public override void WritePromptRules(PromptWriter w) => w.Action(Name, Description);

        /// <summary>Sets the prompt line from code (WorldBuilder, tests).</summary>
        public void SetDescription(string text) => description = text;
    }
}
