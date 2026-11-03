using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// Counts the ticks since each animal's last meal (plan M16 §5.4), for <see cref="InteroceptionSense"/>: two stats,
    /// updated once a tick in the death phase, the hook of the thirst recipe (22 §1). It never gives a cause of death.
    /// </summary>
    /// <remarks>
    /// The state lives in the animal's stats (CORE-05): <c>neuro.mealsSeen</c> is the meal count at the last update and
    /// <c>neuro.sinceMeal</c> the ticks since <see cref="Animal.Meals"/> last grew. A graze, a strike or a scavenge each
    /// count one meal (<c>InteractionResolver.Eat</c>), so any of them resets the clock to 0.
    /// </remarks>
    public class HungerClock : DeathRule
    {
        /// <summary>A ceiling for the counter, so it stays a finite stat on very long lives.</summary>
        public const float MaxTicks = 1e9f;

        /// <summary>The meal count the clock last saw (stat <c>neuro.mealsSeen</c>).</summary>
        public StatId MealsSeen { get; private set; }
        /// <summary>The ticks since the last meal (stat <c>neuro.sinceMeal</c>); 0 at birth.</summary>
        public StatId SinceMeal { get; private set; }

        /// <inheritdoc/>
        public override void Declare(SpeciesBuilder b)
        {
            MealsSeen = b.DeclareStat("neuro.mealsSeen", StatStart.Fixed(0f), 0f, MaxTicks);
            SinceMeal = b.DeclareStat("neuro.sinceMeal", StatStart.Fixed(0f), 0f, MaxTicks);
        }

        /// <summary>Never a cause of death; updates nothing here (<see cref="CheckAll"/> updates every animal).</summary>
        /// <param name="a">A living animal.</param>
        /// <returns>Null.</returns>
        public override string CauseOfDeath(Animal a) => null;

        /// <summary>Updates every living animal's clock, whatever cause an earlier rule gave it; gives no cause.</summary>
        /// <param name="animals">The species' living animals.</param>
        /// <param name="causes">Left as they are.</param>
        public override void CheckAll(IReadOnlyList<Animal> animals, string[] causes)
        {
            for (int i = 0; i < animals.Count; i++) Tick(animals[i]);
        }

        /// <summary>One tick of one animal's clock: back to 0 when it ate since the last update, else one more tick.</summary>
        /// <param name="a">The animal.</param>
        public void Tick(Animal a)
        {
            if (a.Meals > a[MealsSeen])
            {
                a[MealsSeen] = a.Meals;
                a[SinceMeal] = 0f;
            }
            else a[SinceMeal] = Mathf.Min(a[SinceMeal] + 1f, MaxTicks);
        }
    }
}
