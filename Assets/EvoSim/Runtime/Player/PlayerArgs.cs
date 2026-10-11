using System;
using System.Collections.Generic;
using System.Globalization;

namespace EvoSim
{
    /// <summary>
    /// A watched run's settings from a command line (the Windows player): -scene name, -seed n, -ticks n, -minutes m (wall clock),
    /// -speed (ticks per second | fast | fixed), -wait (responsive | freeze), -brain (a brain's GameObject name or id),
    /// -capture seconds (0 = start and end only, off = none), -cache folder, -run folder name, -noMutation, -quitAtEnd,
    /// -window WIDTHxHEIGHT (a window of that many pixels), -stagger (every species decides staggered, DEC-04).
    /// Parsing and applying are separate, so tests check both without a player.
    /// </summary>
    /// <remarks>
    /// <para>The arguments (names in any case; an argument left out keeps the scene's setting):</para>
    /// <list type="bullet">
    /// <item><description><c>-scene NAME</c>: the scene to run, by name; a World of another scene doesn't start, and NAME loads instead.</description></item>
    /// <item><description><c>-seed N</c>: the run seed, a whole number, 0 or more (<see cref="World.Seed"/>).</description></item>
    /// <item><description><c>-ticks N</c>: stop after N ticks; 0 = no limit (<see cref="World.TickLimit"/>).</description></item>
    /// <item><description><c>-minutes M</c>: stop after M minutes of wall-clock time, more than 0 (<see cref="World.WallClockMinutes"/>).</description></item>
    /// <item><description><c>-speed S</c>: S ticks per second, at least 0.1 (real time); <c>fast</c>: as many ticks per frame as fit in
    /// the time budget; <c>fixed</c>: a fixed number of ticks per FixedUpdate (<see cref="World.RunSpeed"/>). Results never
    /// depend on it (SPACE-11).</description></item>
    /// <item><description><c>-wait MODE</c>: <c>responsive</c> or <c>freeze</c>, how a tick waits for the brains (SPACE-14).</description></item>
    /// <item><description><c>-brain NAME</c>: the World's default brain, by GameObject name or brain id; a species with a brain of its own
    /// keeps it, and is reported.</description></item>
    /// <item><description><c>-capture S</c>: seconds of real time between screen captures; <c>0</c> = only at the start and the end;
    /// <c>off</c> = no capture (<see cref="RunCapture"/>).</description></item>
    /// <item><description><c>-cache FOLDER</c>: the answer cache's folder, relative to the build's folder (<see cref="AnswerCache"/>).</description></item>
    /// <item><description><c>-run NAME</c>: the run folder's name for every <see cref="RunRecorder"/>; <c>{world}</c> and <c>{seed}</c> are
    /// replaced.</description></item>
    /// <item><description><c>-noMutation</c>: no mutation at all (the NO-MUT control, <see cref="World.NoMutation"/>).</description></item>
    /// <item><description><c>-stagger</c>: every species decides staggered (DEC-04); a <see cref="DecisionSchedule"/> is added where a
    /// species has none.</description></item>
    /// <item><description><c>-quitAtEnd</c>: quit the player once the run stopped and its captures are written.</description></item>
    /// <item><description><c>-window WxH</c>: a window of W × H pixels, at least 160 × 120, instead of the player's own screen mode.</description></item>
    /// </list>
    /// <para>Other arguments (Unity's own, such as <c>-logFile</c>) are ignored. A value that doesn't parse is listed in
    /// <see cref="Problems"/>, and that setting keeps the scene's value. Example:
    /// <c>EvoSim.exe -scene HideVsFlee -seed 1234 -ticks 600 -speed 20 -capture 10 -run NAME -window 1920x1080 -quitAtEnd</c>.</para>
    /// </remarks>
    public sealed class PlayerArgs
    {
        /// <summary>
        /// <c>Scene</c>: <c>-scene</c>, the scene to run, by name. <c>Brain</c>: <c>-brain</c>, a brain's GameObject name or id.
        /// <c>Cache</c>: <c>-cache</c>, the answer cache's folder. <c>Run</c>: <c>-run</c>, the run folder's name. Each null when
        /// not given.
        /// </summary>
        public string Scene, Brain, Cache, Run;
        /// <summary><c>Seed</c>: <c>-seed</c>, the run seed. <c>Ticks</c>: <c>-ticks</c>, the tick limit (0 = none). Each null when not given.</summary>
        public int? Seed, Ticks;
        /// <summary>Ticks per second for <c>-speed S</c> (real time), or null with <c>fast</c>, <c>fixed</c> or no <c>-speed</c>.</summary>
        public float? TicksPerSecond;
        /// <summary>Stop after this many minutes of wall-clock time (the World's WallClockMinutes).</summary>
        public float? Minutes;
        /// <summary>The run speed from <c>-speed</c>, or null: the scene's setting.</summary>
        public RunSpeed? Speed;
        /// <summary>The wait mode from <c>-wait</c>, or null: the scene's setting.</summary>
        public WaitMode? Wait;
        /// <summary>Seconds between captures; null = the scene's setting; negative = no capture.</summary>
        public float? Capture;
        /// <summary>
        /// <c>NoMutation</c>: <c>-noMutation</c> was given. <c>QuitAtEnd</c>: <c>-quitAtEnd</c> was given. <c>Stagger</c>:
        /// <c>-stagger</c> was given.
        /// </summary>
        public bool NoMutation, QuitAtEnd, Stagger;
        /// <summary>The window's size in pixels (-window 1920x1080), or 0 × 0: the player's own setting.</summary>
        public int WindowWidth, WindowHeight;
        /// <summary>One line per argument that could not be parsed or applied; the launcher logs them as warnings.</summary>
        public readonly List<string> Problems = new List<string>();

