namespace EvoSim
{
    /// <summary>A statistics row every statsEvery ticks and the periodic flushes (TICK-04 #11, RAND-22); live statistics for the editor.</summary>
    /// <remarks>
    /// Last phase of the reference tick. It reads the world and changes nothing in it. Rows and flushes are labelled with
    /// the tick count after this tick (<c>t.Tick + 1</c>): the row <c>t = 100</c> is written at the end of tick 99, once
    /// 100 ticks are done. Without a <see cref="RunRecorder"/> or <see cref="LiveStatistics"/> it does nothing.
    /// </remarks>
    public class RecordPhase : TickPhase
    {
        /// <summary>Writes the statistics row when due, lets the recorder flush, and samples the live statistics.</summary>
        /// <inheritdoc/>
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
