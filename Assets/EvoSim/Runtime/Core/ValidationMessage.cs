using System.Text;

namespace EvoSim
{
    /// <summary>One problem, naming the object at fault (EDIT-02).</summary>
    public sealed class ValidationMessage
    {
        /// <summary>The validator id, e.g. V-01.</summary>
        public readonly string Id;
        /// <summary>How serious it is: an error blocks the run.</summary>
        public readonly Severity Severity;
        /// <summary>The object at fault (a component, a GameObject), selected when the message is clicked; may be null.</summary>
        public readonly UnityEngine.Object Object;
        /// <summary>What is wrong, in a sentence that names the rule (e.g. "… (ACT-01).").</summary>
        public readonly string Text;
        /// <summary>A fix the editor offers as a button, or null.</summary>
        public readonly ValidationFix Fix;

        /// <summary>Makes a message; <see cref="ValidationReport.Add"/> makes them.</summary>
        /// <param name="id">The validator id, e.g. <c>"V-01"</c>.</param>
        /// <param name="severity">How serious it is.</param>
        /// <param name="obj">The object at fault, or null.</param>
        /// <param name="text">What is wrong.</param>
        /// <param name="fix">A fix, or null.</param>
        public ValidationMessage(string id, Severity severity, UnityEngine.Object obj, string text, ValidationFix fix)
        {
            Id = id; Severity = severity; Object = obj; Text = text; Fix = fix;
        }

        /// <summary>The message on one line, for logs and test failures.</summary>
        /// <returns>The text <c>"&lt;id&gt; &lt;severity&gt; [&lt;object's path&gt;]: &lt;text&gt;"</c>.</returns>
        public override string ToString()
        {
            string where = Object == null ? "" : " [" + PathOf(Object) + "]";
            return $"{Id} {Severity}{where}: {Text}";
        }

        /// <summary>The hierarchy path of a scene object, for messages and logs.</summary>
        /// <param name="o">A component, a GameObject or another object.</param>
        /// <returns>The path from the root, e.g. <c>"Lab1 World/Prey/Actions/eat"</c>; the object's name for an asset;
        /// empty for null.</returns>
        public static string PathOf(UnityEngine.Object o)
        {
            if (o is UnityEngine.Component c) o = c.gameObject;
            if (!(o is UnityEngine.GameObject go)) return o != null ? o.name : "";
            var sb = new StringBuilder(go.name);
            for (var t = go.transform.parent; t != null; t = t.parent) sb.Insert(0, t.name + "/");
            return sb.ToString();
        }
    }
}