        /// <summary>Reads the arguments; never throws. Bad values go to <see cref="Problems"/>.</summary>
        /// <param name="args">The command line, e.g. <c>Environment.GetCommandLineArgs()</c> (the program's path first is ignored).</param>
        /// <returns>The settings found.</returns>
        public static PlayerArgs Parse(IReadOnlyList<string> args)
        {
            var p = new PlayerArgs();
            for (int i = 0; i < args.Count; i++)
            {
                string a = args[i], v = i + 1 < args.Count ? args[i + 1] : null;
                switch (a.ToLowerInvariant())
                {
                    case "-scene": p.Scene = v; i++; break;
                    case "-seed": p.Seed = Int(p, a, v); i++; break;
                    case "-ticks": p.Ticks = Int(p, a, v); i++; break;
                    case "-minutes":
                        i++;
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float m) && m > 0f) p.Minutes = m;
                        else p.Problems.Add($"-minutes {v}: minutes of wall-clock time, more than 0");
                        break;
                    case "-brain": p.Brain = v; i++; break;
                    case "-cache": p.Cache = v; i++; break;
                    case "-run": p.Run = v; i++; break;
                    case "-speed":
                        i++;
                        if (string.Equals(v, "fast", StringComparison.OrdinalIgnoreCase)) p.Speed = RunSpeed.Fast;
                        else if (string.Equals(v, "fixed", StringComparison.OrdinalIgnoreCase)) p.Speed = RunSpeed.PerFixedUpdate;
                        else if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float tps) && tps >= 0.1f)
                        {
                            p.Speed = RunSpeed.RealTime;
                            p.TicksPerSecond = tps;
                        }
                        else p.Problems.Add($"-speed {v}: give ticks per second (at least 0.1), fast or fixed");
                        break;
                    case "-wait":
                        i++;
                        if (string.Equals(v, "freeze", StringComparison.OrdinalIgnoreCase)) p.Wait = WaitMode.Freeze;
                        else if (string.Equals(v, "responsive", StringComparison.OrdinalIgnoreCase)) p.Wait = WaitMode.Responsive;
                        else p.Problems.Add($"-wait {v}: responsive or freeze");
                        break;
                    case "-capture":
                        i++;
                        if (string.Equals(v, "off", StringComparison.OrdinalIgnoreCase)) p.Capture = -1f;
                        else if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float s) && s >= 0f) p.Capture = s;
                        else p.Problems.Add($"-capture {v}: seconds between captures, 0 or off");
                        break;
                    case "-window":
                        i++;
                        var wh = (v ?? "").ToLowerInvariant().Split('x');
                        if (wh.Length == 2 && int.TryParse(wh[0], out int ww) && int.TryParse(wh[1], out int hh) && ww >= 160 && hh >= 120)
                        {
                            p.WindowWidth = ww;
                            p.WindowHeight = hh;
                        }
                        else p.Problems.Add($"-window {v}: WIDTHxHEIGHT in pixels, e.g. 1920x1080");
                        break;
                    case "-nomutation": p.NoMutation = true; break;
                    case "-stagger": p.Stagger = true; break;
                    case "-quitatend": p.QuitAtEnd = true; break;
                }
            }
            return p;
        }

        /// <summary>A whole number, 0 or more, from an argument's value; null and a problem line when it isn't one.</summary>
        static int? Int(PlayerArgs p, string name, string v)
        {
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0) return n;
            p.Problems.Add($"{name} {v}: a whole number, 0 or more");
            return null;
        }

        /// <summary>Sets the World before it initialises; returns one line saying what was set.</summary>
        /// <remarks>
        /// Called by <see cref="PlayerLauncher"/> from <see cref="World.Configuring"/>. Only given arguments change the World;
        /// a brain, cache or species problem is added to <see cref="Problems"/>. <c>-scene</c>, <c>-capture</c>,
        /// <c>-window</c> and <c>-quitAtEnd</c> are handled by the launcher.
        /// </remarks>
        /// <param name="w">The World, not initialised yet.</param>
        /// <returns>What was set, e.g. <c>"seed 1234, ticks 600, speed 20 ticks/s"</c>; "" when nothing was.</returns>
        public string ApplyTo(World w)
        {
            var said = new List<string>();
            if (Seed.HasValue) { w.Seed = Seed.Value; said.Add("seed " + Seed.Value); }
            if (Ticks.HasValue) { w.TickLimit = Ticks.Value; said.Add("ticks " + Ticks.Value); }
            if (Minutes.HasValue) { w.WallClockMinutes = Minutes.Value; said.Add("minutes " + Minutes.Value.ToString(CultureInfo.InvariantCulture)); }
            if (Speed.HasValue)
            {
                w.RunSpeed = Speed.Value;
                if (TicksPerSecond.HasValue) w.RealTimeTicksPerSecond = TicksPerSecond.Value;
                said.Add("speed " + (TicksPerSecond.HasValue ? TicksPerSecond.Value.ToString(CultureInfo.InvariantCulture) + " ticks/s" : Speed.Value.ToString()));
            }
            if (Wait.HasValue) { w.WaitMode = Wait.Value; said.Add("wait " + Wait.Value); }
            if (Brain != null)
            {
                Brain found = null;
                foreach (var b in w.GetComponentsInChildren<Brain>(true))
                    if (string.Equals(b.gameObject.name, Brain, StringComparison.OrdinalIgnoreCase) || string.Equals(b.Id, Brain, StringComparison.OrdinalIgnoreCase))
                    {
                        found = b;
                        break;
                    }
                if (found == null) Problems.Add($"-brain {Brain}: no brain of that name under the World");
                else
                {
                    w.DefaultBrain = found;
                    said.Add("brain " + found.gameObject.name);
                    foreach (var s in w.GetComponentsInChildren<Species>(true))
                        if (s.OwnBrain != null && s.OwnBrain != found) Problems.Add($"species {s.DisplayName} keeps its own brain {s.OwnBrain.name}");
                }
            }
            if (NoMutation) { w.NoMutation = true; said.Add("no mutation"); }
            if (Stagger)
            {
                foreach (var s in w.GetComponentsInChildren<Species>(true))                // a schedule of its own, or the species' one, staggered
                {
                    var schedule = s.GetComponentInChildren<DecisionSchedule>(true);
                    if (schedule == null)
                    {
                        var go = new UnityEngine.GameObject("Decision schedule");
                        go.transform.SetParent(s.transform, false);
                        schedule = go.AddComponent<DecisionSchedule>();
                    }
                    schedule.Staggered = true;
                }
                said.Add("staggered decisions");
            }
            if (Cache != null)
            {
                var cache = w.GetComponentInChildren<AnswerCache>(true);
                if (cache != null) { cache.SetFolder(Cache); said.Add("cache " + cache.Folder); }
                else Problems.Add("-cache: the World has no answer cache");
            }
            if (Run != null)
            {
                foreach (var r in w.GetComponentsInChildren<RunRecorder>(true)) r.RunName = Run;
                said.Add("run " + Run);
            }
            return string.Join(", ", said);
        }

        /// <summary>-capture on the scene's capture component: off disables it, else the seconds between captures.</summary>
        /// <param name="capture">The scene's capture component, or null: then nothing happens.</param>
        public void ApplyTo(RunCapture capture)
        {
            if (capture == null || !Capture.HasValue) return;
            if (Capture.Value < 0f) capture.enabled = false;
            else capture.EverySeconds = Capture.Value;
        }
    }
}
