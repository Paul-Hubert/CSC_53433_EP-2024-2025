using System;
using System.Collections.Generic;
using System.Globalization;

namespace EvoSim
{
    /// <summary>
    /// A watched run's settings from a command line (the Windows player): -scene name, -seed n, -ticks n,
    /// -speed (ticks per second | fast | fixed), -wait (responsive | freeze), -brain (a brain's GameObject name or id),
    /// -capture seconds (0 = start and end only, off = none), -cache folder, -run folder name, -noMutation, -quitAtEnd,
    /// -window WIDTHxHEIGHT (a window of that many pixels).
    /// Parsing and applying are separate, so tests check both without a player.
    /// </summary>
    public sealed class PlayerArgs
    {
        public string Scene, Brain, Cache, Run;
        public int? Seed, Ticks;
        public float? TicksPerSecond;
        public RunSpeed? Speed;
        public WaitMode? Wait;
        /// <summary>Seconds between captures; null = the scene's setting; negative = no capture.</summary>
        public float? Capture;
        public bool NoMutation, QuitAtEnd;
        /// <summary>The window's size in pixels (-window 1920x1080), or 0 × 0: the player's own setting.</summary>
        public int WindowWidth, WindowHeight;
        public readonly List<string> Problems = new List<string>();

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
                    case "-quitatend": p.QuitAtEnd = true; break;
                }
            }
            return p;
        }

        static int? Int(PlayerArgs p, string name, string v)
        {
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 0) return n;
            p.Problems.Add($"{name} {v}: a whole number, 0 or more");
            return null;
        }

        /// <summary>Sets the World before it initialises; returns one line saying what was set.</summary>
        public string ApplyTo(World w)
        {
            var said = new List<string>();
            if (Seed.HasValue) { w.Seed = Seed.Value; said.Add("seed " + Seed.Value); }
            if (Ticks.HasValue) { w.TickLimit = Ticks.Value; said.Add("ticks " + Ticks.Value); }
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
        public void ApplyTo(RunCapture capture)
        {
            if (capture == null || !Capture.HasValue) return;
            if (Capture.Value < 0f) capture.enabled = false;
            else capture.EverySeconds = Capture.Value;
        }
    }
}
