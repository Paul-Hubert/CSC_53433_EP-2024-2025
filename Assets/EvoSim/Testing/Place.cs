using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>
    /// Puts animals, food items, cover and carcasses at exact positions in a built world (30 §2), so a test sets up the
    /// very situation it checks. Records no event.
    /// </summary>
    /// <remarks>
    /// Call it after <see cref="WorldBuilder.Build"/>. An animal placed this way is added to its species at once (the
    /// species' modules hear <c>OnBorn</c>) and has no chosen action: set one with <see cref="Lab.Choose"/>, or let a brain
    /// decide on the next decision tick.
    /// </remarks>
    /// <example>
    /// <code>
    /// var w = New().Ground&lt;PondGround&gt;(20, 20).Food(0f, 0f).Phase&lt;ActPhase&gt;()
    ///     .Species("prey", s =&gt; s.PreyBody().Action&lt;DrinkAction&gt;("drink").Module&lt;Thirst&gt;()).Build();
    /// var sp = w.FindSpecies("prey");
    /// var a = Place.Animal(sp, Place.At(10f, 15f));       // 2 m from the pond's shore
    /// var thirst = sp.Module&lt;Thirst&gt;().Value;
    /// a[thirst] = 50f;
    /// a.Choose("drink");
    /// w.Advance(3);
    /// Assert.AreEqual(0f, a[thirst], "drank");
    /// </code>
    /// </example>
    public static class Place
    {
        /// <summary>A point on the ground plane (height 0; <see cref="Animal"/> sets the height from the ground).</summary>
        /// <param name="x">The x coordinate, in meters.</param>
        /// <param name="z">The z coordinate, in meters.</param>
        /// <returns>The point (x, 0, z).</returns>
        public static Vector3 At(float x, float z) => new Vector3(x, 0f, z);

        /// <summary>
        /// Creates an animal of a species at a point and adds it to the living animals: by default each gene has its pool's
        /// first founder allele.
        /// </summary>
        /// <param name="s">The species, from <c>w.FindSpecies("prey")</c>.</param>
        /// <param name="p">Where the animal stands; its height is set from the ground.</param>
        /// <param name="genome">The animal's genome; null = each gene's first founder allele.</param>
        /// <param name="heading">The direction it faces, in degrees around the vertical axis (0 = +z, 90 = +x).</param>
        /// <param name="origin">How it came into the world, as recorded on the animal ("founder", "birth", "immigrant"…).</param>
        /// <param name="atCreation">
        /// Called after the genome sets the traits and before the stats start from them: change a trait here (ANIM-17).
        /// </param>
        /// <returns>The new animal: generation 0, age 0, no parents, no chosen action.</returns>
        public static Animal Animal(Species s, Vector3 p, Genome genome = null, float heading = 0f, string origin = "founder",
                                    System.Action<Animal> atCreation = null)
        {
            var w = s.World;
            if (genome == null)
            {
                var alleles = new Allele[s.Genes.Count];
                for (int i = 0; i < alleles.Length; i++)
                {
                    var f = s.Genes[i].FounderPool[0];
                    alleles[i] = w.Alleles.Register(s.Genes[i], f.Value, f.Origin);
                }
                genome = new Genome(s, alleles);
            }
            if (w.Ground != null) p = w.Ground.OnGround(p);
            var a = s.CreateAnimal(origin, p, heading, genome, 0, null, atCreation);
            s.Add(a);
            return a;
        }

        /// <summary>
        /// Creates an animal whose text genes hold the given sentences, locus by locus in the species' gene order; a missing
        /// or null sentence takes the gene's first founder allele.
        /// </summary>
        /// <param name="s">The species.</param>
        /// <param name="p">Where the animal stands; its height is set from the ground.</param>
        /// <param name="sentences">One sentence per locus, from the first; the alleles are registered with the origin "custom".</param>
        /// <returns>The new animal, as <see cref="Animal"/> makes it.</returns>
        public static Animal WithGenes(Species s, Vector3 p, params string[] sentences)
        {
            var w = s.World;
            var alleles = new Allele[s.Genes.Count];
            for (int i = 0; i < alleles.Length; i++)
            {
                var value = i < sentences.Length && sentences[i] != null ? AlleleValue.OfText(sentences[i]) : s.Genes[i].FounderPool[0].Value;
                alleles[i] = w.Alleles.Register(s.Genes[i], value, "custom");
            }
            return Animal(s, p, new Genome(s, alleles));
        }

        /// <summary>
        /// Puts a food item on the cell of a food layer that contains <paramref name="p"/>; nothing happens outside the layer's grid.
        /// </summary>
        /// <param name="layer">The food layer, from <c>w.Service&lt;FoodGrid&gt;()</c>.</param>
        /// <param name="p">A point in the cell.</param>
        public static void Food(FoodGrid layer, Vector3 p) => layer.SetItem(p, true);

        /// <summary>Puts cover on the cell of a cover layer that contains <paramref name="p"/>.</summary>
        /// <param name="cover">The cover layer, from <c>w.Service&lt;CoverLayer&gt;()</c>.</param>
        /// <param name="p">A point in the cell.</param>
        public static void Cover(CoverLayer cover, Vector3 p) => cover.SetCover(p, true);

        /// <summary>Adds a carcass of a species at a point, as if an animal had just been killed there.</summary>
        /// <param name="carcasses">The carcass system, from <c>w.Service&lt;CarcassSystem&gt;()</c>.</param>
        /// <param name="of">The dead animal's species: it decides who may eat the carcass (diets name "carcass:&lt;species&gt;").</param>
        /// <param name="p">Where the carcass lies.</param>
        /// <param name="killerId">The id of the animal that killed it, which may not eat from it (03 §3); -1 = none.</param>
        /// <param name="portions">The portions left (reference 2).</param>
        /// <param name="energy">The energy of each portion, in points (reference 30).</param>
        /// <param name="lifetime">The ticks before it rots away (reference 100).</param>
        /// <returns>The carcass, with its id and creation tick.</returns>
        public static Carcass Carcass(CarcassSystem carcasses, Species of, Vector3 p, int killerId = -1, int portions = 2,
                                      float energy = 30f, int lifetime = 100)
        {
            return carcasses.Add(new Carcass
            {
                Of = of,
                KillerId = killerId,
                PortionsLeft = portions,
                EnergyPerPortion = energy,
                LifetimeLeft = lifetime,
                Position = p,
            });
        }
    }
}
