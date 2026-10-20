using UnityEngine;

namespace EvoSim
{
    /// <summary>One animal: plain data owned by its species (ANIM-01, ARCH-03). Modules read and write it through this record.</summary>
    /// <remarks>
    /// <para>An animal is no GameObject and has no Update: phases loop over the animals of each species
    /// (<see cref="Species.Animals"/>), and a pooled view only shows it. Species modules keep their per-animal values
    /// here, as stats and traits they declared (ANIM-10, ANIM-15): read and write a stat with the indexer, a trait with
    /// <see cref="Trait"/> and <see cref="SetTrait"/>.</para>
    /// <para>Animals are made by <see cref="Species.CreateAnimal"/> and join the living with <see cref="Species.Add"/>.
    /// A dead animal is marked <see cref="IsGone"/> by <see cref="World.RecordDeath"/>; its record stays readable.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Energy energy;
    /// public override void Initialize() => energy = Species.Module&lt;Energy&gt;();
    ///
    /// public override int Read(Animal a, SenseContext s) => a[energy.Value] &lt; 30f ? 1 : 0;   // a stat, by its id
    /// float vision = a.Trait(visionTrait);                                                  // a trait, fixed at birth
    /// </code>
    /// </example>
    public sealed class Animal
    {
        /// <summary>The animal's id: unique in the run, increasing, never reused (ANIM-02).</summary>
        public readonly int Id;
        /// <summary>The species that owns it.</summary>
        public readonly Species Species;

        /// <summary>Where it stands now, and where it stood at the start of the tick, in world coordinates (meters).</summary>
        public Vector3 Position, PreviousPosition;     // previous: where the view interpolates from
        /// <summary>The direction it faces, in degrees around the vertical axis (0 = +z); wandering keeps it.</summary>
        public float Heading;                          // degrees around the vertical axis, for wandering
        /// <summary>Its genome: one allele per gene of its species, in locus order; null for an animal without genes.</summary>
        public Genome Genome;
        /// <summary>
        /// Generation: 0 for founders and newcomers, else one more than its parents'. BornTick: the tick it was created.
        /// Age: in ticks; the death phase adds one every tick (ANIM-35).
        /// </summary>
        public int Generation, BornTick, Age;
        /// <summary>The parents' ids; empty for founders and newcomers.</summary>
        public int[] Parents;
        /// <summary>"founder", "immigrant" or "birth".</summary>
        public string Origin;

        /// <summary>
        /// The current action: an index into <see cref="EvoSim.Species.Actions"/>, or -1 before its first decision. Under
        /// an overlay it is the carrier; the overlay itself when the row left no carrier, and the animal then stays (ACT-07).
        /// </summary>
        public int Action = -1;                        // index into Species.Actions; -1 = none
        /// <summary>
        /// The overlay drawn at its last decision (ACT-07), an index into <see cref="EvoSim.Species.Actions"/>, or -1: none,
        /// or it has ended (its ticks ran out, or the animal became busy).
        /// </summary>
        public int Overlay = -1;
        /// <summary>Ticks the overlay still acts, this one included; 0 = until the next decision. Meaningless when <see cref="Overlay"/> is -1.</summary>
        public int OverlayLeft;
        /// <summary>
        /// Searching: its action found nothing to act on since the last decision, so it wanders (ACT-04).
        /// BredThisPeriod: it bred since its last decision. Killed: struck dead this tick; the death phase records it.
        /// </summary>
        public bool Searching, BredThisPeriod, Killed;
        /// <summary>KilledBy: the id of the animal that killed it, or -1. BusyTicks: ticks it stays busy (ANIM-30); 0 = not busy.</summary>
        public int KilledBy = -1, BusyTicks;
        /// <summary>Why it is busy (e.g. digesting), or null.</summary>
        public string BusyReason;
        /// <summary>Meals: food items, kills or portions it ate. Offspring: babies it had. Both count over its whole life.</summary>
        public int Meals, Offspring;
        /// <summary>Removed from the world (dead or migrated); its record stays readable.</summary>
        public bool IsGone;

        /// <summary>The observation of its last decision, for the inspector (DEC-21); null before its first.</summary>
        public Observation LastObservation;            // for the inspector (DEC-21)
        /// <summary>The situation text of its last decision, for the inspector; null before its first.</summary>
        public string LastSituation;
        /// <summary>The brain's probabilities at its last decision, in action order, for the inspector; null before its first.</summary>
        public float[] LastProbabilities;
        /// <summary>The tick of its last decision, or -1 before its first.</summary>
        public int LastDecisionTick = -1;

