namespace EvoSim
{
    /// <summary>
    /// How serious a validation message is (21 §2): an error (E) blocks Play and the run; a warning (W) and an
    /// information (I) don't.
    /// </summary>
    public enum Severity
    {
        /// <summary>Information: worth knowing, nothing is wrong.</summary>
        Info,
        /// <summary>A warning: the run starts, but something probably isn't what you meant.</summary>
        Warning,
        /// <summary>An error: the World doesn't start (Initialize returns false) and Play is blocked.</summary>
        Error
    }
}
