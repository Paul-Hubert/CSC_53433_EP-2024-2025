namespace EvoSim
{
    /// <summary>A statistics row every statsEvery ticks and the periodic flushes (TICK-04 #11, RAND-22); live statistics for the editor.</summary>
    public class RecordPhase : TickPhase
    {
        public override void Run(TickContext t)
        {
            var recorder = World.Service<RunRecorder>();
            if (recorder != null)
            {
                if ((t.Tick + 1) % recorder.StatsEvery == 0) recorder.WriteStats(t.Tick + 1);
                recorder.AfterTick(t.Tick + 1);
            }
            World.Service<LiveStatistics>()?.Sample(t.Tick);
        }
    }
}
