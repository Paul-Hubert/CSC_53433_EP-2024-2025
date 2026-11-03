namespace EvoSim.Editor
{
    /// <summary>
    /// An extra section of the Animal Inspector (21 §4): derive from it in any editor assembly, with a constructor without
    /// parameters, and the window draws it under the decision for the animals it shows. The neural species' panel (plan
    /// M16 §5) is one.
    /// </summary>
    /// <remarks>
    /// The window makes one instance of each panel class and keeps it. Panels only read the animal and its world
    /// (SPACE-12): a run gives the same results with the window open or closed.
    /// </remarks>
    public abstract class AnimalInspectorPanel
    {
        /// <summary>The section's title, drawn in bold above it; panels are drawn in title order.</summary>
        public abstract string Title { get; }

        /// <summary>Whether this panel has something to show for the animal.</summary>
        /// <param name="a">The selected animal.</param>
        /// <returns>True to draw the panel.</returns>
        public abstract bool Shows(Animal a);

        /// <summary>Draws the panel with <c>EditorGUILayout</c>, inside the window's scroll view.</summary>
        /// <param name="a">The selected animal.</param>
        /// <param name="w">Its world.</param>
        public abstract void Draw(Animal a, World w);
    }
}
