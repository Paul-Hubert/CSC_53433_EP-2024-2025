using UnityEngine;

namespace EvoSim.Testing
{
    /// <summary>Puts animals, items, carcasses and cover at exact positions (30 §2). Records no event.</summary>
    public static class Place
    {
        /// <summary>A position on the horizontal plane.</summary>
        public static Vector3 At(float x, float z) => new Vector3(x, 0f, z);

        /// <summary>An animal of a species at a point, with each gene's first founder allele (or a given genome).</summary>
        public static Animal Animal(Species s, Vector3 p, Genome genome = null, float heading = 0f, string origin = "founder")
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
            var a = s.CreateAnimal(origin, p, heading, genome, 0, null);
            s.Add(a);
            return a;
        }

        /// <summary>An animal of a species with these text sentences, locus by locus (the rest from the pools).</summary>
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

        /// <summary>A food item in the cell containing p; returns the layer's item there.</summary>
        public static void Food(FoodGrid layer, Vector3 p) => layer.SetItem(p, true);

        /// <summary>Cover on the cell containing p.</summary>
        public static void Cover(CoverLayer cover, Vector3 p) => cover.SetCover(p, true);

        /// <summary>A carcass of a species at p, as if killed by killerId.</summary>
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
