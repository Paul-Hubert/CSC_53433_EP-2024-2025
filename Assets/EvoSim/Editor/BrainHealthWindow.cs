using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// Brain health (21 §4): every brain and mutator client of the current world with its host, a Test connection
    /// button (V-60, on demand), model and revision or digest, latency, failures, and the answer cache's size and hits.
    /// </summary>
    public class BrainHealthWindow : EditorWindow
    {
        readonly System.Collections.Concurrent.ConcurrentDictionary<Object, string> tested = new System.Collections.Concurrent.ConcurrentDictionary<Object, string>();   // written by test tasks
        Vector2 scroll;

        [MenuItem("Window/EvoSim/Brain Health")]
        public static void Open() => GetWindow<BrainHealthWindow>("Brain Health").Show();

        void OnInspectorUpdate() => Repaint();

        void OnGUI()
        {
            var w = EditorWorlds.Current();
            if (w == null) { EditorGUILayout.HelpBox("No World in the open scenes.", MessageType.Info); return; }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var b in w.GetComponentsInChildren<Brain>(true))
            {
                EditorGUILayout.LabelField(b.gameObject.name, $"{b.Id}, {b.ModelIdentity}, mode {b.Mode}", EditorStyles.boldLabel);
                if (b is HttpBrain h) Http(h, h.Host, h.Stats, () => h.TestConnection());
                EditorGUILayout.LabelField("Strict, cache", $"{b.Strict}, {b.UsesCache}" + (w.DefaultBrain == b ? " — world default" : ""));
            }
            foreach (var c in w.GetComponentsInChildren<HttpMutatorClient>(true))
            {
                EditorGUILayout.LabelField(c.gameObject.name, "mutator " + c.ModelIdentity, EditorStyles.boldLabel);
                Http(c, c.Host, c.Stats, () => c.TestConnection());
            }
            var cache = w.GetComponentInChildren<AnswerCache>(true);
            if (cache != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Answer cache", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Folder", cache.Folder);
                long bytes = Directory.Exists(cache.Folder) ? new DirectoryInfo(cache.Folder).GetFiles("*.jsonl").Sum(f => f.Length) : 0;
                EditorGUILayout.LabelField("Size, hits, stores", $"{bytes / 1024f:0} KB, {cache.Hits}, {cache.Stores}");
            }
            if (w.IsInitialized)
                EditorGUILayout.LabelField("This run", $"{w.Decisions.ModelCalls} model calls, {w.Decisions.CacheHits} cache hits, {w.Mutations.ModelCalls} mutator calls");
            EditorGUILayout.EndScrollView();
        }

        void Http(Object owner, string host, HttpStats stats, System.Func<System.Threading.Tasks.Task<string>> test)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Host", host);
            if (GUILayout.Button("Test connection", GUILayout.Width(120)))
            {
                tested[owner] = "testing…";
                var task = test();
                task.ContinueWith(t =>
                {
                    var report = new ValidationReport();
                    HttpChecks.Reachability(report, owner, t.Result);
                    tested[owner] = report.Messages.Count == 0 ? "reachable" : report.Messages[0].ToString();
                });
            }
            EditorGUILayout.EndHorizontal();
            if (tested.TryGetValue(owner, out var result)) EditorGUILayout.LabelField("", result, EditorStyles.wordWrappedMiniLabel);
            var lat = stats.Latencies;
            EditorGUILayout.LabelField("Tries, retries, failures", $"{stats.Tries}, {stats.Retries}, {stats.Failures} ({stats.Timeouts} time-outs)");
            if (lat.Count > 0) EditorGUILayout.LabelField("Latency", $"mean {lat.Average():0.00} s, last {lat[lat.Count - 1]:0.00} s");
        }
    }
}
