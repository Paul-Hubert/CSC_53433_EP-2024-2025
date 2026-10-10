using UnityEngine;
using UnityEngine.AI;


#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(MeshFilter))]
public class MeshDeformer : MonoBehaviour
{
    private Mesh deformingMesh;
    private string meshName;
    private Vector3[] originalVertices, displacedVertices;
    private string currentDeformer;

    void Start()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;


        if (!mesh.name.EndsWith("Deformed"))
        {
            // Duplicate so we don’t overwrite the project asset
            Mesh newMesh = Instantiate(mesh);
#if UNITY_EDITOR
            int id = AssetDatabase.GetAllAssetPaths().Length;
#else
            int id = GetInstanceID();      // AssetDatabase exists only in the editor; any unique number names the copy
#endif
            newMesh.name = id + "_" + mesh.name + "_Deformed";
            meshName = newMesh.name;
            mf.sharedMesh = newMesh;
            deformingMesh = newMesh;
        }
        else
        {
            Mesh newMesh = Instantiate(mesh);
            newMesh.name = mesh.name;
            meshName= newMesh.name;
            mf.sharedMesh = newMesh;
            deformingMesh = newMesh;
        }


        originalVertices = deformingMesh.vertices;
        displacedVertices = (Vector3[])originalVertices.Clone();
    }


    public void ApplyVertices()
    {
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();

#if UNITY_EDITOR
        // Save as asset if we are in editor
        string path = "Assets/DeformedMeshes/" + meshName + ".asset";
        System.IO.Directory.CreateDirectory("Assets/DeformedMeshes");

        AssetDatabase.CreateAsset(Object.Instantiate(deformingMesh), path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }


    public void AddDeformingForce(Vector3 point, float force)
    {
        // convert hit point into local space
        Vector3 localPoint = transform.InverseTransformPoint(point);

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            AddForceToVertex(i, localPoint, force);
        }

        ApplyVertices();
    }

    public void SetDeformer(string deformer)
    {
        currentDeformer = deformer;
    }


    // -------------------------- DEFORMER FUNCTIONS -------------------------------------------------------

    public void AddForceToVertex(int i, Vector3 point, float force)
    {
        switch(currentDeformer)
        {
            // case : your custom functions
            case "Push":
                PushVertex(i, point, force);
                break;

            // ...

            default:
                Debug.Log("Adding 0 force - test");
                break; 
        }

    }

    public void PushVertex(int i, Vector3 point, float force)
    {
        // FILL HERE
        

    }


}
