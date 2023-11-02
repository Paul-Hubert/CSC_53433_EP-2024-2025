using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using UnityEngine;

public class FootStepper : MonoBehaviour
{
    [Header("Stepper Settings")]
    public Transform homeTransform; // The position and rotation from which we want to stay in range (represented as the blue chip).
    public float distanceThreshold = 0.4f; // If we exceed this distance threshold, we come back to the home position.
    public float angleThreshold = 135f; // If we exceed this rotation threshold, we come back to the home rotation.
    public float moveDuration; // The time it takes a step to complete.
    [Range(0, 1)]
    public float stepOvershootFraction; // To add some variability, we add a fraction to overshoot the target.

    [Header("Raycast Settings")]
    public LayerMask groundRaycastMask = ~0; // Ground layer that you need to detect by raycasting.
    public float heightOffset; // Necessary when foot joints aren't exactly at the base of the foot geometry.

    // Flag to define when a leg is moving.
    public bool Moving;

    // Awake is called when the script instance is being loaded.
    void Awake()
    {
        // We put the steppers at the top of the hierarchy, to avoid other influences from the parent transforms and to see them better.
        transform.SetParent(null);

        // Adapt the legs just after starting the script.
        MoveLeg();
    }

    /// <summary>
    /// Method that set the preconditions for moving a leg and exectutes the coroutine process.
    /// </summary>
    public void MoveLeg()
    {
        // If we are already moving, don't start another move.
        if (Moving)
        {
            return;
        }

        /*
         * First, we want to calculate the distance from the GameObject where this script is attached (target, red sphere) to the home position of the respective leg (blue chip).
         * We also calculate the quaternion between the current rotation and the home rotation.
         * If such distance is larger than the threshold step, OR the angle difference is larger than the angle threshold, we call the coroutine to move the leg.
         */

        // START TODO ###################
        float distFromHome = (homeTransform.position - transform.position).magnitude;
        float angleFromHome = Quaternion.Angle(homeTransform.rotation, homeTransform.rotation);     
        // Change condition!
        if (distFromHome > distanceThreshold || angleFromHome > angleThreshold)
        {
            // END TODO ###################
            UnityEngine.Debug.Log("Oi");
            // Get the grounded location for the feet. It can return false - in that case, it won't move.
            // This method modifies the values by reference, which are used later.
            if (GetGroundedEndPosition(out Vector3 endPos, out Vector3 endNormal))
            {
                // The foot rotation will be defined by:
                // Forward direction: Forward normalized vector of home, but projected on the same plane where the grounded position was detected.
                // Upward direction: Normal of the plane where the grounded position was detected.
                Quaternion endRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(homeTransform.forward, endNormal), endNormal);

                // Start MoveFoot coroutine with the target position, rotation and duration of the movement.
                StartCoroutine(MoveFoot(endPos, endRot, moveDuration));
            }

            else
            {
                Quaternion endRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(homeTransform.forward, endNormal), endNormal);
                StartCoroutine(MoveFoot(endPos, endRot, moveDuration));
            }
        }


    }

    /// <summary>
    /// Method that saves the ground location information where the foot should be located.
    /// Returns false if no grounded position was found.
    /// </summary>
    /// <param name="endPos"></param>
    /// <param name="endNormal"></param>
    /// <returns></returns>
    bool GetGroundedEndPosition(out Vector3 endPos, out Vector3 endNormal)
    {
        /*
         * First, we calculate the normalized vector that goes from the current position to the home position. It will describe the direction of our overshoot (offset) vector.
         * Then, you can create a factor by taking the distance threshold and modifying it by a fraction [0, 1].
         * Finally, you can multiply such new factor by the normalized distance previously calculated.
         * The result is a vector in the direction of the movement when the foot is coming back to the home position, with a small magnitude.
         * Summing such vector to the real home position, you will get a new home position slighly moved.
         */

        Vector3 towardsHome = (homeTransform.position - transform.position).normalized;
        float overshootDistance = distanceThreshold * stepOvershootFraction;
        Vector3 overshootVector = towardsHome * overshootDistance;

        /*
         * Now, we build a raycast system. Check: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
         * The ray will detect a collision with the terrain and use such information to place the foot accordingly on the ground.
         * First, you create the origin (Vector3) of your ray, which will be in the home position (remember to add the overshoot vector calculated before).
         * You can also add some vertical y displacement to let the ray having more space when going down and avoiding undesired collisions.
         * Then, throw the ray downwards, and save the position and normal vector of the hit in "endPos" and "endNormal" respectively.
         * If there is a collision, return true. Otherwise, you can return false.
         */

        // START TODO ###################

        Vector3 raycastOrigin = homeTransform.position;

        RaycastHit hit;
        if (Physics.Raycast(raycastOrigin, Vector3.down, out hit))
        {
            endPos = hit.point + 2*overshootVector;
            endNormal = hit.normal;
            return true;
        }

        endPos = homeTransform.position + overshootVector;
        endNormal = Vector3.zero;
        return false;

    }

    /// <summary>
    /// Coroutine that performs the motion part.
    /// </summary>
    /// <param name="endPos"></param>
    /// <param name="endRot"></param>
    /// <param name="moveTime"></param>
    /// <returns></returns>
    IEnumerator MoveFoot(Vector3 endPos, Quaternion endRot, float moveTime)
    {
        // We are in the coroutine, meaning that a moving action is taking place.
        Moving = true;

        // Store the initial, current position and rotation for the interpolation.
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // Apply the height offset to the home position, where we will move towards.
        // You can change the heightOffset to see the effect. It just changes the vertical component to modify where the IK target is.
        endPos += homeTransform.up * heightOffset;

        // Initialize the time.
        float timeElapsed = 0;

        do
        {
            /*
             * First, you need to keep the record of the total elapsed time.
             * Then, you can normalized by using the moveTime variable.
             */

            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveTime;

            // We could also apply some animation curve(e.g.Easing.EaseInOutCubic) to make the foot go smoother.
            normalizedTime = Easing.EaseInOutCubic(normalizedTime);

            /*
             * We know startPos and endPos. We could interpolate directly from the starting point to the end point, but we have a problem: The movement would be straight and flat on the terrain. Try it out!
             * transform.position = Vector3.Lerp(startPoint, endPoint, normalizedTime);
             * We need to find a way to guide the foot from the ground to a lifted position, and then put it back on the ground. 
             * Any idea? Just a tip: https://en.wikipedia.org/wiki/B%C3%A9zier_curve#Constructing_B.C3.A9zier_curves
             */

            // START TODO ###################
            Vector3 controlPoint1 = startPos + (Vector3.up);
            //transform.position = Vector3.Lerp(startPos, endPos, normalizedTime);
            Vector3 controlPoint2 = endPos + (Vector3.up);
            transform.position = BezierCurve(startPos, controlPoint1, controlPoint2, endPos, normalizedTime);
            // transform.position = ...

            // END TODO ###################

            /*
             * You just need now to also move from the starting rotation and the ending rotation.
             */

            // START TODO ###################

            transform.rotation = Quaternion.Lerp(startRot, endRot, normalizedTime);

            // END TODO ###################

            // Wait for one frame
            yield return null;
        }
        while (timeElapsed < moveDuration);

        // Motion has finished.
        Moving = false;
    }

    private Vector3 BezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return Mathf.Pow(1 - t, 3) * p0 +
               3 * Mathf.Pow(1 - t, 2) * t * p1 +
               3 * (1 - t) * Mathf.Pow(t, 2) * p2 +
               Mathf.Pow(t, 3) * p3;
    }

    /// <summary>
    /// Function for better visualization.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (Moving)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, homeTransform.position);
        Gizmos.DrawWireCube(homeTransform.position, Vector3.one * 0.1f);
    }
}
