namespace EvoSim
{
    /// <summary>One prompt for the mutator model, with its seed and temperature (MUT-11, MUT-14).</summary>
    public readonly struct MutatorRequest
    {
        /// <summary>The whole prompt (MUT-11): context line, instruction, quoted sentence, reply line.</summary>
        public readonly string Prompt;
        /// <summary>The seed of the call, drawn at conception (MUT-30); 31 bits with the reference operator.</summary>
        public readonly long Seed;
        /// <summary>The sampling temperature (reference 1.2).</summary>
        public readonly float Temperature;
        /// <summary>The model's name, e.g. <c>gemma4:26b</c>.</summary>
        public readonly string Model;

        /// <summary>Makes a request.</summary>
        /// <param name="prompt">The whole prompt.</param>
        /// <param name="seed">The call's seed.</param>
        /// <param name="temperature">The sampling temperature.</param>
        /// <param name="model">The model's name.</param>
        public MutatorRequest(string prompt, long seed, float temperature, string model)
        {
            Prompt = prompt; Seed = seed; Temperature = temperature; Model = model;
        }

        /// <summary>The cache key: model, prompt, seed, temperature (MUT-14), as 32 hex digits of a SHA-256 hash.</summary>
        public string CacheKey => Hashing.Sha256Hex(Model + "\n" + Prompt + "\n" + Seed + "\n" +
                                                    Temperature.ToString("R", System.Globalization.CultureInfo.InvariantCulture), 32);
    }
}
