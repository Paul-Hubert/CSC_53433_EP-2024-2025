using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FabricIKQuadruped : MonoBehaviour
{
    [Header("Chain Settings")]
    public int chainLength = 2; // Number of spaces between the bone positions.

    // Target (where we aim) and pole (to move along the multiple solutions for one target position).
    [Header("Target / Pole Settings")]
    public Transform target;
    public Transform pole;

    [Header("Solver Settings")]
    public int iterations = 10; // Solver iterations per update.
    public float delta = 0.001f; // Distance to the target when the solver stops.

    // Bones information (to be initialize).
    [Header("Bones Information")]
    public Transform[] bones;
    public Vector3[] bonesPositions;
    public float[] bonesLength;
    public float completeLength;

    // This case we just hardcode the bones of the legs to avoid iteraing.
    [Header("Bones Settings")]
    public Transform firstBone;
    public Vector3 firstBoneEulerAngleOffset;
    public Transform secondBone;
    public Vector3 secondBoneEulerAngleOffset;
    public Transform thirdBone;
    public Vector3 thirdBoneEulerAngleOffset;
    public bool alignThirdBoneWithTargetRotation = true;

    // Debug.
    private Vector3[] startingBoneDirectionToNext;
    private Quaternion[] startingBoneRotation;
    private Quaternion startingTargetRotation;
    private Quaternion startingRotationRoot;

    // Extra: Strength of going back to the start position.
    [Range(0, 1f)]
    public float snapBackStrength = 1f;

    // Awake is called when the script instance is being loaded.
    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        /*
         * If chain length is n, we have n+1 bones.
         * See bones as a set of a joint (bonePosition, represented as "O") and a length (bones Length, equal to chainLength).
         * The last bone (leaf bone, b3) does not have any length and still has a joint that can move/rotate.
         * 
         * b3   c2   b2    c1   b1    c0   b0
         * O ======== O ======== O ======== O
         */

        bones = new Transform[chainLength + 1];
        bonesPositions = new Vector3[chainLength + 1];
        bonesLength = new float[chainLength];
        completeLength = 0;

        // We also need the direction and rotation of each bone, along with the initial target rotation.
        startingBoneDirectionToNext = new Vector3[chainLength + 1];
        startingBoneRotation = new Quaternion[chainLength + 1];
        startingTargetRotation = target.rotation;

        // Initialize data.
        // We need to fill the arrays described above with the information for each bone.
        var current = thirdBone.transform;
        for (var i = bones.Length - 1; i >= 0; i--)
        {

            /*
             * Fill bones[i] with the transform of the current sphere where we are.
             * We also save the rotation in startingBoneRotation[i].
             */

            // START TODO ###################

            // Just a placeholder. Change with the correct transform!
            bones[i] = current;
            startingBoneRotation[i] = current.rotation;

            // END TODO ###################

            /*
             * Fill bonesLength[i] with the length between each bone.
             * We need to differenciate between two cases:
             * 
             * Leaf bones (last bone in the chain):They do not have any length. We are only saving here its direction to the target.
             * 
             * Mid-bones: You can access to Transform.position for each bone and use Vector3.magnitude to calculate the distance between them.
             * Update also completeLength to keep track of the total length.
             */

            if (i == bones.Length - 1)
            {
                startingBoneDirectionToNext[i] = target.position - current.position;
            }
            else
            {
                startingBoneDirectionToNext[i] = bones[i + 1].position - current.position;
                // START TODO ###################

                bonesLength[i] = startingBoneDirectionToNext[i].magnitude; 
                completeLength += bonesLength[i];

                // END TODO ###################
            }
            current = current.parent;
        }
    }

    // LateUpdate is called after all Update functions have been called.
    private void LateUpdate()
    {
        FastIK();
    }

    /// <summary>
    /// Fast IK with Forward/Backward pass.
    /// </summary>
    private void FastIK()
    {        
        // If no target is found.
        if (target == null)
        {
            Debug.Log("[INFO] No Target selected");
            return;
        }

        // If during run-time, I change chainLength, we re-initialize the bones.
        if (bonesLength.Length != chainLength)
        {
            Debug.Log("[INFO] Re-initializing bones");
            Init();
        }

        // First, we get the bonesPositions.
        // The reason why we keep the positions of the bones in a separated array, is because we won't manipulate directly the transforms of the bones in the chain.
        // Instead, we make a copy of the positions, work with them and at the end, set them back to the final chain.
        for (int i = 0; i < bones.Length; i++)
        {
            bonesPositions[i] = bones[i].position;
        }

        // Extra: For correcting the rotations.
        var rootRot = (bones[0].parent != null) ? bones[0].parent.rotation : Quaternion.identity;
        var rootRotDiff = rootRot * Quaternion.Inverse(startingRotationRoot);

        /* 
         * We first need to differenciate between two cases:
         * First, if the target is far away from the end-effector (target not reachable).
         * Meaning: Distance from bones[0] to target is larger than the length of the entire chain.
         * In that case, by default, it will remain straight pointing towards the target.
         * 
         *  Target        b3   c2   b2    c1   b1    c0   b0
         *     X----------O ======== O ======== O ======== O
         *     
         * Hints: The distance between the bones will always remain constant! 
         * The updated root bone position (bonesPositions[0]) should not change.
         * The other bones positions will be equal to the position of the previous bone plus the direction to the target * length of the bone.
         * Remember: All positions we are working with, are in world-space.
         */

        // START TODO ###################

        Vector3 target_direction = target.position - bones[0].position;
        if (target_direction.magnitude > completeLength)
        {
            target_direction.Normalize();
            float cur_length = 0;
            for (int i = 0; i < chainLength; i++)
            {
                cur_length += bonesLength[i];
                bonesPositions[i + 1] = bones[0].position + target_direction * cur_length;
            }

            // END TODO ###################

            // Extra: Rotation fixes for the bones of this skeleton.
            Vector3 towardPole = pole.position - firstBone.position;
            Vector3 towardTarget = target.position - firstBone.position;

            bones[0].rotation = Quaternion.LookRotation(towardTarget, towardPole);
            bones[0].localRotation *= Quaternion.Euler(firstBoneEulerAngleOffset);

            bones[1].rotation = Quaternion.LookRotation(towardTarget, towardPole);
            bones[1].localRotation *= Quaternion.Euler(secondBoneEulerAngleOffset);

            bones[2].rotation = target.rotation;
            bones[2].localRotation *= Quaternion.Euler(thirdBoneEulerAngleOffset);
        }

        /*
         * Second, if the target is closer, in such a way that the chain will need to move in order to reach it.
         * In this case, IK takes places. Fabric IK works with a Forward pass and a Backward pass:
         * 
         * Forward pass: We set the end-effector position where the target is, and we change the position of the bones towards the root, while keeping the distance between them constant.
         * Backward pass: Since we want the root bone to not move, we place it again in its original position and we again change the position of the bones, now towards the target, also keeping the distance between them constant.
         * 
         * This can be repeated by n iterations, and it is stopped when the end-effector is close enough to the target (measured by delta).
         *
         *      b3   c2   b2    c1   b1    c0   b0
         *      O ======== O ======== O ======== O
         *          Target
         *            X
         */

        else
        {
            // Extra: Going back to original position with certain strength.
            for (int i = 0; i < bonesPositions.Length - 1; i++)
            {
                bonesPositions[i + 1] = Vector3.Lerp(bonesPositions[i + 1], bonesPositions[i] + rootRotDiff * startingBoneDirectionToNext[i], snapBackStrength);
            }

            // We go though each iteration.
            for (int ite = 0; ite < iterations; ite++)
            {
                // Backward pass (we skip the root bone, as we do not need to change it because in the final chain it will keep its position).
                for (int i = bonesPositions.Length - 1; i > 0; i--)
                {
                    /*
                     * In this pass, if the index belongs to the end-effector, we set its position to the target position.
                     * For the other bones, we just set their positions equal to the position of the previous bone plus the distance * direction to that bone (as we did before).
                     */

                    // START TODO ###################

                    if (i == bonesPositions.Length - 1)
                        bonesPositions[i] = target.position;
                    else {
                        // Calculate the direction from the current bone to the next bone
                        Vector3 direction = (bonesPositions[i] - bonesPositions[i+1]).normalized;

                        // Calculate the new position of the current bone
                        bonesPositions[i] = bonesPositions[i + 1] + direction * bonesLength[i];
                    }

                    // END TODO ###################
                }

                // Forward pass.
                for (int i = 1; i < bonesPositions.Length; i++)
                {
                    /*
                     * Now, we do the same in reverse. We skip the root as it was not modified during the backward pass, and it will keep the original position.
                     */

                    // START TODO ###################

                    Vector3 direction = bonesPositions[i] - bonesPositions[i - 1];
                    bonesPositions[i] = bonesPositions[i - 1] + direction.normalized * bonesLength[i - 1];

                    // END TODO ###################

                }

                // Last check: Is the end-effector close enough to the target?
                if ((bonesPositions[bonesPositions.Length - 1] - target.position).sqrMagnitude >= delta * delta)
                {
                    break;
                }
            }

            // A pole is used to move between the multiple solutions of one configuration.
            // A virtual plane is created in the bone previous to the one being modified. The bone position, along with the pole, are projected on it.
            // Then, we rotate the bone in a way that its distance to the pole is the minimal.
            if (pole != null)
            {
                for (int i = 1; i < bonesPositions.Length - 1; i++)
                {
                    var plane = new Plane(bonesPositions[i + 1] - bonesPositions[i - 1], bonesPositions[i - 1]);
                    var projectedPole = plane.ClosestPointOnPlane(pole.position);
                    var projectedBone = plane.ClosestPointOnPlane(bonesPositions[i]);
                    var angle = Vector3.SignedAngle(projectedBone - bonesPositions[i - 1], projectedPole - bonesPositions[i - 1], plane.normal);
                    bonesPositions[i] = Quaternion.AngleAxis(angle, plane.normal) * (bonesPositions[i] - bonesPositions[i - 1]) + bonesPositions[i - 1];
                }
            }

            // This is used to correct as well the rotation of each bone.
            for (int i = 0; i < bonesPositions.Length; i++)
            {
                if (i == bonesPositions.Length - 1)
                    bones[i].rotation = target.rotation * Quaternion.Inverse(startingTargetRotation) * startingBoneRotation[i];
                else
                    bones[i].rotation = Quaternion.FromToRotation(startingBoneDirectionToNext[i], bonesPositions[i + 1] - bonesPositions[i]) * startingBoneRotation[i];
                bones[i].position = bonesPositions[i];
            }

        }

        // Finally, we set back bonesPositions to the actual transforms (positions) of the bones in the chain.
        for (int i = 0; i < bonesPositions.Length; i++)
        {
            bones[i].position = bonesPositions[i];
        }
    }
}
