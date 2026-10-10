namespace EvoSim.Testing
{
    /// <summary>A test-only diet: every strike kills, and every meal gives exactly <see cref="Meal"/> energy.</summary>
    public class SureKillDiet : Diet
    {
        public float Meal = 1f;

        public override float KillChanceAgainst(Animal hunter, Animal prey) => 1f;

        public override float EnergyFrom(Animal eater, EdibleTarget entry, float energy) => Meal;
    }
}
