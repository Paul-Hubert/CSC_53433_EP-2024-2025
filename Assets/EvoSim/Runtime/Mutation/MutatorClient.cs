using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Sends prompts to the mutator model: Ollama or an OpenAI-compatible server in EvoSim.Http, a fake in tests.
    /// It never sees anything but the prompts (CORE-07).
    /// </summary>
    /// <remarks>
    /// <para>A world service: place one under the World next to the <see cref="MutatorService"/>, which finds it at the
    /// start of the run. The service calls <see cref="Send"/> on the main thread, with at most one batch out at a time.</para>
    /// <para>What a client must keep: answer every request, at its index, with <see cref="MutatorReply.SetAnswer"/> or
    /// <see cref="MutatorReply.Fail"/>, then call <see cref="MutatorReply.Complete"/> once (or make the reply with a pending
    /// that tells when it is complete); send the prompt alone, with the request's model, seed and temperature (MUT-14);
    /// never touch Unity
    /// objects from another thread. The answers may come at once or later: <c>HttpMutatorClient</c> runs its calls on the
    /// thread pool and completes the reply when all are in.</para>
    /// </remarks>
    /// <example>
    /// A client that answers at once, simplified from the test client <c>EvoSim.Testing.FakeMutator</c>:
    /// <code>
    /// public class QuicklyMutator : MutatorClient
    /// {
    ///     public override string ModelIdentity => "fake-mutator";
    ///
    ///     public override MutatorReply Send(IReadOnlyList&lt;MutatorRequest&gt; requests)
    ///     {
    ///         var reply = new MutatorReply(requests.Count);
    ///         for (int i = 0; i &lt; requests.Count; i++)
    ///             reply.SetAnswer(i, FakeMutator.SentenceOf(requests[i].Prompt).TrimEnd('.') + " quickly.");
    ///         reply.ModelCalls = requests.Count;
    ///         reply.Complete();
    ///         return reply;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class MutatorClient : WorldService
    {
        /// <summary>Send a batch; the reply fills later (or at once).</summary>
        /// <param name="requests">The prompts of one round, in job order: the answer to request i goes to index i.</param>
        /// <returns>The reply, holding an answer or an error per request; its pending is done when all are in.</returns>
        public abstract MutatorReply Send(IReadOnlyList<MutatorRequest> requests);

        /// <summary>
        /// The model name and digest, recorded in the run info (GENE-12); empty by default. Mutant alleles record the
        /// service's model name.
        /// </summary>
        public virtual string ModelIdentity => "";
    }
}
