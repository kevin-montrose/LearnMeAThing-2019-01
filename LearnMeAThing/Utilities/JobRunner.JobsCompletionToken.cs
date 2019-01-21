using System;
using System.Threading;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// An object representing some number of jobs waiting to be completed.
    /// 
    /// Once WaitForCompletion() returns, references to this object should be 
    ///    dropped as it may be handed out by it's associated JobRunner again.
    /// </summary>
    sealed class JobsCompletionToken
    {
        private readonly object Lock;

        private int ActiveJobs;
        private readonly IJob[] AssociatedJobs;

        private readonly JobRunner Owner;
        
        /// <summary>
        /// Returns true if all the associated jobs
        ///   of this completion token have completed
        /// </summary>
        internal bool IsComplete
        {
            get
            {
                for (var i = 0; i < ActiveJobs; i++)
                {
                    var job = AssociatedJobs[i];
                    
                    // we recycled the token, so this thing is incomplete
                    if (job == null) return false;

                    // any single job being incomplete means the whole
                    //     token is incomplete
                    if (!job.IsComplete) return false;
                }

                return true;
            }
        }

        internal JobsCompletionToken(JobRunner owner, int maximumJobs)
        {
            Lock = new object();

            Owner = owner;
            AssociatedJobs = new IJob[maximumJobs];
            ActiveJobs = 0;
        }

        /// <summary>
        /// Make this completion token ready for use again
        /// </summary>
        internal void Reset()
        {
            ActiveJobs = 0;
            Array.Clear(AssociatedJobs, 0, AssociatedJobs.Length);
        }

        /// <summary>
        /// Attach a job to this token.
        /// 
        /// This does not start the job, it just makes
        ///    it's completion a condition for WaitForCompletion()'s
        ///    return.
        /// </summary>
        internal void AddJob(IJob job)
        {
            // just ignore it
            if (job == null) return;

            if (ActiveJobs == AssociatedJobs.Length) throw new InvalidOperationException($"Tried to add a job to a full {nameof(JobsCompletionToken)}");

            AssociatedJobs[ActiveJobs] = job;
            ActiveJobs++;
        }

        /// <summary>
        /// Called to indicate that a single job has completed,
        ///   and that might indicate that the whole completion
        ///   token is now complete.
        /// </summary>
        internal void SignalJobCompleted()
        {
            lock (Lock)
            {
                Monitor.Pulse(Lock);
            }
        }
        
        /// <summary>
        /// Blocks until all the jobs associated with this runner are
        ///   complete.
        /// </summary>
        public void WaitForCompletion()
        {
            lock (Lock)
            {
                // kick everything off
                //   need to do this in the lock
                //   so there's no race to call
                //   SignalJobCompleted()
                for(var i = 0; i < ActiveJobs; i++)
                {
                    Owner.EnqueueJob(AssociatedJobs[i]);
                }

                Owner.WakeUpWorkerThreads();

                while (!IsComplete)
                {
                    Monitor.Wait(Lock);
                }
            }
            
            Owner.ReturnCompletionToken(this);
        }
    }
}
