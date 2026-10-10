using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Sends prompts to the mutator model: Ollama or an OpenAI-compatible server in EvoSim.Http, a fake in tests.
    /// It never sees anything but the prompts (CORE-07).
    /// </summary>
    public abstract class MutatorClient : WorldService
    {
        /// <summary>Send a batch; the reply fills later (or at once).</summary>
        public abstract MutatorReply Send(IReadOnlyList<MutatorRequest> requests);

        /// <summary>The model name and digest, recorded with alleles and run info (GENE-12).</summary>
        public virtual string ModelIdentity => "";
    }
}
