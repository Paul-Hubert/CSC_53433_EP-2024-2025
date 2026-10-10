using System.Text;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The run's numbers on screen, in the editor and in a build (immediate-mode GUI): tick, state and what the tick
    /// waits for, ticks per second achieved against the target, frames per second, each species' population, births
    /// and deaths, mutations, model calls; and the followed animal's action, stats and genes (<see cref="WorldCamera"/>).
    /// It only reads the World (<see cref="RunNumbers"/>). Tab hides it.
    /// </summary>
    [DisallowMultipleComponent]
    public class RunOverlay : MonoBehaviour
    {
        [SerializeField, Tooltip("The World shown; empty = the first World of the scene.")]
        World world;
        [SerializeField, Tooltip("The camera that follows animals; empty = the one in the scene.")]
        WorldCamera worldCamera;
        [SerializeField, Min(6), Tooltip("Font size at 1080 lines; it scales with the window.")]
        int fontSize = 16;
        [SerializeField, Tooltip("Show the panels (Tab toggles).")]
        bool visible = true;

        readonly RunNumbers numbers = new RunNumbers();
        readonly StringBuilder sb = new StringBuilder();
        readonly System.Collections.Generic.List<string> legend = new System.Collections.Generic.List<string>();
        GUIStyle box, label;
        Rect runPanel, animalPanel;
        float scale = 1f;

        // Pacing, measured over about a second of real time; and over the whole run (from its second second).
        float windowStart = -1f, waitingSince = -1f, runStart = -1f, runSeconds;
        int windowFrames, windowStartTick, runFrames, runStartTick, windows;

        public World World { get => world; set => world = value; }
        public RunNumbers Numbers => numbers;
        /// <summary>Ticks per second achieved over the last second.</summary>
        public float TicksPerSecond { get; private set; }
        public float FramesPerSecond { get; private set; }
        /// <summary>Real seconds the current tick has waited, or 0.</summary>
        public float WaitingSeconds => waitingSince < 0f ? 0f : Time.unscaledTime - waitingSince;
        public bool Visible { get => visible; set => visible = value; }
        /// <summary>The lowest one-second frame rate of the run so far (the first second, loading, is left out).</summary>
        public float MinFramesPerSecond { get; private set; } = float.PositiveInfinity;
        /// <summary>The most animals alive at once so far.</summary>
        public int MostLiving { get; private set; }

        /// <summary>The run's pace in one line: mean ticks/s against the target, mean and lowest FPS, most animals alive.</summary>
        public string PaceSummary() =>
            runSeconds <= 0f ? "no pace measured yet" :
            $"{(numbers.Tick - runStartTick) / runSeconds:0.00} ticks/s (target {TargetText()}), {runFrames / runSeconds:0} FPS mean, " +
            $"{(float.IsInfinity(MinFramesPerSecond) ? 0f : MinFramesPerSecond):0} FPS lowest, {MostLiving} animals at most, over {runSeconds:0} s";

        void Update()
        {
            if (world == null) world = FindAnyObjectByType<World>();
            if (worldCamera == null) worldCamera = FindAnyObjectByType<WorldCamera>();
            if (worldCamera != null && worldCamera.Blocks == null) worldCamera.Blocks = Covers;
            if (Input.GetKeyDown(KeyCode.Tab)) visible = !visible;
            if (world == null || !world.IsInitialized) return;

            float now = Time.unscaledTime;
            if (windowStart < 0f) { windowStart = now; windowStartTick = world.Tick; }
            windowFrames++;
            if (now - windowStart >= 1f)
            {
                TicksPerSecond = (world.Tick - windowStartTick) / (now - windowStart);
                FramesPerSecond = windowFrames / (now - windowStart);
                if (++windows >= 2 && world.State != RunState.Stopped) MinFramesPerSecond = Mathf.Min(MinFramesPerSecond, FramesPerSecond);
                windowStart = now;
                windowStartTick = world.Tick;
                windowFrames = 0;
            }
            if (world.State != RunState.Stopped && windows >= 1)                              // the whole run, after its first second
            {
                if (runStart < 0f) { runStart = now; runStartTick = world.Tick; }
                runFrames++;
                runSeconds = now - runStart;
            }
            bool waits = world.State == RunState.Waiting || world.State == RunState.Blocked;
            if (waits && waitingSince < 0f) waitingSince = now;
            else if (!waits) waitingSince = -1f;
            numbers.Read(world);
            int living = 0;
            foreach (var s in numbers.Species) living += s.Living;
            MostLiving = Mathf.Max(MostLiving, living);
        }

        /// <summary>Whether a screen point (origin bottom left) lies on a panel: the camera doesn't pick there.</summary>
        public bool Covers(Vector2 screen)
        {
            if (!visible) return false;
            var gui = new Vector2(screen.x / scale, (Screen.height - screen.y) / scale);
            return runPanel.Contains(gui) || animalPanel.Contains(gui);
        }

        /// <summary>The run panel's text (the overlay draws exactly this).</summary>
        public string RunText()
        {
            sb.Clear();
            sb.Append("Tick ").Append(numbers.Tick).Append("   ").Append(StateText()).AppendLine();
            sb.Append("Ticks/s ").Append(TicksPerSecond.ToString("0.0")).Append(" of ").Append(TargetText())
              .Append("   FPS ").Append(FramesPerSecond.ToString("0")).AppendLine();
            foreach (var s in numbers.Species)
                sb.Append(s.Name).Append(": ").Append(s.Living).Append(" living, ").Append(s.Births).Append(" born, ")
                  .Append(s.Deaths).Append(" died").Append(s.Kills > 0 ? $", {s.Kills} kills" : "")
                  .Append(s.Eggs > 0 ? $", {s.Eggs} eggs" : "").Append(", gen ").Append(s.MaxGeneration).AppendLine();
            sb.Append("Mutations ").Append(numbers.MutationsOk).Append(" ok, ").Append(numbers.MutationsFailed).Append(" failed")
              .Append(" of ").Append(numbers.MutationAttempts).AppendLine();
            sb.Append("Model calls: brain ").Append(numbers.BrainCalls).Append(", mutator ").Append(numbers.MutatorCalls)
              .Append("; cache ").Append(numbers.CacheHits).Append(", memo ").Append(numbers.MemoHits);
            return sb.ToString();
        }

        string StateText()
        {
            switch (numbers.State)
            {
                case RunState.Waiting:
                case RunState.Blocked:
                    return $"waiting for {numbers.WaitingFor}, {WaitingSeconds:0.0} s";
                case RunState.Stopped: return "stopped: " + world.StopReason;
                default: return numbers.State.ToString().ToLowerInvariant();
            }
        }

        string TargetText()
        {
            switch (world.RunSpeed)
            {
                case RunSpeed.RealTime: return world.RealTimeTicksPerSecond.ToString("0.#");
                case RunSpeed.PerFixedUpdate: return (world.TicksPerFixedUpdate / Mathf.Max(1e-4f, Time.fixedDeltaTime)).ToString("0.#");
                default: return "as fast as possible";
            }
        }

        /// <summary>The followed animal's panel text, or null.</summary>
        public string AnimalText()
        {
            var a = worldCamera != null ? worldCamera.Followed : null;
            if (a == null)
                return worldCamera != null && worldCamera.Lost != null ? $"{worldCamera.Lost.Species.DisplayName} #{worldCamera.Lost.Id} is gone" : null;
            var s = a.Species;
            sb.Clear();
            sb.Append(s.DisplayName).Append(" #").Append(a.Id).Append(", age ").Append(a.Age).Append(", generation ").Append(a.Generation).AppendLine();
            sb.Append("Action: ").Append(a.CurrentAction != null ? a.CurrentAction.Name : "none").Append(a.Searching ? " (searching)" : "");
            if (a.IsBusy) sb.Append(", busy ").Append(a.BusyTicks).Append(" (").Append(a.BusyReason).Append(')');
            sb.AppendLine();
            foreach (var d in s.Declarations.Stats) sb.Append(d.Id.Name).Append(' ').Append(a[d.Id].ToString("0.#")).Append("   ");
            sb.AppendLine();
            if (a.LastProbabilities != null)
            {
                sb.Append("Last decision (tick ").Append(a.LastDecisionTick).Append("): ");
                for (int i = 0; i < a.LastProbabilities.Length && i < s.Actions.Count; i++)
                    sb.Append(s.Actions[i].Name).Append(' ').Append(a.LastProbabilities[i].ToString("0.00")).Append("  ");
                sb.AppendLine();
            }
            sb.Append("Genes:");
            for (int i = 0; i < s.Genes.Count && a.Genome != null; i++)
            {
                var allele = a.Genome[i];
                sb.AppendLine().Append("  ").Append(s.Genes[i].Label).Append(": ")
                  .Append(allele.Kind == AlleleKind.Text ? allele.Text : allele.Number.ToString("0.##"));
            }
            return sb.ToString();
        }

        void OnGUI()
        {
            if (!visible || world == null || !world.IsInitialized) return;
            scale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 2.5f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (box == null)
            {
                box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, richText = false, wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
                label = new GUIStyle(GUI.skin.label) { wordWrap = false };
            }
            box.fontSize = label.fontSize = fontSize;
            float width = Screen.width / scale, lineH = fontSize * 1.45f;

            var run = new GUIContent(RunText());
            float runW = Mathf.Min(520f, width * 0.5f);
            runPanel = new Rect(8f, 8f, runW, box.CalcHeight(run, runW));
            GUI.Box(runPanel, run, box);

            float y = runPanel.yMax + 6f, x = 8f;                                       // the action legend, each name once
            legend.Clear();
            foreach (var s in world.AllSpecies)
                foreach (var act in s.Actions)
                    if (!legend.Contains(act.Name)) legend.Add(act.Name);
            legend.Add("search");
            foreach (var name in legend)
            {
                float w = label.CalcSize(new GUIContent(name)).x + lineH;
                if (x + w > runPanel.xMax) { x = 8f; y += lineH; }
                Legend(new Rect(x, y, w, lineH), name == "search" ? ActionColors.Searching : ActionColors.Of(name), name);
                x += w + 6f;
            }

            string animal = AnimalText();
            if (animal != null)
            {
                var content = new GUIContent(animal);
                float w = Mathf.Min(560f, width * 0.45f);
                animalPanel = new Rect(width - w - 8f, 8f, w, box.CalcHeight(content, w));
                GUI.Box(animalPanel, content, box);
            }
            else animalPanel = Rect.zero;
        }

        void Legend(Rect r, Color c, string text)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x, r.y + r.height * 0.25f, r.height * 0.5f, r.height * 0.5f), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(r.x + r.height * 0.7f, r.y, r.width, r.height), text, label);
        }
    }
}
