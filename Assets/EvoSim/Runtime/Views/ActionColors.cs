using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// One colour per action name, shared by the views and the overlay's legend. Actions it doesn't know get a colour
    /// from their name's characters (the same on every run; no random numbers, RAND-11).
    /// </summary>
    public static class ActionColors
    {
        static readonly Dictionary<string, Color> Known = new Dictionary<string, Color>
        {
            { "eat", new Color(0.35f, 0.85f, 0.25f) },
            { "flee", new Color(1f, 0.85f, 0.1f) },
            { "hide", new Color(0.2f, 0.45f, 1f) },
            { "follow", new Color(0.3f, 0.9f, 0.9f) },
            { "rest", new Color(0.6f, 0.6f, 0.6f) },
            { "mate", new Color(1f, 0.45f, 0.75f) },
            { "hunt", new Color(0.9f, 0.1f, 0.1f) },
        };

        /// <summary>Shown while an animal searches because its action had nothing to act on (ACT-04).</summary>
        public static readonly Color Searching = Color.white;
        /// <summary>Shown before an animal's first decision.</summary>
        public static readonly Color None = new Color(0.15f, 0.15f, 0.15f);

        public static Color Of(string action)
        {
            if (string.IsNullOrEmpty(action)) return None;
            if (Known.TryGetValue(action, out var c)) return c;
            int h = 0;
            foreach (char ch in action) h = h * 31 + ch;
            return Color.HSVToRGB((h & 0xffff) / 65536f, 0.7f, 0.95f);
        }

        /// <summary>The colour of an animal's current action (white while searching).</summary>
        public static Color Of(Animal a) => a.Searching ? Searching : Of(a.CurrentAction?.Name);
    }
}
