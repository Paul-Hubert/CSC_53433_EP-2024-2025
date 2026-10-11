namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §2: how far the nearest water is, in bands, with "here" at the shore (a query the ground offers).</summary>
    /// <remarks>
    /// <para><b>What it adds.</b> A sense labelled "Water" whose tokens are here, one per distance band, and none:
    /// "Water: 1-4 meters away." in V1, "I am at the water's edge." when within 0.5 m of the water. Only a ground that knows
    /// water answers (<see cref="Ground.NearestWater"/>: the terrain ground, or a test's pond); on a flat ground it always
    /// says none.</para>
    /// <para><b>Mechanism.</b> A <see cref="BandedSense"/>: the base class builds the tokens from the bands and writes the
    /// texts; the subclass only turns on "here" (<see cref="HasHere"/>) and maps a distance to a token in <see cref="Read"/>.</para>
    /// <para><b>In a scene.</b> Add it on a new child GameObject among the species' senses: its place among them is its
    /// place in the situation text. It multiplies the observation space by its number of tokens (6 with the reference bands).</para>
    /// </remarks>
    public class NearestWaterSense : BandedSense
    {
        /// <summary>True: this sense has a "here" token, for an animal at the shore.</summary>
        protected override bool HasHere => true;

        /// <summary>"Water".</summary>
        protected override string DefaultLabel => "Water";

        /// <summary>"I am at the water's edge."</summary>
        /// <inheritdoc/>
        protected override string HereSentence() => "I am at the water's edge.";

        /// <summary>
        /// The band of the distance to the nearest water within the animal's vision; "here" within 0.5 m; none when no water
        /// is in sight or the world has no ground.
        /// </summary>
        /// <inheritdoc/>
        public override int Read(Animal a, SenseContext s)
        {
            if (s.Ground == null) return NoneToken;
            float vision = s.Vision(a);
            var water = s.Ground.NearestWater(a.Position, vision);
            return water == null ? NoneToken : TokenFor(water.Value.Distance, vision, here: water.Value.Distance <= 0.5f);
        }
    }
}
