using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The validation list of the inspectors (EDIT-01, EDIT-02): one line per message with its severity, a button that
    /// selects the object at fault, and the fix button when the validator offers one.
    /// </summary>
    /// <remarks>
    /// <para>Each line reads <c>V-xx  text</c> in a help box coloured by severity (error, warning, information). To its right,
    /// <c>Select</c> selects and pings the object at fault, and a second button, labelled with the fix (<c>add Rest</c>,
    /// <c>sort</c>, <c>clamp</c>…), applies it with Undo.</para>
    /// <para>To offer a fix for a check of your own module, pass a <see cref="ValidationFix"/> to
    /// <see cref="ValidationReport.Error"/> or <see cref="ValidationReport.Warning"/>: this panel shows its button.</para>
    /// </remarks>
    public static class ValidationPanel
    {
        /// <summary>Draws a report; returns true when a fix was applied (the caller validates again).</summary>
        /// <param name="report">The report to draw, or null: "Not validated yet.".</param>
        /// <returns>True when a fix button was pressed and its fix applied this frame.</returns>
        public static bool Draw(ValidationReport report)
        {
            if (report == null) { EditorGUILayout.HelpBox("Not validated yet.", MessageType.None); return false; }
            if (report.Messages.Count == 0) { EditorGUILayout.HelpBox("Validation is green.", MessageType.Info); return false; }
            bool fixedOne = false;
            foreach (var m in report.Messages)
            {
                var type = m.Severity == Severity.Error ? MessageType.Error : m.Severity == Severity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"{m.Id}  {m.Text}", type);
                EditorGUILayout.BeginVertical(GUILayout.Width(60));
                if (m.Object != null && GUILayout.Button("Select"))
                {
                    Selection.activeObject = m.Object;
                    EditorGUIUtility.PingObject(m.Object);
                }
                if (m.Fix != null && GUILayout.Button(m.Fix.Label))
                {
                    if (m.Object != null) Undo.RecordObject(m.Object, m.Fix.Label);
                    m.Fix.Apply();
                    if (m.Object != null) EditorUtility.SetDirty(m.Object);
                    fixedOne = true;
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
            return fixedOne;
        }

        /// <summary>Validates a world with the engine World.Initialize uses (outside Play mode it prepares the world).</summary>
        /// <param name="w">The world to validate.</param>
        /// <returns>In Play mode, once the world runs, the report of its initialisation; otherwise a fresh report from
        /// <see cref="World.Prepare"/>.</returns>
        public static ValidationReport Validate(World w)
        {
            if (Application.isPlaying && w.IsInitialized) return w.LastReport;
            var report = new ValidationReport();
            w.Prepare(report);
            return report;
        }
    }
}
