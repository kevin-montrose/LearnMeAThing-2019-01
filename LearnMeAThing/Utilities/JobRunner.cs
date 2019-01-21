using System;
using System.Threading;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Handles running pre-registered jobs across some number of threads.
    /// 
    /// The idea is:
    ///  - make a job runner with a certain number of threads and a maximum
    ///      number of jobs
    ///  - register a bunch of jobs, getting Job(T)'s back
    ///  - (time passes)
    ///  - provide some number of Job(T)'s back, those are scheduled 
    ///       and a JobsCompletionToken is returned
    ///  - caller waits on JobsCompletitonToken, once it's done
    ///       then all the tasks finished up
    /// </summary>
    sealed class JobRunner : IDisposable
    {
        private readonly GameState State;

        private readonly Thread[] Threads;
        
        private volatile bool KeepAlive;
        
        /// <summary>
        /// Sync lock for parking and waking
        ///   threads.
        /// </summary>
        private readonly object Lock;

        /// <summary>
        /// A number that is incremented everytime a PendingJob is enqueued.
        /// </summary>
        private int Generation;
        /// <summary>
        /// Jobs that a thread hasn't picked up to work on yet,
        ///    but are associated with something in
        ///    PendingCompletionToken.
        /// </summary>
        private readonly IJob[] PendingJobs;

        /// <summary>
        /// Tokens that aren't assigned to any jobs
        /// </summary>
        private readonly JobsCompletionToken[] AvailableCompletionTokens;

        /// <summary>
        /// Tokens that are assigned to a job.
        /// </summary>
        private readonly JobsCompletionToken[] PendingCompletionTokens;

        /// <summary>
        /// Creates a new JobRunner associated with the given GameState.
        /// 
        /// Create `numThreads` new threads for running jobs, it's ill
        ///   advised to create more threads than you have logical cores
        ///   so be careful.
        ///   
        /// `maxSimultaneousJobs` is used to pre-allocate completion tokens,
        ///   exceeding it can result in blocking during StartJobs(...) calls.
        /// </summary>
        public JobRunner(GameState state, int numThreads, int maxSimultaneousJobs)
        {
            Lock = new object();

            State = state;

            Threads = new Thread[numThreads];

            PendingJobs = new IJob[maxSimultaneousJobs];

            AvailableCompletionTokens = new JobsCompletionToken[maxSimultaneousJobs];
            PendingCompletionTokens = new JobsCompletionToken[maxSimultaneousJobs];
        }

        /// <summary>
        /// Actually spin up all the associated threads and whatnot.
        /// 
        /// After this call, no operation should allocate.
        /// </summary>
        public void Initialize()
        {
            KeepAlive = true;

            for (var i = 0; i < Threads.Length; i++)
            {
                var t = new Thread(WorkerThreadLoop);
                t.IsBackground = true;
                t.Name = $"{nameof(JobRunner)} Thread Index={i:N0}";

                Threads[i] = t;

                t.Start();
            }

            for (var i = 0; i < AvailableCompletionTokens.Length; i++)
            {
                var token = new JobsCompletionToken(this, 8);   // 8 is the maximum number of parameters to StartJobs(...)
                AvailableCompletionTokens[i] = token;
            }
        }

        /// <summary>
        /// Create a job from some constant state and a delegate.
        /// 
        /// The current game state will be available at run time.
        /// 
        /// Jobs should be re-used, not allocated on demand.
        /// </summary>
        public Job<T> CreateJob<T>(JobDelegate<T> runner, T state)
        {
            var ret = new Job<T>(runner, state);
            return ret;
        }

        /// <summary>
        /// Start up to 8 jobs in parallel.
        /// 
        /// This method will return "soon" with a completion token
        ///   to wait on.
        ///   
        /// No work will begin until the WaitForCompletion() is called
        ///   on the returned token.
        ///   
        /// At least the first job must be non-null, all other jobs are
        ///    optional.
        ///    
        /// Blocking can occur if completion tokens aren't immediately
        ///   available, which shouldn't happen(?).
        /// </summary>
        public JobsCompletionToken StartJobs(IJob job1, IJob job2 = null, IJob job3 = null, IJob job4 = null, IJob job5 = null, IJob job6 = null, IJob job7 = null, IJob job8 = null)
        {
            // heeeeey, that should at least be non-null
            if (job1 == null) throw new ArgumentNullException(nameof(job1));

            var token = GetCompletionToken();

            // prepare jobs
            job1.Reset();   
            job2?.Reset();
            job3?.Reset();
            job4?.Reset();
            job5?.Reset();
            job6?.Reset();
            job7?.Reset();
            job8?.Reset();

            // attach jobs to the token
            token.AddJob(job1);
            token.AddJob(job2);
            token.AddJob(job3);
            token.AddJob(job4);
            token.AddJob(job5);
            token.AddJob(job6);
            token.AddJob(job7);
            token.AddJob(job8);

            // record the completion token
            EnqueuePendingCompletionToken(token);
            
            // you can now wait on the token
            return token;
        }

        /// <summary>
        /// Place the given JobsCompletionToken back into our reusable buffer.
        /// 
        /// Also resets the token, so any state it had is lst
        /// </summary>
        internal void ReturnCompletionToken(JobsCompletionToken token)
        {
            token.Reset();

            // this _cannot_ fail
            tryAgain:
            for (var i = 0; i < AvailableCompletionTokens.Length; i++)
            {
                var cur = AvailableCompletionTokens[i];
                if (cur != null) continue;

                var res = Interlocked.CompareExchange(ref AvailableCompletionTokens[i], token, null);
                if (res == null)
                {
                    return;
                }
            }

            goto tryAgain;
        }

        /// <summary>
        /// Returns a JobsCompletionToken that is free to assign jobs to.
        /// </summary>
        private JobsCompletionToken GetCompletionToken()
        {
            // this cannot fail
            tryAgain:
            for (var i = 0; i < AvailableCompletionTokens.Length; i++)
            {
                var ret = AvailableCompletionTokens[i];
                if (ret == null) continue;

                var res = Interlocked.CompareExchange(ref AvailableCompletionTokens[i], null, ret);

                if (ReferenceEquals(ret, res)) return ret;
            }

            goto tryAgain;
        }

        /// <summary>
        /// Checks for any pending jobs, and removes and returns one if
        ///   there are any.
        ///   
        /// If there are no jobs, returns null.
        /// </summary>
        /// <returns></returns>
        private IJob GetJob()
        {
            tryAgain:
            var curGen = Generation;
            for (var i = 0; i < PendingJobs.Length; i++)
            {
                var ret = PendingJobs[i];
                if (ret == null) continue;
                
                var res = Interlocked.CompareExchange(ref PendingJobs[i], null, ret);

                if (ReferenceEquals(ret, res))
                {
                    return ret;
                }
            }

            var finalGen = Generation;

            if(curGen != finalGen)
            {
                // the list was modified while we were running
                goto tryAgain;
            }
            
            return null;
        }

        /// <summary>
        /// Add this token to the queue of "we're working on it" tokens.
        /// 
        /// It will be removed when the last job completes.
        /// </summary>
        private void EnqueuePendingCompletionToken(JobsCompletionToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            // this _cannot_ fail
            tryAgain:
            for (var i = 0; i < PendingCompletionTokens.Length; i++)
            {
                var cur = PendingCompletionTokens[i];
                if (cur != null) continue;

                var res = Interlocked.CompareExchange(ref PendingCompletionTokens[i], token, null);
                if (res == null)
                {
                    return;
                }
            }

            goto tryAgain;
        }

        /// <summary>
        /// Add a job to a queue indicating that it needs to be run.
        /// 
        /// Jobs should be in a state such that immediately beginning
        ///   executing is correct.
        /// </summary>
        internal void EnqueueJob(IJob job)
        {
            // it's easier in some places to just try and add a 
            //   maybe-null job, just roll with it
            if (job == null) return;
            
            // this _cannot_ fail
            tryAgain:
            for (var i = 0; i < PendingJobs.Length; i++)
            {
                var cur = PendingJobs[i];
                if (cur != null) continue;
                
                // kick the generation up, so anybody who's live at this point knows to rescan
                Interlocked.Increment(ref Generation);
                var res = Interlocked.CompareExchange(ref PendingJobs[i], job, null);

                if (res == null)
                {
                    return;
                }
            }
            
            goto tryAgain;
        }
        
        /// <summary>
        /// Spin loop for worker threads.
        /// 
        /// Only returns if KeepAlive == false,
        ///   parks itself if there's nothing to do.
        /// </summary>
        private void WorkerThreadLoop()
        {
            while(KeepAlive)
            {
                var job = GetJob();
                if(job != null)
                {
                    job.Run(State);
                    CheckPendingCompletionTokens();
                    continue;
                }

                ParkWorkerThread();
            }
        }

        /// <summary>
        /// Runs through all pending completion tokens,
        ///   and if they're complete tries to remove 
        ///   and signal them.
        /// </summary>
        private void CheckPendingCompletionTokens()
        {
            for(var i = 0; i < PendingCompletionTokens.Length; i++)
            {
                var tok = PendingCompletionTokens[i];
                if (tok == null) continue;

                if (tok.IsComplete)
                {
                    var res = Interlocked.CompareExchange(ref PendingCompletionTokens[i], null, tok);

                    if(ReferenceEquals(res, tok))
                    {
                        tok.SignalJobCompleted();
                    }
                }
            }
        }

        /// <summary>
        /// Cause all worker threads to wake up, and go try
        ///   and grab a job.
        /// </summary>
        internal void WakeUpWorkerThreads()
        {
            lock (Lock)
            {
                Monitor.PulseAll(Lock);
            }
        }

        /// <summary>
        /// Parks the current thread until WakeUpWorkerThreads()
        ///   is called.
        ///   
        /// Does nothing if KeepAlive == false
        /// </summary>
        private void ParkWorkerThread()
        {
            if (!KeepAlive) return;

            lock (Lock)
            {
                Monitor.Wait(Lock);
            }
        }

        public void Dispose()
        {
            KeepAlive = false;
            
            tryAgain:
            WakeUpWorkerThreads();
            for (var i = 0; i < Threads.Length; i++)
            {
                var t = Threads[i];
                t.Join(100);
                if (t.IsAlive)
                {
                    goto tryAgain;
                }
            }
        }
    }
}
