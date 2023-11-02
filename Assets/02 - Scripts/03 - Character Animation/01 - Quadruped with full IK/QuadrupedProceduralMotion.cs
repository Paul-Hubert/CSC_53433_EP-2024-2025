using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;

public class QuadrupedProceduralMotion : MonoBehaviour
{
    [Header("Goal")]
    public Transform goal; // The character will move towards this goal.

    // Settings relative to the root motion.
    [Header("Root Motion Settings")]
    public float turnSpeed;
    public float moveSpeed;
    public float moveBackwardFactor = 0.5f;
    [Space(20)]
    public float turnAcceleration;
    public float moveAcceleration;
    [Space(20)]
    public float minDistToGoal;
    public float maxDistToGoal;
    SmoothDamp.Vector3 currentVelocity;
    SmoothDamp.Float currentAngularVelocity;

    // Settings relative to body adaptation to the terrain.
    [Header("Body Adaptation Settings")]
    public Transform hips;
    public float heightAcceleration;
    public Vector3 constantHipsPosition;
    public Vector3 constantHipsRotation;
    public Transform groundChecker;
    public Vector3 normalTerrain;
    public float distanceHit;
    public Vector3 posHit;

    // Settings relative to the tail.
    [Header("Tail Settings")]
    public Transform[] tailBones;
    public float tailTurnMultiplier;
    public float tailTurnSpeed;
    Quaternion[] tailHomeLocalRotation;
    SmoothDamp.Float tailRotation;

    // Settings relative to the head.
    [Header("Head Settings")]
    public Transform headBone; 
    public float speedHead = 1f;
    public bool headDebug = false;
    public Vector3 goalWorldLookDir;
    public Vector3 goalLocalLookDir;
    public float angleHeadLimit = 90f;

    // Foot Steppers for each leg.
    [Header("Controllers for the steps")]
    public FootStepper frontLeftFoot;
    public FootStepper frontRightFoot;
    public FootStepper backLeftFoot;
    public FootStepper backRightFoot;

    // Awake is called when the script instance is being loaded.
    void Awake()
    {
        StartCoroutine(Gait());
        TailInitialize();
        BodyInitialize();
    }

    // Update is called every frame, if the MonoBehaviour is enabled.
    private void Update()
    {        
        RootMotion();
    }

    // LateUpdate is called after all Update functions have been called.
    private void LateUpdate()
    {
        TrackHead();
        TailUpdate();
        RootAdaptation();
    }

    #region Root Motion

    /// <summary>
    /// Method used to move the character towards the goal.
    /// </summary>
    private void RootMotion()
    {
        // Get the vector towards the goal and projectected it on the plane defined by the normal transform.up.
        Vector3 towardGoal = goal.position - transform.position;
        Vector3 towardGoalProjected = Vector3.ProjectOnPlane(towardGoal, transform.up);

        // Angles between the forward direction and the direction to the goal. 
        var angToGoal = Vector3.SignedAngle(transform.forward, towardGoalProjected, transform.up);

        // Get a perfectange of the turnSpeed to apply based on how far the goal is and its sign.
        float targetAngularVelocity = Mathf.Sign(angToGoal) * Mathf.InverseLerp(5f, 30f, Mathf.Abs(angToGoal)) * turnSpeed;

        // Step() smoothing function.
        currentAngularVelocity.Step(targetAngularVelocity, turnAcceleration);

        // Initialize zero root velocity.
        Vector3 targetVelocity = Vector3.zero;

        // Estimating targetVelocity.
        // Only translate if we are close facing to the goal.
        if (Mathf.Abs(angToGoal) < 90)
        {
            var distToGoal = towardGoalProjected.magnitude;

            // Move towards target if we are too far.
            if (distToGoal > maxDistToGoal)
            {
                targetVelocity = moveSpeed * towardGoalProjected.normalized;
            }
            // Move away from target if we are too close.
            else if (distToGoal < minDistToGoal)
            {
                targetVelocity = moveSpeed * -towardGoalProjected.normalized * moveBackwardFactor;
            }

            // Limit velocity progressively as we approach max angular velocity.
            targetVelocity *= Mathf.InverseLerp(turnSpeed, turnSpeed * 0.2f, Mathf.Abs(currentAngularVelocity));
        }

        // Apply targetVelocity using Step() and applying.
        currentVelocity.Step(targetVelocity, moveAcceleration);
        transform.position += currentVelocity.currentValue * Time.deltaTime;

        // Apply rotation.
        transform.rotation *= Quaternion.AngleAxis(Time.deltaTime * currentAngularVelocity, transform.up);
    }

    #endregion

    #region Root Adaptation

    /// <summary>
    /// Saves the initial position and rotation of the hips.
    /// </summary>
    private void BodyInitialize()
    {
        constantHipsPosition = new Vector3(hips.position.x, hips.position.y, hips.position.z);
        constantHipsRotation = new Vector3(hips.rotation.x, hips.rotation.y, hips.rotation.z);
    }

