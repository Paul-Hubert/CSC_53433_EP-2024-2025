using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Who may breed and with whom (11 §1): an adult with enough energy is ready (REPRO-01); an animal that chose mate
    /// breeds with a ready partner within reach, whatever the partner chose (REPRO-02), once per decision period (REPRO-03).
    /// </summary>
    /// <remarks>
    /// <para>One per species; a species without it never breeds. The breed phase takes, by id, each animal whose current
    /// action mates, that <see cref="IsReady"/> and hasn't bred this period, and pairs it with the nearest ready animal of
    /// its species within <see cref="Reach"/> that hasn't bred either (ties by id). When <see cref="Sexual"/> is false it
    /// breeds alone (REPRO-04). The litter, eggs and energy cost follow (<see cref="Litter"/>).</para>
    /// <para>Kin senses report readiness through the same <see cref="IsReady"/>. Overrides of <see cref="IsAdult"/>,
    /// <see cref="IsReady"/> and <see cref="Sexual"/> must only read the animal: no change, no random draw.</para>
    /// </remarks>
    public class MatingRule : SpeciesModule
    {
        [SerializeField, Min(0f), Tooltip("Energy needed to be ready to mate (reference 50).")]
        float mateEnergy = 50f;
        [SerializeField, Min(0f), Tooltip("Age from which an animal is an adult, in ticks (a trait; reference 150; ANIM-36).")]
        float maturity = 150f;
        [SerializeField, Min(0.01f), Tooltip("Distance within which two animals can breed, in meters (reference 1).")]
        float reach = 1f;
        [SerializeField, Tooltip("Two parents (reference). Off: asexual, one parent pays all (REPRO-04, control C7).")]
        bool sexual = true;
        [SerializeField, TextArea(2, 4), Tooltip("Rule line in the prompt (PROMPT-01).")]
        string promptRule = "Breeding needs only one of the two to choose mate: an adult that chooses mate\n" +
                            "breeds as soon as it reaches a ready partner, whatever the partner is doing.";

        Energy energy;                                               // the species' Energy module, or null

        /// <summary>
        /// The maturity trait: the age from which an animal is an adult, in ticks (reference 150; ANIM-36). A number gene
        /// may set it.
        /// </summary>
        public TraitId Maturity { get; private set; }
        /// <summary>The energy an animal needs to be ready to mate, in points (reference 50; REPRO-01).</summary>
        public float MateEnergy => mateEnergy;
        /// <summary>The distance within which two animals can breed, in meters (reference 1).</summary>
        public float Reach => reach;
        /// <summary>Sexual unless this rule or the world's asexual control (C7) says otherwise.</summary>
        public virtual bool Sexual => sexual && (World == null || !World.Asexual);

        /// <summary>Declares the maturity trait, so that every animal carries it.</summary>
        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b) => Maturity = b.DeclareTrait("maturity", maturity, 0f, 100000f);

        /// <summary>Finds the species' Energy module.</summary>
        /// <inheritdoc/>
        public override void Initialize() => energy = Species.Module<Energy>();

        /// <summary>Adult (ANIM-36): age at least the maturity trait.</summary>
        /// <param name="a">An animal of the species.</param>
        /// <returns>True when its age, in ticks, has reached its maturity trait.</returns>
        public virtual bool IsAdult(Animal a) => a.Age >= a.Trait(Maturity);

        /// <summary>Ready to mate (REPRO-01): adult, alive, energy at least mateEnergy.</summary>
        /// <param name="a">An animal of the species.</param>
        /// <returns>True when it is alive, adult and has at least <see cref="MateEnergy"/>; always false without an Energy
        /// module.</returns>
        public virtual bool IsReady(Animal a) => !a.Killed && !a.IsGone && IsAdult(a) && energy != null && a[energy.Value] >= mateEnergy;

        /// <summary>Writes the breeding rule line into the prompt (PROMPT-01).</summary>
        /// <inheritdoc/>
        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        /// <summary>Sets the rule from code (WorldBuilder, tests).</summary>
        /// <param name="energyNeeded">The energy needed to be ready, in points.</param>
        /// <param name="maturityTicks">The default maturity, in ticks.</param>
        /// <param name="breedReach">The breeding distance, in meters.</param>
        /// <param name="twoParents">True: two parents per birth. False: one parent alone.</param>
        public void Configure(float energyNeeded, float maturityTicks, float breedReach = 1f, bool twoParents = true)
        {
            mateEnergy = energyNeeded;
            maturity = maturityTicks;
            reach = breedReach;
            sexual = twoParents;
        }

        /// <summary>Sets the rule line of the prompt, from code.</summary>
        /// <param name="text">The line, as the brain reads it.</param>
        public void SetPromptRule(string text) => promptRule = text;

        /// <summary>Warns when the species has no Energy module: then no animal is ever ready (V-26).</summary>
        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Energy>() == null)
                report.Warning("V-26", this, "The mating rule needs an Energy module: without it no animal is ever ready.", BreedPhase.AddModule<Energy>(Species, "Energy"));
        }
    }
}
