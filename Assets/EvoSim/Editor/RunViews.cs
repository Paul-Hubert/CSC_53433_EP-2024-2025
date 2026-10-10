using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EvoSim.Editor
{
    /// <summary>
    /// Makes a scene watchable (20 §6): a "Run views" object with the ground, entity markers, overlay and capture, and a
    /// <see cref="WorldCamera"/> on the main camera; and the reference bodies (a capsule standing on the ground, a nose
    /// that shows where it goes, a marker in the colour of its action). All of them only read the World (SPACE-12).
    /// </summary>
    public static class RunViews
    {
        public const string ObjectName = "Run views";
        const string MarkerMaterial = ReferenceScenes.BodiesFolder + "/ViewMarker.mat";
        const string NoseMaterial = ReferenceScenes.BodiesFolder + "/ViewNose.mat";

        /// <summary>Adds what is missing to the open scene's run views and camera; returns what it added.</summary>
        public static string AddTo(Scene scene)
        {
            var added = new List<string>();
            GameObject views = null;
            Camera camera = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == ObjectName) views = root;
                if (camera == null) camera = root.GetComponentInChildren<Camera>(true);
            }
            if (views == null)
            {
                views = new GameObject(ObjectName);
                SceneManager.MoveGameObjectToScene(views, scene);
                added.Add(ObjectName);
            }
            Add<GroundView>(views, added);
            Add<EntityMarkers>(views, added);
            Add<RunOverlay>(views, added);
            Add<RunCapture>(views, added);
            if (camera != null && camera.GetComponent<WorldCamera>() == null)
            {
                camera.gameObject.AddComponent<WorldCamera>();
                added.Add("WorldCamera on " + camera.name);
            }
            return added.Count == 0 ? "nothing" : string.Join(", ", added);
        }

        static void Add<T>(GameObject go, List<string> added) where T : Component
        {
            if (go.GetComponent<T>() != null) return;
            go.AddComponent<T>();
            added.Add(typeof(T).Name);
        }

        /// <summary>Adds the run views to the six reference scenes and rebuilds the two reference bodies; saves.</summary>
        [MenuItem("EvoSim/Views/Add Run Views to Reference Scenes")]
        public static string AddToReferenceScenes()
        {
            var lines = new List<string> { "bodies: " + RebuildBodies() };
            foreach (var path in Directory.GetFiles(ReferenceScenes.ScenesFolder, "*.unity"))
            {
                string p = path.Replace('\\', '/');
                var scene = EditorSceneManager.OpenScene(p, OpenSceneMode.Additive);
                try
                {
                    string added = AddTo(scene);
                    if (added != "nothing") EditorSceneManager.SaveScene(scene);
                    lines.Add($"{Path.GetFileNameWithoutExtension(p)}: {added}");
                }
                finally { EditorSceneManager.CloseScene(scene, true); }
            }
            AssetDatabase.SaveAssets();
            string result = string.Join("\n", lines);
            Debug.Log("EvoSim run views:\n" + result);
            return result;
        }

        /// <summary>Rebuilds PreyBody and PredatorBody in place (same assets, same GUIDs), keeping their materials.</summary>
        public static string RebuildBodies()
        {
            var made = new List<string>();
            foreach (var name in new[] { "PreyBody", "PredatorBody" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(ReferenceScenes.BodiesFolder + "/" + name + ".mat");
                if (material == null) continue;
                BodyPrefab(name, material);
                made.Add(name);
            }
            return string.Join(", ", made);
        }

        /// <summary>
        /// A reference body: the capsule 1 m high standing on the ground (the Body scales it to its size), a dark nose at
        /// the front (views turn toward where the animal goes), an action marker above, and an <see cref="ActionView"/>.
        /// </summary>
        internal static GameObject BodyPrefab(string file, Material bodyMaterial)
        {
            var root = new GameObject(file);
            var body = Part(root, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(1f, 0.5f, 1f), bodyMaterial);
            Part(root, PrimitiveType.Sphere, "Nose", new Vector3(0f, 0.6f, 0.48f), Vector3.one * 0.3f, Material(NoseMaterial, new Color(0.08f, 0.08f, 0.08f)));
            var marker = Part(root, PrimitiveType.Sphere, "Action", new Vector3(0f, 1.4f, 0f), Vector3.one * 0.6f, Material(MarkerMaterial, Color.white));
            root.AddComponent<ActionView>().Configure(body.GetComponent<Renderer>(), marker.GetComponent<Renderer>());
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ReferenceScenes.BodiesFolder + "/" + file + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject Part(GameObject root, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());                         // views are never hit (SPACE-12)
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Material Material(string path, Color color)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Standard")) { color = color };
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
