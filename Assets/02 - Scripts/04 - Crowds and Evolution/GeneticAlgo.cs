using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeneticAlgo : MonoBehaviour
{

    [Header("Genetic Algorithm parameters")]
    public int popSize = 100;
    public GameObject animalPrefab;
    public GameObject alligatorPrefab;

    [Header("Dynamic elements")]
    public float vegetationGrowthRate = 1.0f;
    public float currentGrowth;
    public float maxAltitudeGrowth;

    private List<GameObject> animals;
    private List<GameObject> aligators;
    protected Terrain terrain;
    protected CustomTerrain customTerrain;
    protected float width;
    protected float height;

    void Start()
    {
        // Retrieve terrain.
        terrain = Terrain.activeTerrain;
        customTerrain = GetComponent<CustomTerrain>();
        width = terrain.terrainData.size.x;
        height = terrain.terrainData.size.z;

        // Initialize terrain growth.
        currentGrowth = 0.0f;

        // Initialize animals array.
        animals = new List<GameObject>();
        for (int i = 0; i < popSize; i++)
        {
            GameObject animal = makeAnimal();
            animals.Add(animal);

        }
    }

    void Update()
    {
        // Keeps animal to a minimum.
        while (animals.Count < popSize / 2)
        {
            animals.Add(makeAnimal());
        }
        customTerrain.debug.text = "N° animals: " + animals.Count.ToString();

        // Update grass elements/food resources.
        updateResources();
    }

    /// <summary>
    /// Method to place grass or other resource in the terrain.
    /// </summary>
    public void updateResources()
    {
        Vector2 detail_sz = customTerrain.detailSize();
        int[,] details = customTerrain.getDetails();
        currentGrowth += vegetationGrowthRate;
        while (currentGrowth > 1.0f)
        {
            int x = (int)(UnityEngine.Random.value * detail_sz.x);
            int y = (int)(UnityEngine.Random.value * detail_sz.y);

            Vector3 scale = terrain.terrainData.heightmapScale;
            float altitude = customTerrain.getInterp(x / scale.x / detail_sz.x * width, y / scale.z / detail_sz.y * height);
            float altitudeTest = UnityEngine.Random.value * altitude / maxAltitudeGrowth;
            if (altitudeTest < 1.0f)
            {
                details[y, x] = 1;
                currentGrowth -= 1.0f;
            }
        }
        customTerrain.saveDetails();
    }


    /// <summary>
    /// Method to instantiate an animal prefab. It must contain the animal.cs class attached.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public GameObject makeAnimal(Vector3 position)
    {
        GameObject animal = Instantiate(animalPrefab, transform);
        animal.GetComponent<Animal>().Setup(customTerrain, this);
        animal.transform.position = position;
        animal.transform.Rotate(0.0f, UnityEngine.Random.value * 360.0f, 0.0f);
        return animal;
    }

    /// <summary>
    /// If makeAnimal() is called without position, we randomize it on the terrain.
    /// </summary>
    /// <returns></returns>
    public GameObject makeAnimal()
    {
        Vector3 scale = terrain.terrainData.heightmapScale;
        float x = UnityEngine.Random.value * width;
        float z = UnityEngine.Random.value * height;
        float y = customTerrain.getInterp(x / scale.x, z / scale.z);
        return makeAnimal(new Vector3(x, y, z));
    }

    /// <summary>
    /// Method to add an animal inherited from anothed. It spawns where the parent was.
    /// </summary>
    /// <param name="parent"></param>
    public void addOffspring(Animal parent)
    {
        GameObject animal = makeAnimal(parent.transform.position);
        animal.GetComponent<Animal>().InheritBrain(parent.GetBrain(), true);
        animals.Add(animal);
    }

    /// <summary>
    /// Remove instance of an animal.
    /// </summary>
    /// <param name="animal"></param>
    public void removeAnimal(Animal animal)
    {
        animals.Remove(animal.transform.gameObject);
        Destroy(animal.transform.gameObject);
    }

    public void updateSpeed(Animal animal, float speed)
    {
        CapsuleAutoController capsuleMotionScript = animal.GetComponent<CapsuleAutoController>();
        capsuleMotionScript.changeSpeed(speed);

    }

}
