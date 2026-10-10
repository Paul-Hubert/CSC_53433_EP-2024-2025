using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The scene view (21 §5): gizmos for the selected animal (vision, a line to its target coloured by action, its
    /// reach) and, as the overlay's toggles say, food items, cover, carcasses, eggs and a letter per action above each
    /// animal (stable colours from the action order). Drawing only reads the world (SPACE-12).
    /// </summary>
    [InitializeOnLoad]
    public static class SceneDrawing
    {
        public const int MaxMarks = 6000;

        static SceneDrawing() => SceneView.duringSceneGui += Draw;

        public static bool Food { get => EditorPrefs.GetBool("EvoSim.Overlay.Food", true); set => EditorPrefs.SetBool("EvoSim.Overlay.Food", value); }
        public static bool CoverCells { get => EditorPrefs.GetBool("EvoSim.Overlay.Cover", true); set => EditorPrefs.SetBool("EvoSim.Overlay.Cover", value); }
        public static bool Carcasses { get => EditorPrefs.GetBool("EvoSim.Overlay.Carcasses", true); set => EditorPrefs.SetBool("EvoSim.Overlay.Carcasses", value); }
        public static bool Eggs { get => EditorPrefs.GetBool("EvoSim.Overlay.Eggs", true); set => EditorPrefs.SetBool("EvoSim.Overlay.Eggs", value); }
        public static bool ActionLetters { get => EditorPrefs.GetBool("EvoSim.Overlay.Actions", true); set => EditorPrefs.SetBool("EvoSim.Overlay.Actions", value); }
        public static string HiddenSpecies { get => EditorPrefs.GetString("EvoSim.Overlay.Hidden", ""); set => EditorPrefs.SetString("EvoSim.Overlay.Hidden", value); }

        static void Draw(SceneView view)
        {
            if (!Application.isPlaying) return;
            var w = EditorWorlds.Current();
            if (w == null || !w.IsInitialized) return;
            if (Event.current.type != EventType.Repaint) return;
            int marks = 0;
            foreach (var service in w.Services)
            {
                if (Food && service is FoodGrid food && food.Grid != null)
                {
                    Handles.color = new Color(0.3f, 0.9f, 0.3f, 0.8f);
                    for (int c = 0; c < food.Grid.Count && marks < MaxMarks; c++)
                        if (food.HasItem(c)) { Mark(w, food.Grid.Center(c), food.Grid.CellSize * 0.2f); marks++; }
                }
                if (CoverCells && service is CoverLayer cover && cover.Grid != null)
                {
                    Handles.color = new Color(0.1f, 0.4f, 0.1f, 0.6f);
                    for (int c = 0; c < cover.Grid.Count && marks < MaxMarks; c++)
                    {
                        var p = cover.Grid.Center(c);
                        if (cover.InCover(p)) { Handles.DrawWireCube(OnGround(w, p), new Vector3(cover.Grid.CellSize, 0.05f, cover.Grid.CellSize)); marks++; }
                    }
                }
                if (Carcasses && service is CarcassSystem carcasses)
                {
                    Handles.color = new Color(0.6f, 0.35f, 0.2f);
                    foreach (var e in carcasses.Entities) Handles.DrawWireDisc(OnGround(w, e.Position), Vector3.up, 0.5f);
                }
                if (Eggs && service is EggSystem eggs)
                {
                    Handles.color = Color.yellow;
                    foreach (var e in eggs.Entities) Handles.DrawWireDisc(OnGround(w, e.Position), Vector3.up, 0.25f);
                }
            }
            string hidden = HiddenSpecies;
            if (ActionLetters)
            {
                var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                foreach (var s in w.AllSpecies)
                {
                    if (hidden.Contains("|" + s.Id + "|")) continue;
                    foreach (var a in s.Animals)
                    {
                        if (a.IsGone || a.CurrentAction == null || marks++ > MaxMarks) continue;
                        style.normal.textColor = EditorWorlds.ActionColor(a.Action);
                        Handles.Label(OnGround(w, a.Position) + Vector3.up * 1.2f, a.CurrentAction.Name.Substring(0, 1).ToUpperInvariant(), style);
                    }
                }
            }
            Selected(w);
        }

        /// <summary>The selected animal: vision, a line to its target in its action's colour, its reach.</summary>
        static void Selected(World w)
        {
            var a = EditorWorlds.SelectedAnimal(out var world);
            if (a == null || world != w) return;
            var p = OnGround(w, a.Position);
            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawWireDisc(p, Vector3.up, w.Queries.Vision(a));
            Handles.color = EditorWorlds.ActionColor(a.Action);
            Handles.DrawWireDisc(p, Vector3.up, 1f);
            if (a.TargetId >= 0 || a.TargetPoint != Vector3.zero) Handles.DrawAAPolyLine(3f, p, OnGround(w, a.TargetPoint));
        }

        static void Mark(World w, Vector3 p, float size) => Handles.DrawWireDisc(OnGround(w, p), Vector3.up, size);

        static Vector3 OnGround(World w, Vector3 p) => w.Ground != null ? w.Ground.OnGround(p) + Vector3.up * 0.05f : p;
    }
}
