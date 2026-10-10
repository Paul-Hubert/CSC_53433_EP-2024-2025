using UnityEngine;

namespace EvoSim
{
    /// <summary>One animal: plain data owned by its species (ANIM-01, ARCH-03). Modules read and write it through this record.</summary>
    public sealed class Animal
    {
        public readonly int Id;
        public readonly Species Species;

        public Vector3 Position, PreviousPosition;     // previous: where the view interpolates from
        public float Heading;                          // degrees around the vertical axis, for wandering
        public Genome Genome;
        public int Generation, BornTick, Age;
        public int[] Parents;
        /// <summary>"founder", "immigrant" or "birth".</summary>
        public string Origin;

        public int Action = -1;                        // index into Species.Actions; -1 = none
        public bool Searching, BredThisPeriod, Killed;
        public int KilledBy = -1, BusyTicks;
        public string BusyReason;
        public int Meals, Offspring;
        /// <summary>Removed from the world (dead or migrated); its record stays readable.</summary>
        public bool IsGone;

        public Observation LastObservation;            // for the inspector (DEC-21)
        public string LastSituation;
        public float[] LastProbabilities;
        public int LastDecisionTick = -1;

        /// <summary>Meters moved in the current tick, as the locomotion reported (MOVE-06).</summary>
        public float MetersMoved;
        /// <summary>Struck once already this tick as a hunter (ACT-12).</summary>
        public bool StruckThisTick;

        readonly float[] stats, traits;
        /// <summary>The spatial index cell (core bookkeeping, not animal state).</summary>
        internal int SpatialCell = -1;

        internal Animal(int id, Species species, int statCount, int traitCount)
        {
            Id = id;
            Species = species;
            stats = new float[statCount];
            traits = new float[traitCount];
        }

        /// <summary>A stat's value; writes are clamped to its range (ANIM-22).</summary>
        public float this[StatId s]
        {
            get => stats[s.Index];
            set => stats[s.Index] = s.Clamp(value, traits);
        }

        public float Trait(TraitId t) => traits[t.Index];

        /// <summary>Sets a trait. Only at creation, or for traits declared changeable (ANIM-17).</summary>
        public void SetTrait(TraitId t, float value) => traits[t.Index] = t.Clamp(value);

        public bool IsBusy => BusyTicks > 0;
        public bool IsAlive => !IsGone && !Killed;
        public AnimalAction CurrentAction => Action >= 0 && Action < Species.Actions.Count ? Species.Actions[Action] : null;

        internal float[] RawStats => stats;
        internal float[] RawTraits => traits;

        public override string ToString() => $"{Species?.Id}#{Id}";
    }
}
