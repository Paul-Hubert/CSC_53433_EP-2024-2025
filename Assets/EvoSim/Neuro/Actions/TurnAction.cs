using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// <c>left</c> or <c>right</c> (plan M16 D1, §5.5): one step toward the heading turned by <see cref="Angle"/> degrees,
    /// at walk, run or slow speed. The act phase then turns the heading toward the move, so a run of turns steers in a
    /// circle (radius about 2.9 m at walk speed and 20°).
    /// </summary>
    public class TurnAction : NeuralAction
    {
        [SerializeField, Tooltip("On: turn right (clockwise, headings run clockwise from +z). Off: turn left.")]
        bool right;
        [SerializeField, Range(0f, 180f), Tooltip("Degrees turned per tick (plan default 20).")]
        float angle = 20f;
        [SerializeField, Tooltip("The speed while turning: walk (default), run, or a fraction of walk.")]
        NeuralSpeed speed = NeuralSpeed.Walk;
        [SerializeField, Range(0.05f, 1f), Tooltip("The share of the walk speed at the Slow speed (plan default 0.5).")]
        float slowFraction = 0.5f;

        /// <summary>True for <c>right</c>, false for <c>left</c>.</summary>
        public bool Right => right;
        /// <summary>Degrees turned per tick.</summary>
        public float Angle => angle;
        /// <summary>The speed while turning.</summary>
        public NeuralSpeed Speed => speed;

        /// <inheritdoc/>
        protected override string DefaultDescription => right ? "turn right while moving" : "turn left while moving";

        /// <summary>Sets the turn from code (templates, tests), as the Inspector does.</summary>
        /// <param name="turnRight">True for right, false for left.</param>
        /// <param name="degrees">Degrees per tick.</param>
        /// <param name="moveSpeed">The speed while turning.</param>
        /// <param name="slow">The share of the walk speed at Slow.</param>
        /// <returns>This action.</returns>
        public TurnAction Configure(bool turnRight, float degrees = 20f, NeuralSpeed moveSpeed = NeuralSpeed.Walk, float slow = 0.5f)
        {
            right = turnRight;
            angle = Mathf.Clamp(degrees, 0f, 180f);
            speed = moveSpeed;
            slowFraction = Mathf.Clamp(slow, 0.05f, 1f);
            return this;
        }

        /// <inheritdoc/>
        protected override void Move(Animal a, ActContext c)
        {
            bool run = speed == NeuralSpeed.Run;
            float d = SpeedOf(a, run) * (speed == NeuralSpeed.Slow ? slowFraction : 1f);
            Step(a, c, right ? angle : -angle, d, run);
        }
    }
}
