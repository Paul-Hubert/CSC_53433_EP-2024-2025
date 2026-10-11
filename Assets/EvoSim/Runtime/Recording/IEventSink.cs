namespace EvoSim
{
    /// <summary>Receives every recorded event with its canonical line (OUT-06): the run recorder, live graphs, tests.</summary>
    /// <remarks>
    /// Register with <see cref="EventLog.AddSink"/> on <see cref="World.Events"/>, after the World initialised (each run has a
    /// new log). A sink only reads: it must not record events or change the world from <see cref="OnEvent"/>.
    /// </remarks>
    public interface IEventSink
    {
        /// <summary>Called by the event log for each event, right after it is added to the hash.</summary>
        /// <param name="e">The event, with its kind, tick, id, species and fields.</param>
        /// <param name="line">Its canonical JSON line, as hashed and as written to <c>events.jsonl</c> (outside the compatibility mode).</param>
        void OnEvent(SimEvent e, string line);
    }
}
