namespace EvoSim
{
    /// <summary>
    /// Common animal words, for the stale-name check V-23 (SPEC-21): a founder sentence naming one of them that is no
    /// species of the world was probably written for another world, or before a rename.
    /// </summary>
    public static class AnimalWords
    {
        static readonly string[] Words =
        {
            "wolf", "wolves", "fox", "foxes", "rabbit", "rabbits", "hare", "hares", "deer", "bear", "bears", "lion", "lions",
            "tiger", "tigers", "hawk", "hawks", "owl", "owls", "eagle", "eagles", "snake", "snakes", "cat", "cats", "dog", "dogs",
            "mouse", "mice", "rat", "rats", "sheep", "goat", "goats", "cow", "cows", "horse", "horses", "boar", "boars", "lynx",
            "weasel", "weasels", "squirrel", "squirrels", "badger", "badgers", "coyote", "coyotes", "frog", "frogs", "bird", "birds",
        };

        /// <summary>Whether a lower-case word is one of them.</summary>
        public static bool Contains(string word) => System.Array.IndexOf(Words, word) >= 0;
    }
}
