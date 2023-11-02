using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RandomAltitudeBrush : InstanceBrush
{

    public float hMax = 50;
    public float MaxSteepness = 100;
    public override void draw(float x, float z)
    {
        System.Random rand = new System.Random();
        float rdx = (float)((rand.NextDouble() - 0.5) * 2 * radius);
        float rdz = (float)((rand.NextDouble() - 0.5) * 2 * radius);
        float h = terrain.get(x + rdx, z + rdz);
        float steepness = terrain.getSteepness(x + rdx, z + rdz);

        float hChoice = 1f / (1f + (float)Math.Exp(-h / hMax));
        float rdChoice = (float)rand.NextDouble();

        float scaler = 1f - h / hMax;
        if (hChoice < rdChoice & steepness < MaxSteepness)
        {
            spawnObjectScaled(x + rdx, z + rdz, scaler);
        }

    }
}