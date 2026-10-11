namespace EvoSim.Testing
{
    /// <summary>A test-only diet: every strike kills, and every meal gives exactly <see cref="Meal"/> energy.</summary>
    /// <remarks>It shows that a diet decides kills and meals (ACT-11): a student's diet overrides the same two methods.</remarks>
    public class SureKillDiet : Diet
    {
        /// <summary>The energy of every meal, in points, whatever was eaten.</summary>
        public float Meal = 1f;

        /// <summary>Always 1: every strike kills.</summary>
        /// <inheritdoc/>
        public override float KillChanceAgainst(Animal hunter, Animal prey) => 1f;

        /// <summary>Always <see cref="Meal"/>.</summary>
        /// <inheritdoc/>
        public override float EnergyFrom(Animal eater, EdibleTarget entry, float energy) => Meal;
    }
}