    /// <summary>
    /// In LateUpdate, after moving the root body to the target, we perform the adaptation on a top-layer to place the animal parallel to the ground.
    /// </summary>
    private void RootAdaptation()
    {
        // Origin of the ray.
        Vector3 raycastOrigin = groundChecker.position;

        // The ray information gives you where you hit and the normal of the terrain in that location.
        if (Physics.Raycast(raycastOrigin, -transform.up, out RaycastHit hit, Mathf.Infinity))
        {   
            posHit = hit.point;
            distanceHit = hit.distance;
            normalTerrain = hit.normal;

            // Rotate the root body to be parallel to the ground
            //Quaternion desiredRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            //transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * heightAcceleration);

            UnityEngine.Debug.Log(hips.position);

        }

        Vector3 desiredHipPosition = new Vector3(transform.position.x, hit.point.y + 0.95f, transform.position.z);
        hips.position = desiredHipPosition;

        Quaternion desiredRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
        hips.rotation = Quaternion.Slerp(hips.rotation, desiredRotation, Time.deltaTime * heightAcceleration);

        /*
         * In this layer, we need to refine the position and rotation of the hips based on the ground. Without this part, the animal would not lift its root body when walking on high terrains.
         * First, try to use the hit information to modify hips.position and move it up when you are in a higher ground.
         * Then, use also this information (normalTerrain) to rotate the root body and place it parallel to the ground. You can use Quaternion.FromToRotation() for that.
         * When you have the angle that you want to have in your root body, you can place it directly, or use some interpolation technique to go smoothly to that value, in order to have less drastical movements.
         */

        // START TODO ###################/
        // Calculate the desired hip position based on the hit point and distance
        UnityEngine.Debug.DrawRay(raycastOrigin, -transform.up, Color.red);



        // END TODO ###################
    }

    #endregion

    #region Tail Motion

    /// <summary>
    /// Initialize all the bones of the tail.
    /// </summary>
    void TailInitialize()
    {
        // Store the default rotation of the tail bones.
        tailHomeLocalRotation = new Quaternion[tailBones.Length];
        for (int i = 0; i < tailHomeLocalRotation.Length; i++)
        {
            tailHomeLocalRotation[i] = tailBones[i].localRotation;
        }
    }


    /// <summary>
    /// Rotate the tail as a function of the angular velocity of the root body.
    /// </summary>
    void TailUpdate()
    {
        // Tail rotates opposite to the current angular velocity to have some inertia effect.
        tailRotation.Step(-currentAngularVelocity / turnSpeed * tailTurnMultiplier, tailTurnSpeed);

        for (int i = 0; i < tailBones.Length; i++)
        {
            // Convert to Euler and apply the rotation my multiplying to the current local quaternion.
            Quaternion rotation = Quaternion.Euler(0, tailRotation, 0);
            tailBones[i].localRotation = rotation * tailHomeLocalRotation[i];
        }
    }

    #endregion

    #region Head Motion

    /// <summary>
    /// This method is used to rotate the head of your character in the direction of the goal.
    /// </summary>
    private void TrackHead()
    {
        // We save the local rotation of the head with respect to his parent, and reset it to the identity.
        Quaternion currentLocalRotation = headBone.localRotation;
        headBone.localRotation = Quaternion.identity;

        /* 
         * First, we need to get goalWorldLookDir: the position of the goal with respect to the head transform (you can use Debug.DrawRay() to debug it).
         * Use InverseTransformDirection() and headbone.parent to transform it with respect to the parent of the head (goalLocalLookDir).
         * Use RotateTowards() to have Vector3.forward always looking to goalLocalLookDir.
         * Finally, define targetLocalRotation: The target local angle for your head. The forward axis (along the bone) will need to point to the object. To do this, you can use Quaternion.LookRotation().
         */

        // START TODO ###################

        goalWorldLookDir = goal.position - headBone.position;
        goalLocalLookDir = headBone.parent.InverseTransformDirection(goalWorldLookDir);
        Vector3 goalForward = Vector3.RotateTowards(headBone.forward, goalLocalLookDir, angleHeadLimit * Mathf.Deg2Rad, 0);

        Quaternion targetLocalRotation = Quaternion.LookRotation(goalLocalLookDir, Vector3.up);
        // END TODO ###################

        /* 
         * Interpolation for damping. 
         * We do not apply directly the quaternion to the head (that would cause a quite robotic movement).
         * Instead, we just use interpolation to apply it over time, to provide a more natural effect.
         */

        headBone.localRotation = Quaternion.Slerp(currentLocalRotation, targetLocalRotation, 1 - Mathf.Exp(-speedHead * Time.deltaTime));
    }

    #endregion

    #region Gait

    /// <summary>
    /// Coroutine that describes how the gait of the character will be.
    /// It calls the MoveLeg() function for each leg in a defined moment. In this case, we want the diagonal legs pair move simultaneiously, while the other pair of diagonal legs stays in place.
    /// This is necessary as we do not have any kinematic animation - our character moves purely with IK and procedural functions.
    /// Other complex behaviors might be created.
    /// </summary>
    /// <returns></returns>
    IEnumerator Gait()
    {
        while (true)
        {
            do
            {
                frontLeftFoot.MoveLeg();
                backRightFoot.MoveLeg();

                // Wait a frame
                yield return null;

            } while (backRightFoot.Moving || frontLeftFoot.Moving);

            // Do the same thing for the other diagonal pair
            do
            {
                frontRightFoot.MoveLeg();
                backLeftFoot.MoveLeg();

                // Wait a frame
                yield return null;

            } while (backLeftFoot.Moving || frontRightFoot.Moving);
        }
    }

    #endregion
}