        /// <summary>Meters moved in the current tick, as the locomotion reported (MOVE-06).</summary>
        public float MetersMoved;
        /// <summary>Struck once already this tick as a hunter (ACT-12).</summary>
        public bool StruckThisTick;
        /// <summary>The animal its action was about last time it acted (followed, fled, chased), or -1 (inspector, 22 §13).</summary>
        public int TargetId = -1;
        /// <summary>Where its last intent was heading.</summary>
        public Vector3 TargetPoint;

        readonly float[] stats, traits;                // values of the declared stats and traits, by their ids' Index
        /// <summary>The spatial index cell (core bookkeeping, not animal state).</summary>
        internal int SpatialCell = -1;

        /// <summary>
        /// Makes an empty record with room for the species' stats and traits; only <see cref="EvoSim.Species.CreateAnimal"/> calls it.
        /// </summary>
        internal Animal(int id, Species species, int statCount, int traitCount)
        {
            Id = id;
            Species = species;
            stats = new float[statCount];
            traits = new float[traitCount];
        }

        /// <summary>A stat's value; writes are clamped to its range (ANIM-22).</summary>
        /// <param name="s">The stat's id, as its module's <see cref="SpeciesBuilder.DeclareStat"/> returned it.</param>
        /// <remarks>A write above the stat's maximum (or its cap trait's value) keeps the maximum; below its minimum, the
        /// minimum.</remarks>
        public float this[StatId s]
        {
            get => stats[s.Index];
            set => stats[s.Index] = s.Clamp(value, traits);
        }

        /// <summary>A trait's value for this animal (ANIM-15).</summary>
        /// <param name="t">The trait's id, as <see cref="SpeciesBuilder.DeclareTrait"/> or <see cref="SpeciesBuilder.FindTrait"/>
        /// returned it.</param>
        /// <returns>The value, fixed at creation unless the trait is changeable.</returns>
        public float Trait(TraitId t) => traits[t.Index];

        /// <summary>Sets a trait. Only at creation, or for traits declared changeable (ANIM-17).</summary>
        /// <param name="t">The trait's id.</param>
        /// <param name="value">The new value; clamped to the trait's range (ANIM-18).</param>
        /// <exception cref="System.InvalidOperationException">The animal is created and the trait is not changeable.</exception>
        public void SetTrait(TraitId t, float value)
        {
            if (Created && !t.Changeable)
                throw new System.InvalidOperationException($"Trait '{t.Name}' of {this} is set at creation only; declare it changeable to change it during life (ANIM-17).");
            traits[t.Index] = t.Clamp(value);
        }

        /// <summary>False while the species creates it (traits from the genome); then non-changeable traits are fixed (ANIM-17).</summary>
        public bool Created { get; internal set; }

        /// <summary>True while it is busy (digesting…): it neither decides nor moves, and the counter drops by one per tick
        /// (ANIM-30).</summary>
        public bool IsBusy => BusyTicks > 0;
        /// <summary>True when it is neither gone nor killed this tick.</summary>
        public bool IsAlive => !IsGone && !Killed;
        /// <summary>The action it is doing, or null before its first decision.</summary>
        public AnimalAction CurrentAction => Action >= 0 && Action < Species.Actions.Count ? Species.Actions[Action] : null;
        /// <summary>The overlay it is doing beside its action (ACT-07), or null.</summary>
        public AnimalAction CurrentOverlay => Overlay >= 0 && Overlay < Species.Actions.Count ? Species.Actions[Overlay] : null;

        /// <summary>
        /// What it is doing, for the inspector and the run overlay: "none", the action's name ("flee"), or the carrier and
        /// the overlay ("flee + alarm"; "stay + alarm" when the row left no carrier; ACT-07).
        /// </summary>
        public string ActionLabel
        {
            get
            {
                var action = CurrentAction;
                if (action == null) return "none";
                string carrier = action.Overlay ? "stay" : action.Name;
                var overlay = CurrentOverlay;
                return overlay != null ? carrier + " + " + overlay.Name : carrier;
            }
        }

        /// <summary>Ends the overlay, if any (at a decision, when the animal becomes busy, at death; ACT-07).</summary>
        public void EndOverlay()
        {
            Overlay = -1;
            OverlayLeft = 0;
        }

        /// <summary>The stat values, by index, without clamping (the species fills them at creation).</summary>
        internal float[] RawStats => stats;
        /// <summary>The trait values, by index, without the creation check (the species and gene expression fill them).</summary>
        internal float[] RawTraits => traits;

        /// <summary>The species id and animal id, e.g. <c>prey#12</c>, for logs and messages.</summary>
        /// <returns>The text <c>&lt;species id&gt;#&lt;id&gt;</c>.</returns>
        public override string ToString() => $"{Species?.Id}#{Id}";
    }
}
