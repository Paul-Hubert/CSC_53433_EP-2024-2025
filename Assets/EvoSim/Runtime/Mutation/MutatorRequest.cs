namespace EvoSim
{
    /// <summary>One prompt for the mutator model, with its seed and temperature (MUT-11, MUT-14).</summary>
    public readonly struct MutatorRequest
    {
        public readonly string Prompt;
        public readonly long Seed;
        public readonly float Temperature;
        public readonly string Model;

        public MutatorRequest(string prompt, long seed, float temperature, string model)
        {
            Prompt = prompt; Seed = seed; Temperature = temperature; Model = model;
        }

        /// <summary>The cache key: model, prompt, seed, temperature (MUT-14).</summary>
        public string CacheKey => Hashing.Sha256Hex(Model + "\n" + Prompt + "\n" + Seed + "\n" +
                                                    Temperature.ToString("R", System.Globalization.CultureInfo.InvariantCulture), 32);
    }
}
