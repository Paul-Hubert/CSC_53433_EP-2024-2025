using System.Text;

namespace EvoSim
{
    /// <summary>One problem, naming the object at fault (EDIT-02).</summary>
    public sealed class ValidationMessage
    {
        /// <summary>The validator id, e.g. V-01.</summary>
        public readonly string Id;
        public readonly Severity Severity;
        public readonly UnityEngine.Object Object;
        public readonly string Text;
        public readonly ValidationFix Fix;

        public ValidationMessage(string id, Severity severity, UnityEngine.Object obj, string text, ValidationFix fix)
        {
            Id = id; Severity = severity; Object = obj; Text = text; Fix = fix;
        }

        public override string ToString()
        {
            string where = Object == null ? "" : " [" + PathOf(Object) + "]";
            return $"{Id} {Severity}{where}: {Text}";
        }

        /// <summary>The hierarchy path of a scene object, for messages and logs.</summary>
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
