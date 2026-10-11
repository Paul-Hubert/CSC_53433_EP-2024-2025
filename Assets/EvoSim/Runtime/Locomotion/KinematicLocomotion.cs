using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The reference locomotion (MOVE-04): straight lines, no physics. Moves by the smallest of speed, stamina and the
    /// intent's maximum distance; slides along borders and obstacles; if stuck, takes a random sidestep.
    /// </summary>
    /// <remarks>
    /// <para>Teaching-path exercise 11. <see cref="Move"/> works in five steps:</para>
    /// <list type="number">
    /// <item><description>a stay moves nothing and returns 0;</description></item>
    /// <item><description>the distance is the smallest of the speed trait (<c>speed.run</c> when running,
    /// <c>speed.walk</c> otherwise), the stamina budget and the intent's maximum distance;</description></item>
    /// <item><description><see cref="Ground.SlideTo"/> goes that far in a straight line, sliding along borders and
    /// obstacles;</description></item>
    /// <item><description>if that made no progress (less than 1 mm), <see cref="Ground.SideStep"/> takes a step of the
    /// same length in a random direction (up to 8 tries, else it stays), drawn from the species' <c>actions</c>
    /// stream;</description></item>
    /// <item><description>it sets the animal's position and returns the distance moved.</description></item>
    /// </list>
    /// <para>Without a ground (some test worlds) it moves straight, with no check.</para>
    /// </remarks>
    public class KinematicLocomotion : Locomotion
    {
        /// <summary>
        /// Moves in a straight line by the smallest of speed, stamina and the intent's maximum distance; slides along
        /// obstacles and sidesteps when stuck (MOVE-04).
        /// </summary>
        /// <inheritdoc/>
        public override float Move(Animal a, Intent intent, float staminaBudget, MoveContext c)
        {
            if (intent.IsStay) return 0f;
            float speed = a.Trait(intent.Run ? RunSpeed : WalkSpeed);
            float d = Mathf.Min(speed, Mathf.Min(staminaBudget, intent.MaxDistance));
            if (d <= 0f) return 0f;
            var from = a.Position;
            var ground = c.Ground;
            Vector3 reached = ground != null ? ground.SlideTo(from, from + intent.Direction * d) : from + intent.Direction * d;
            if (ground != null && c.Distance(from, reached) < Units.Epsilon)                    // stuck: a random sidestep
                reached = ground.SideStep(from, d, c.Stream(Species, "actions"));
            float moved = c.Distance(from, reached);
            a.Position = reached;
            return moved;
        }
    }
}
