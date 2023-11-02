using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
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
        //MoveLeg();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
