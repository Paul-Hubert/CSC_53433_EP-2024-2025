using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MagicInstanceBrush : InstanceBrush {

    public override void draw(float x, float z) {
       
        float randomx = UnityEngine.Random.Range(x - radius, x + radius);
        float randomz = UnityEngine.Random.Range(z - radius, z + radius);
        float maxangle = 50;
        float area = 10;
        bool isClose = false;
        if (terrain.getSteepness(x, z) < maxangle)
        {
            for (int i = 0; i < terrain.getObjectCount()-1; i++)
            {
                Vector3 objectLoc = terrain.getObjectLoc(i);
                if (objectLoc.x >= randomx - area && objectLoc.x <= randomx + area &&
                    objectLoc.z >= randomz - area && objectLoc.z <= randomz + area)
                {
                    isClose = true;
                    break;
                }
            }
            if (!isClose)
            {
                spawnObject(randomx, randomz);
                terrain.changeTree();
            }
        }
        
       
    }
}
