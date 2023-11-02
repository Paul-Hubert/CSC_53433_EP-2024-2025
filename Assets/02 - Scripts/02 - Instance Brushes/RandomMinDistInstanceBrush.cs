using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RandomMinDistInstanceBrush : InstanceBrush
{
    public float minDistance = 5.0f;
    public override void draw(float x, float z)
    {
        System.Random rand = new System.Random();

        float rdx = (float)((rand.NextDouble() - 0.5) * 2 * radius);
        float rdz = (float)((rand.NextDouble() - 0.5) * 2 * radius);
        bool spwn = true;
        Vector3 loc;
        float xloc;
        float zloc;
        for (int i = 0; i < terrain.getObjectCount(); i++)
        {
            loc = terrain.getObjectLoc(i);
            xloc = loc.x;
            zloc = loc.z;
            if (dist(x + rdx, z + rdz, xloc, zloc) < minDistance)
            {
                spwn = false;
            }
        }
        if (spwn)
        {
            spawnObject(x + rdx, z + rdz);
        }
    }
    private static float dist(float x1, float z1, float x2, float z2)
    {
        return (float)Math.Pow(Math.Pow(x1 - x2, 2) + Math.Pow(z1 - z2, 2), 0.5f);
    }
}