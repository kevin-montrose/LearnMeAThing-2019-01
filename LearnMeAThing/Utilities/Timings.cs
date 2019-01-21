using LearnMeAThing.Systems;
using System;
using System.Diagnostics;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Struct for tracking the performance of the systems.
    /// </summary>
    struct Timings
    {
        public struct TimingBlock : IDisposable
        {
            private Stopwatch Watch;
            private long[] Timings;
            public TimingBlock(long[] timings, Stopwatch watch)
            {
                Timings = timings;
                Watch = watch;

                // move everything forward 1
                Array.Copy(Timings, 1, Timings, 0, Timings.Length - 1);
                Timings[Timings.Length - 1] = watch.ElapsedTicks;
            }

            public void Dispose()
            {
                var end = Watch.ElapsedTicks;

                Timings[Timings.Length - 1] = end - Timings[Timings.Length - 1];

                Timings = null;
                Watch = null;
            }
        }

        public int NumPointsTracked => Elapsed[0].Length;
        public int NumSystemsTracked => Elapsed.Length;

        private readonly Stopwatch Watch;
        private readonly long[][] Elapsed;

        public Timings(int numForEachSystem)
        {
            Watch = Stopwatch.StartNew();

            var max = -1;
            foreach(SystemType type in Enum.GetValues(typeof(SystemType)))
            {
                var asInt = (int)type;
                if(asInt > max)
                {
                    max = asInt;
                }
            }

            Elapsed = new long[max + 1][];

            for(var i =0;i < Elapsed.Length; i++)
            {
                Elapsed[i] = new long[numForEachSystem];
            }
        }
        
        /// <summary>
        /// Returns the last N timings for a given system.
        /// 
        /// Older timings occur earlier in the returned result.
        /// 
        /// It is not valid to call this method while a system is
        ///    in the act of being timed.
        /// </summary>
        public long[] GetTicksForSystem(SystemType type) => Elapsed[(int)type];

        /// <summary>
        /// Returns a block that will track the runtime for the given system.
        /// 
        /// Note that only one timing block can be active for a system, 
        ///   and you cannot read the ticks for a system while it is being timed.
        /// </summary>
        public TimingBlock Time(SystemType type)
        {
            var arr = Elapsed[(int)type];

            return new TimingBlock(arr, Watch);
        }
    }
}
