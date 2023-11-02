using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CopyPasteBrush : InstanceBrush
{
    // the copying takes place when the left shift key is pressed, the pasting when the mouse is clicked
    private List<float> xpos = new List<float> { 0 };
    private List<float> zpos = new List<float> { 0 };
    public float MinimalDistance = 1.0f;
    void update(float x, float z, float radius)
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            // Left Shift is currently pressed down (and we are clicking)
            xpos.Clear();
            zpos.Clear();
            Vector3 loc;
            float xloc;
            float zloc;
            for (int i = 0; i < terrain.getObjectCount(); i++)
            {
                loc = terrain.getObjectLoc(i);
                xloc = loc.x;
                zloc = loc.z;
                if ((xloc < x + radius) & (xloc > x - radius) & (zloc > z - radius) & (zloc < z + radius))
                {
                    xpos.Add(xloc - x);
                    zpos.Add(zloc - z);
                }
            }
        }
    }
    public override void draw(float x, float z)
    {
        update(x, z, radius);
        Vector3 loc;
        float xloc;
        float zloc;
        bool spwn;
        for (int i = 0; i < xpos.Count; i++)
        {
            spwn = true;
            for (int k = 0; k < terrain.getObjectCount(); k++)
            {
                loc = terrain.getObjectLoc(k);
                xloc = loc.x;
                zloc = loc.z;
                if (dist(x + xpos[i], z + zpos[i], xloc, zloc) < MinimalDistance)
                {
                    spwn = false;
                }

            }
            if (spwn)
            {
                spawnObject(x + xpos[i], z + zpos[i]);
            }
        }
    }
    private static float dist(float x1, float z1, float x2, float z2)
    {
        return (float)Math.Pow(Math.Pow(x1 - x2, 2) + Math.Pow(z1 - z2, 2), 0.5f);
    }

}