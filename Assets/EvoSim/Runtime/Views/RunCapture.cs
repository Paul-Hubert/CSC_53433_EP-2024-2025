using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace EvoSim
{
    /// <summary>
    /// Saves what the screen shows (overlay included): a PNG when the run starts, every few seconds of real time, and
    /// when it stops, into &lt;folder&gt;/&lt;run&gt;/ (the run recorder's folder name), then a contact sheet of them.
    /// PNGs are encoded off the main thread. It only reads the World and draws no random numbers (RAND-11).
    /// </summary>
    [DisallowMultipleComponent]
    public class RunCapture : MonoBehaviour
    {
        [SerializeField, Tooltip("The World captured; empty = the first World of the scene.")]
        World world;
        [SerializeField, Min(0f), Tooltip("Seconds of real time between captures (0 = only at the start and the end).")]
        float everySeconds = 10f;
        [SerializeField, Tooltip("Folder of the captures, relative to the project (in a build: to the build's folder).")]
        string folder = "Logs/EvoSim/captures";
        [SerializeField, Tooltip("Make contact_sheet.png when the run stops.")]
        bool contactSheet = true;

        readonly List<Task> writes = new List<Task>();
        float next = -1f;
        bool startTaken, finished, capturing;
        float capturingSince;
        const float GiveUpSeconds = 5f;                                                // the editor's end of frame needs a visible Game view

        public World World { get => world; set => world = value; }
        public float EverySeconds { get => everySeconds; set => everySeconds = Mathf.Max(0f, value); }
        public string Folder { get => folder; set => folder = value; }
        /// <summary>This run's capture folder, once the run started.</summary>
        public string RunFolder { get; private set; }
        public int Captures { get; private set; }
        /// <summary>True once the end capture and the contact sheet are written.</summary>
        public bool Finished => finished;
        public string ContactSheetPath { get; private set; }
        /// <summary>Captures given up because no frame was drawn for five seconds (a hidden Game view in the editor).</summary>
        public int Skipped { get; private set; }

        /// <summary>A path relative to the project in the editor, to the build's folder in a player (where the run files go too).</summary>
        public static string Resolve(string path) => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), path));

        static bool CanCapture => SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;

        void Update()
        {
            if (world == null) world = FindAnyObjectByType<World>();
            if (capturing && Time.unscaledTime - capturingSince > GiveUpSeconds)
            {
                StopAllCoroutines();
                capturing = false;
                Skipped++;
                if (world != null && world.State == RunState.Stopped) Finish();
            }
            if (world == null || !world.IsInitialized || finished || capturing || !CanCapture) return;
            if (RunFolder == null) RunFolder = MakeFolder();
            if (!startTaken) { startTaken = true; StartCoroutine(Capture("start", false)); return; }
            if (world.State == RunState.Stopped) { StartCoroutine(Capture("end", true)); return; }
            if (everySeconds > 0f && Time.unscaledTime >= next) StartCoroutine(Capture("t", false));
        }

        string MakeFolder()
        {
            var recorder = world.Service<RunRecorder>();
            string run = recorder != null && !string.IsNullOrEmpty(recorder.RunFolder)
                ? Path.GetFileName(recorder.RunFolder)
                : $"{world.name}-s{world.Seed}-{DateTime.Now:yyyyMMdd-HHmmss}";
            string path = Path.Combine(Resolve(folder), run);
            Directory.CreateDirectory(path);
            return path;
        }

        IEnumerator Capture(string label, bool last)
        {
            capturing = true;
            capturingSince = Time.unscaledTime;
            next = Time.unscaledTime + everySeconds;
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] raw = shot.GetRawTextureData();
            var format = shot.graphicsFormat;
            uint w = (uint)shot.width, h = (uint)shot.height;
            Destroy(shot);
            string file = Path.Combine(RunFolder, $"{Captures:000}-t{world.Tick:000000}-{label}.png");
            Captures++;
            writes.Add(Task.Run(() => File.WriteAllBytes(file, ImageConversion.EncodeArrayToPNG(raw, format, w, h))));
            writes.RemoveAll(t => t.IsCompleted);
            capturing = false;
            if (last) Finish();
        }

        /// <summary>Waits for the files, then makes the contact sheet. Called at the end, and when Play mode stops mid-run.</summary>
        void Finish()
        {
            if (finished) return;
            finished = true;
            try { Task.WaitAll(writes.ToArray()); } catch (AggregateException e) { Debug.LogException(e); }
            writes.Clear();
            if (contactSheet && RunFolder != null && CanCapture) ContactSheetPath = ContactSheet.Make(RunFolder);
        }

        void OnDisable()
        {
            if (RunFolder != null) Finish();
        }
    }

    /// <summary>A grid of a folder's PNG captures, scaled down on the GPU, saved as contact_sheet.png.</summary>
    public static class ContactSheet
    {
        public static string Make(string folder, int columns = 4, int max = 24, int thumbWidth = 480)
        {
            var files = new List<string>(Directory.GetFiles(folder, "*.png"));
            files.RemoveAll(f => Path.GetFileName(f) == "contact_sheet.png");
            files.Sort(StringComparer.Ordinal);
            if (files.Count == 0) return null;
            if (files.Count > max)                                                    // evenly spaced, first and last kept
            {
                var picked = new List<string>();
                for (int i = 0; i < max; i++) picked.Add(files[(int)Math.Round(i * (files.Count - 1) / (double)(max - 1))]);
                files = picked;
            }
            var probe = new Texture2D(2, 2);
            probe.LoadImage(File.ReadAllBytes(files[0]));
            int thumbHeight = Mathf.Max(1, Mathf.RoundToInt(thumbWidth * probe.height / (float)probe.width));
            Free(probe);
            int cols = Mathf.Min(columns, files.Count), rows = (files.Count + cols - 1) / cols;
            var sheet = new Texture2D(cols * thumbWidth, rows * thumbHeight, TextureFormat.RGB24, false);
            var background = new Color32[sheet.width * sheet.height];                    // empty cells dark, not undefined
            for (int i = 0; i < background.Length; i++) background[i] = new Color32(24, 24, 24, 255);
            sheet.SetPixels32(background);
            var rt = RenderTexture.GetTemporary(thumbWidth, thumbHeight, 0, RenderTextureFormat.ARGB32);
            var active = RenderTexture.active;
            for (int i = 0; i < files.Count; i++)
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(files[i]));
                Graphics.Blit(tex, rt);
                RenderTexture.active = rt;
                sheet.ReadPixels(new Rect(0, 0, thumbWidth, thumbHeight), (i % cols) * thumbWidth, (rows - 1 - i / cols) * thumbHeight);
                Free(tex);
            }
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);
            sheet.Apply(false);
            string path = Path.Combine(folder, "contact_sheet.png");
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            Free(sheet);
            return path;
        }

        static void Free(UnityEngine.Object o)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);                                  // called from the editor
        }
    }
}
