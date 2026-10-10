using UnityEditor;
using UnityEngine;

namespace EvoSim.Editor
{
    /// <summary>
    /// The validation list of the inspectors (EDIT-01, EDIT-02): one line per message with its severity, a button that
    /// selects the object at fault, and the fix button when the validator offers one.
    /// </summary>
    public static class ValidationPanel
    {
        /// <summary>Draws a report; returns true when a fix was applied (the caller validates again).</summary>
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
        public static ValidationReport Validate(World w)
        {
            if (Application.isPlaying && w.IsInitialized) return w.LastReport;
            var report = new ValidationReport();
            w.Prepare(report);
            return report;
        }
    }
}
