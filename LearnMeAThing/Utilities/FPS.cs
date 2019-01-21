using System;
using System.Diagnostics;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Helper for tracking the stats around FPS.
    /// 
    /// This is a bit tricky because when we _show_ a value
    ///    we are influencing the subsequent value.
    /// </summary>
    sealed class FPS
    {
        public int? AverageRenderTimeMilliseconds { get; private set; }
        public int? FramesPerSecond { get; private set; }

        private TimeSpan[] RenderTimes;

        private long? TrackingStart;
        private int FramesSinceStart;

        public FPS(int capacity)
        {
            RenderTimes = new TimeSpan[capacity];
        }

        public void PushRenderTime(TimeSpan elapsed)
        {
            for (var i = 0; i < RenderTimes.Length - 1; i++) RenderTimes[i] = RenderTimes[i + 1];
            RenderTimes[RenderTimes.Length - 1] = elapsed;
            UpdateAverageRenderTimeMilliseconds();
        }

        public void FrameFinished()
        {
            var now = Stopwatch.GetTimestamp();

            if (TrackingStart == null)
            {
                TrackingStart = now;
                FramesSinceStart = 0;
            }

            FramesSinceStart++;

            var change = now - TrackingStart;
            if(change >= Stopwatch.Frequency)
            {
                TrackingStart = null;
                FramesPerSecond = FramesSinceStart;
                FramesSinceStart = 0;
            }
        }

        private void UpdateAverageRenderTimeMilliseconds()
        {
            var total = TimeSpan.Zero;
            for(var i = 0; i < RenderTimes.Length; i++)
            {
                var val = RenderTimes[i];

                if (val == TimeSpan.Zero) return;

                total += val;
            }

            var avg = total.TotalSeconds / RenderTimes.Length;

            AverageRenderTimeMilliseconds = (int)Math.Round(avg, 0);
        }
    }
}
