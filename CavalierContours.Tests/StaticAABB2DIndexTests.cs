using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CavalierContours.Spatial;
using Xunit;

namespace CavalierContours.Tests
{
    public class StaticAABB2DIndexTests
    {
        /// <summary>
        /// Runs <paramref name="action"/> on a worker thread so a non-terminating constructor
        /// surfaces as a failed assertion rather than freezing the whole test run.
        /// </summary>
        private static async Task<Exception?> CaptureWithinAsync(Action action, int timeoutMs = 5000)
        {
            Exception? captured = null;
            var worker = Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    captured = e;
                }
            });

            var finished = await Task.WhenAny(worker, Task.Delay(timeoutMs)).ConfigureAwait(false);
            Assert.True(ReferenceEquals(finished, worker), "operation did not terminate within the timeout");
            return captured;
        }

        [Fact]
        public async Task BuilderRejectsNegativeCount()
        {
            var ex = await CaptureWithinAsync(() => new StaticAABB2DIndexBuilder<double>(-1));

            var argEx = Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("count", argEx.ParamName);
        }

        [Fact]
        public async Task BuilderRejectsNegativeCountWithExplicitNodeSize()
        {
            var ex = await CaptureWithinAsync(() => new StaticAABB2DIndexBuilder<double>(-5, 16));

            Assert.IsType<ArgumentOutOfRangeException>(ex);
        }

        [Fact]
        public void BuilderAcceptsZeroItems()
        {
            var index = new StaticAABB2DIndexBuilder<double>(0).Build();

            Assert.Equal(0, index.Count);
            Assert.Null(index.Bounds);
            Assert.Empty(index.Query(-1e9, -1e9, 1e9, 1e9));
        }

        [Fact]
        public void BuildTwiceThrowsAClearError()
        {
            var builder = new StaticAABB2DIndexBuilder<double>(2);
            builder.Add(0.0, 0.0, 1.0, 1.0);
            builder.Add(2.0, 2.0, 3.0, 3.0);

            var first = builder.Build();
            Assert.Equal(2, first.Count);

            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("already", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void VisitNeighborsIsDeterministicAcrossRuns()
        {
            // Four boxes at exactly the same distance from the query point. The visit order must
            // not depend on incidental heap layout.
            var builder = new StaticAABB2DIndexBuilder<double>(4);
            builder.Add(-1.0, -1.0, -1.0, -1.0);
            builder.Add(1.0, -1.0, 1.0, -1.0);
            builder.Add(1.0, 1.0, 1.0, 1.0);
            builder.Add(-1.0, 1.0, -1.0, 1.0);
            var index = builder.Build();

            var firstRun = new List<int>();
            var v1 = new DelegateNeighborVisitor<double>((i, _) => { firstRun.Add(i); return true; });
            index.VisitNeighbors(0.0, 0.0, ref v1);

            var secondRun = new List<int>();
            var v2 = new DelegateNeighborVisitor<double>((i, _) => { secondRun.Add(i); return true; });
            index.VisitNeighbors(0.0, 0.0, ref v2);

            Assert.Equal(4, firstRun.Count);
            Assert.Equal(firstRun, secondRun);
        }

        [Fact]
        public void QueryFindsAllOverlappingBoxes()
        {
            var builder = new StaticAABB2DIndexBuilder<double>(3);
            builder.Add(0.0, 0.0, 1.0, 1.0);
            builder.Add(5.0, 5.0, 6.0, 6.0);
            builder.Add(0.5, 0.5, 5.5, 5.5);
            var index = builder.Build();

            var hits = index.Query(0.0, 0.0, 1.0, 1.0);
            hits.Sort();
            Assert.Equal(new[] { 0, 2 }, hits);
        }
    }
}
