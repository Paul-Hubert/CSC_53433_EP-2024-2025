namespace EvoSim
{
    /// <summary>Receives every recorded event with its canonical line (OUT-06): the run recorder, live graphs, tests.</summary>
    public interface IEventSink
    {
        void OnEvent(SimEvent e, string line);
    }
}
