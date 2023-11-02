using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RandomInstanceBrush : InstanceBrush
{

    public override void draw(float x, float z)
    {
        System.Random rand = new System.Random();

        float rdx = (float)((rand.NextDouble() - 0.5) * 2 * radius);
        float rdz = (float)((rand.NextDouble() - 0.5) * 2 * radius);
        spawnObject(x + rdx, z + rdz);
    }
}