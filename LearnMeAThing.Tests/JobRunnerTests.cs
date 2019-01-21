using LearnMeAThing.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class JobRunnerTests
    {
        [Fact]
        public void InParallel()
        {
            var startTicks = new long[4];
            var stopTicks = new long[4];
            var onThread = new int[4];
            var sums = new int[4];

            var _ = new GameState();
            using (var runner = new JobRunner(_, 4, 10))
            {
                runner.Initialize();

                // setup the jobs
                Job<int> j1, j2, j3, j4;
                {
                    j1 = runner.CreateJob(TestTask, 0);
                    j2 = runner.CreateJob(TestTask, 1);
                    j3 = runner.CreateJob(TestTask, 2);
                    j4 = runner.CreateJob(TestTask, 3);
                }

                // wait for them to finish
                var comp = runner.StartJobs(j1, j2, j3, j4);
                comp.WaitForCompletion();

                // no goofy time travel
                for (var i = 0; i < 4; i++)
                {
                    Assert.True(stopTicks[i] > startTicks[i]);
                }

                // ran on different thread
                Assert.False(onThread[0] == onThread[1]);
                Assert.False(onThread[0] == onThread[2]);
                Assert.False(onThread[0] == onThread[3]);
                Assert.False(onThread[1] == onThread[2]);
                Assert.False(onThread[1] == onThread[3]);
                Assert.False(onThread[2] == onThread[3]);

                // got the right answer
                Assert.Equal(49_995_000, sums[0]);
                Assert.Equal(49_995_000, sums[1]);
                Assert.Equal(49_995_000, sums[2]);
                Assert.Equal(49_995_000, sums[3]);   
            }

            // just a dumb task to do
            void TestTask(GameState ignore, int ix)
            {
                startTicks[ix] = Stopwatch.GetTimestamp();
                onThread[ix] = Thread.CurrentThread.ManagedThreadId;

                Thread.Sleep(10);
                for (var i = 0; i < 10_000; i++)
                {
                    sums[ix] += i;
                }

                stopTicks[ix] = Stopwatch.GetTimestamp();
            }
        }

        [Fact]
        public void Aggressive()
        {
            var observedToRun = 0;
            var rand = new Random(2018_11_27);
            var _ = new GameState();

            // more threads than tasks
            using (var runner = new JobRunner(_, 16, 100))
            {
                runner.Initialize();
                Stress(runner);
            }

            // fewer threads than tasks
            using (var runner = new JobRunner(_, 4, 100))
            {
                runner.Initialize();
                Stress(runner);
            }

            void Stress(JobRunner runner)
            {
                var jobs = Enumerable.Range(0, 100).Select(r => runner.CreateJob(TestTask, r)).ToArray();

                observedToRun = 0;
                var expectedToRun = 0;

                for (var i = 0; i < 10_000; i++)
                {
                    var numJobs = rand.Next(8) + 1;

                    var subset = jobs.OrderBy(o => Guid.NewGuid()).Take(numJobs).ToArray();

                    JobsCompletionToken tok;
                    switch (numJobs)
                    {
                        case 1: tok = runner.StartJobs(subset[0]); break;
                        case 2: tok = runner.StartJobs(subset[0], subset[1]); break;
                        case 3: tok = runner.StartJobs(subset[0], subset[1], subset[2]); break;
                        case 4: tok = runner.StartJobs(subset[0], subset[1], subset[2], subset[3]); break;
                        case 5: tok = runner.StartJobs(subset[0], subset[1], subset[2], subset[3], subset[4]); break;
                        case 6: tok = runner.StartJobs(subset[0], subset[1], subset[2], subset[3], subset[4], subset[5]); break;
                        case 7: tok = runner.StartJobs(subset[0], subset[1], subset[2], subset[3], subset[4], subset[5], subset[6]); break;
                        case 8: tok = runner.StartJobs(subset[0], subset[1], subset[2], subset[3], subset[4], subset[5], subset[6], subset[7]); break;
                        default: throw new Exception("wuh?");
                    }
                    expectedToRun += numJobs;

                    tok.WaitForCompletion();
                }

                Assert.Equal(expectedToRun, observedToRun);
            }

            // Just does a bunch of junk
            void TestTask(GameState ignore, int ix)
            {
                var sum = 0;

                for (var i = 0; i < 10_000; i++)
                {
                    sum += i;
                }

                Interlocked.Increment(ref observedToRun);
            }
        }

        [Fact]
        public void Reuse()
        {
            var onThread = new int[4];
            var sums = new int[4];

            var _ = new GameState();
            using (var runner = new JobRunner(_, 4, 10))
            {
                runner.Initialize();
                
                // setup the jobs
                Job<int> j1, j2, j3, j4;
                {
                    j1 = runner.CreateJob(TestTask, 0);
                    j2 = runner.CreateJob(TestTask, 1);
                    j3 = runner.CreateJob(TestTask, 2);
                    j4 = runner.CreateJob(TestTask, 3);
                }

                // wait for them to finish
                var comp = runner.StartJobs(j1, j2, j3, j4);
                comp.WaitForCompletion();

                // got the right answer
                Assert.Equal(49_995_000, sums[0]);
                Assert.Equal(49_995_000, sums[1]);
                Assert.Equal(49_995_000, sums[2]);
                Assert.Equal(49_995_000, sums[3]);

                // run them _again_
                var comp2 = runner.StartJobs(j1, j2, j3, j4);
                comp2.WaitForCompletion();

                // got the right answer, again
                Assert.Equal(2 * 49_995_000, sums[0]);
                Assert.Equal(2 * 49_995_000, sums[1]);
                Assert.Equal(2 * 49_995_000, sums[2]);
                Assert.Equal(2 * 49_995_000, sums[3]);
            }
            
            // just a dumb task to do
            void TestTask(GameState ignore, int ix)
            {
                for (var i = 0; i < 10_000; i++)
                {
                    sums[ix] += i;
                }
            }
        }
    }
}
