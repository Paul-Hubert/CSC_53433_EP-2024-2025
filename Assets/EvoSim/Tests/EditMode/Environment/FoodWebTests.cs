using System.Linq;
using EvoSim.Testing;
using NUnit.Framework;

namespace EvoSim.Tests
{
    /// <summary>
    /// Checks the food web derived from diets and edible components (04 §2; SPEC-10…15; T-SPEC-03 and parts of T-SPEC-04
    /// and -08): threats and prey come from the diets alone, a cannibal is its own threat, and a diet entry needs an edible
    /// target (V-20, V-24).
    /// </summary>
    /// <remarks>
    /// The species hold only a <see cref="ProbeAction"/>, a <see cref="Diet"/> and, for the prey, an <see cref="Edible"/>:
    /// no reference action is needed to derive the food web. No tick is run.
    /// </remarks>
    public class FoodWebTests : WorldFixture
    {
        /// <summary>
        /// Adds a minimal food web to a builder: 48 m flat ground, grass, carcasses, a prey that eats grass and can be struck,
        /// and a predator whose diet is the prey and its carcasses.
        /// </summary>
        /// <param name="b">The builder, from <c>New()</c>.</param>
        /// <returns>The same builder, not yet built.</returns>
        static WorldBuilder Lab(WorldBuilder b) => b.Flat(48, 48).Food().Carcasses()
            .Species("prey", s => s.Action<ProbeAction>()
                .On<Edible>(e => e.Configure(EatMethod.Strike, 60, 2, 30, 100))
                .Module<Diet>(d => d.Set("Grass")))
            .Species("predator", s => s.Action<ProbeAction>()
                .Module<Diet>(d => d.Set("prey", "carcass:prey")));

        [Test, Description("T-SPEC-03 (SPEC-10, SPEC-12): the prey's threats are derived from the predator's diet; a lynx striking prey joins them")]
        public void ThreatsAreDerived()
        {
            var w = Lab(New()).Build();
            var prey = w.FindSpecies("prey");
            var predator = w.FindSpecies("predator");
            CollectionAssert.AreEqual(new[] { predator }, w.ThreatsOf(prey).ToArray());
            CollectionAssert.IsEmpty(w.ThreatsOf(predator));
            CollectionAssert.AreEqual(new[] { prey }, w.PreyOf(predator).ToArray());
            Assert.IsTrue(w.IsThreat(predator, prey));
            Assert.AreEqual(1, predator.Declarations.FindTrait("kill.chance").IsValid ? 1 : 0, "a hunter gets the kill-chance trait");
            Assert.IsFalse(prey.Declarations.FindTrait("kill.chance").IsValid);

            var w2 = Lab(New(name: "With lynx")).Species("lynx", s => s.Action<ProbeAction>().Module<Diet>(d => d.Set("prey"))).Build();
            var prey2 = w2.FindSpecies("prey");
            CollectionAssert.AreEqual(new[] { "predator", "lynx" }, w2.ThreatsOf(prey2).Select(s => s.Id).ToArray(), "no other change needed");
        }

        [Test, Description("SPEC-11 (part of T-SPEC-04): a cannibal species is among its own threats")]
        public void CannibalsAreTheirOwnThreat()
        {
            var w = New().Flat(20, 20).Food()
                .Species("cannibal", s => s.Action<ProbeAction>().On<Edible>(e => e.Configure(EatMethod.Strike, 60))
                    .Module<Diet>(d => d.Set("Grass", "cannibal")))
                .Build();
            var c = w.AllSpecies[0];
            CollectionAssert.Contains(w.ThreatsOf(c).ToArray(), c);
            CollectionAssert.Contains(w.Resolve(c, AnimalSet.Kin).ToArray(), c);
        }

        [Test, Description("SPEC-13, SPEC-15, V-24 (part of T-SPEC-08): a diet entry whose target has no Edible is an error; scales multiply the energy")]
        public void DietsNeedEdibleTargets()
        {
            var b = New().Flat(20, 20).Service<FoodGrid>("Grass").Service<EggSystem>("Eggs")
                .Species("prey", s => s.Action<ProbeAction>().Module<Diet>(d => d.Set("Grass", "egg:prey", "Nowhere")));
            var w = b.BuildUninitialized();
            var r = new ValidationReport();
            Assert.IsFalse(w.Prepare(r));
            Assert.AreEqual(2, r.Messages.Count(m => m.Id == "V-24"), "the grass and the eggs have no Edible: " + r);
            Assert.IsTrue(r.Has("V-20"), "Nowhere names nothing");

            var w2 = New(name: "Scaled").Flat(20, 20).Food(energy: 25)
                .Species("prey", s => s.Action<ProbeAction>().Module<Diet>(d => d.Set(new Diet.Entry("Grass", 1.5f))))
                .Build();
            var diet = w2.AllSpecies[0].Module<Diet>();
            var grazing = diet.Grazing(w2.Service<FoodGrid>());
            Assert.IsNotNull(grazing);
            Assert.AreEqual(37.5f, grazing.Energy, 1e-5f, "edible energy × the diet's scale");
            Assert.IsNull(diet.Striking(w2.AllSpecies[0]), "not in the diet: no interaction (SPEC-13)");
        }
    }
}
