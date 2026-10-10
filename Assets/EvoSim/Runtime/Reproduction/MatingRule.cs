using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Who may breed and with whom (11 §1): an adult with enough energy is ready (REPRO-01); an animal that chose mate
    /// breeds with a ready partner within reach, whatever the partner chose (REPRO-02), once per decision period (REPRO-03).
    /// </summary>
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

        Energy energy;

        public TraitId Maturity { get; private set; }
        public float MateEnergy => mateEnergy;
        public float Reach => reach;
        /// <summary>Sexual unless this rule or the world's asexual control (C7) says otherwise.</summary>
        public virtual bool Sexual => sexual && (World == null || !World.Asexual);

        public override void Declare(SpeciesBuilder b) => Maturity = b.DeclareTrait("maturity", maturity, 0f, 100000f);

        public override void Initialize() => energy = Species.Module<Energy>();

        /// <summary>Adult (ANIM-36): age at least the maturity trait.</summary>
        public virtual bool IsAdult(Animal a) => a.Age >= a.Trait(Maturity);

        /// <summary>Ready to mate (REPRO-01): adult, alive, energy at least mateEnergy.</summary>
        public virtual bool IsReady(Animal a) => !a.Killed && !a.IsGone && IsAdult(a) && energy != null && a[energy.Value] >= mateEnergy;

        public override void WritePromptRules(PromptWriter w) => w.Rule(promptRule);

        public void Configure(float energyNeeded, float maturityTicks, float breedReach = 1f, bool twoParents = true)
        {
            mateEnergy = energyNeeded;
            maturity = maturityTicks;
            reach = breedReach;
            sexual = twoParents;
        }

        public void SetPromptRule(string text) => promptRule = text;

        public override void Validate(ValidationReport report)
        {
            if (Species.Module<Energy>() == null)
                report.Warning("V-26", this, "The mating rule needs an Energy module: without it no animal is ever ready.", BreedPhase.AddModule<Energy>(Species, "Energy"));
        }
    }
}
