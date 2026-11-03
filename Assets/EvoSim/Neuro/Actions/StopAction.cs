using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// <c>stop</c> (plan M16 D1, §5.5): stay still, by default resting (the rest cost, stamina recovers), and still graze or
    /// strike what is within reach. It is not self-directed: it stays, as rest does (ACT-04).
    /// </summary>
    public class StopAction : NeuralAction
    {
        [SerializeField, Tooltip("On: a still tick costs the rest cost and stamina recovers, as rest does (plan default on).")]
        bool rests = true;

        /// <inheritdoc/>
        public override bool Rests => rests;
        /// <inheritdoc/>
        public override bool SelfDirected => false;

        /// <inheritdoc/>
        protected override string DefaultDescription => "stop and stay where you are";

        /// <summary>Sets resting from code (templates, tests), as the Inspector does.</summary>
        /// <param name="resting">True to rest.</param>
        /// <returns>This action.</returns>
        public StopAction Configure(bool resting)
        {
            rests = resting;
            return this;
        }

        /// <inheritdoc/>
        protected override void Move(Animal a, ActContext c) => c.Stay();
    }
}
