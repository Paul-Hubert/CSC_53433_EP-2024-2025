using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary><c>walk</c> or <c>run</c> (plan M16 D1, §5.5): one step straight along the heading; running costs stamina.</summary>
    public class ForwardAction : NeuralAction
    {
        [SerializeField, Tooltip("On: run at the run speed (costs stamina). Off: walk.")]
        bool run;

        /// <summary>True for <c>run</c>, false for <c>walk</c>.</summary>
        public bool Run => run;

        /// <inheritdoc/>
        protected override string DefaultDescription => run ? "run straight ahead" : "walk straight ahead";

        /// <summary>Sets walk or run from code (templates, tests), as the Inspector does.</summary>
        /// <param name="running">True to run.</param>
        /// <returns>This action.</returns>
        public ForwardAction Configure(bool running)
        {
            run = running;
            return this;
        }

        /// <inheritdoc/>
        protected override void Move(Animal a, ActContext c) => Step(a, c, 0f, SpeedOf(a, run), run);
    }
}
