using System;
using System.Threading;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Interface for a re-runnable job, invoked
    ///   as part of a scheduled
    /// </summary>
    delegate void JobDelegate<T>(GameState game, T state);

    /// <summary>
    /// Public interface for a Job
    /// </summary>
    interface IJob
    {
        bool IsComplete { get; }
        void Run(GameState state);
        void Reset();
    }

    /// <summary>
    /// Represents a re-runnable job.
    /// 
    /// It is the responsiblity of the constructor
    ///   to make sure the job doesn't conflict with
    ///   any other job that may run at the same time.
    /// </summary>
    sealed class Job<T> : IJob
    {
        /// <summary>
        /// If true, this job has finished running.
        /// </summary>
        public bool IsComplete { get; private set; }

        private readonly JobDelegate<T> Runner;
        public readonly T State;

        internal Job(JobDelegate<T> runner, T state)
        {
            IsComplete = false;
            Runner = runner;
            State = state;
        }

        /// <summary>
        /// Make this job ready to run again.
        /// </summary>
        public void Reset()
        {
            IsComplete = false;
        }

        public void Run(GameState game)
        {
            if (IsComplete) throw new InvalidOperationException("Tried to run an already complete job");

            // actually do the work
            Runner(game, State);

            // signal that we're done
            lock (this)
            {
                // can only modify this in the lock
                IsComplete = true;

                // wake up the _single_ thread 
                //   waiting on this job to complete
                Monitor.Pulse(this);
            }
        }
    }
}
