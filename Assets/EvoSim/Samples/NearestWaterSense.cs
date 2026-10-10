namespace EvoSim.Samples
{
    /// <summary>Recipe 22 §2: how far the nearest water is, in bands, with "here" at the shore (a query the ground offers).</summary>
    public class NearestWaterSense : BandedSense
    {
        protected override bool HasHere => true;
        protected override string DefaultLabel => "Water";
        protected override string HereSentence() => "I am at the water's edge.";

        public override int Read(Animal a, SenseContext s)
        {
            if (s.Ground == null) return NoneToken;
            float vision = s.Vision(a);
            var water = s.Ground.NearestWater(a.Position, vision);
            return water == null ? NoneToken : TokenFor(water.Value.Distance, vision, here: water.Value.Distance <= 0.5f);
        }
    }
}
