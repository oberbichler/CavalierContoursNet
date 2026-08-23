using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of the upstream Rust integration test file
    /// <c>cavalier_contours/tests/test_pline_basics.rs</c> (tag 0.7.0).
    /// One <see cref="FactAttribute"/> per Rust <c>#[test]</c>, expected values taken verbatim
    /// from the Rust source.
    /// </summary>
    public class PlineBasicsTests
    {
        private const double Eps = 1e-5;

        /// <summary>
        /// Emulates the traversal protocol of a Rust <c>DoubleEndedIterator</c> +
        /// <c>ExactSizeIterator</c> over the sequence produced by the library, so that the
        /// <c>size_hint</c>/<c>next</c>/<c>next_back</c> assertions of the Rust test can be
        /// ported one to one. The sequence itself always comes from the library.
        /// </summary>
        private sealed class DoubleEndedVertexIter
        {
            private readonly List<PlineVertex<double>> _items;
            private int _front;
            private int _back;

            public DoubleEndedVertexIter(IEnumerable<PlineVertex<double>> source)
            {
                _items = new List<PlineVertex<double>>(source);
                _front = 0;
                _back = _items.Count;
            }

            /// <summary>Equivalent of the Rust <c>size_hint()</c> lower == upper bound.</summary>
            public int SizeHint => _back - _front;

            public PlineVertex<double>? Next()
            {
                if (_front >= _back) return null;
                return _items[_front++];
            }

            public PlineVertex<double>? NextBack()
            {
                if (_front >= _back) return null;
                return _items[--_back];
            }
        }

        private static void AssertFuzzyEq(PlineVertex<double> expected, PlineVertex<double> actual)
        {
            Assert.True(actual.FuzzyEq(expected), $"expected {expected} but got {actual}");
        }

        private static void AssertSome(PlineVertex<double> expected, PlineVertex<double>? actual)
        {
            Assert.Equal<PlineVertex<double>?>(expected, actual);
        }

        /// <summary>
        /// Equivalent of the Rust <c>assert_eq!(iter.size_hint(), (n, Some(n)))</c> assertions.
        /// </summary>
        private static void AssertSizeHint(int expected, int actual)
        {
            Assert.Equal(expected, actual);
        }

        // ------------------------------------------------------------------
        // iter_vertexes
        // ------------------------------------------------------------------

        [Fact]
        public void IterVertexes()
        {
            static void RunIterVertexesTests(bool isClosed)
            {
                var polyline = new Polyline<double>(0, isClosed);
                {
                    // empty
                    var iter = new DoubleEndedVertexIter(polyline.IterVertexes());
                    Assert.Equal(0, iter.SizeHint);
                    Assert.Null(iter.Next());
                }

                polyline.Add(1.0, 2.0, 0.3);

                {
                    // one vertex next
                    var iter = new DoubleEndedVertexIter(polyline.IterVertexes());
                    Assert.Equal(1, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(1.0, 2.0, 0.3), iter.Next());
                    Assert.Equal(0, iter.SizeHint);
                    Assert.Null(iter.Next());
                    Assert.Null(iter.NextBack());
                }

                {
                    // one vertex next_back
                    var iter = new DoubleEndedVertexIter(polyline.IterVertexes());
                    Assert.Equal(1, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(1.0, 2.0, 0.3), iter.NextBack());
                    Assert.Equal(0, iter.SizeHint);
                    Assert.Null(iter.NextBack());
                    Assert.Null(iter.Next());
                }

                polyline.Add(4.0, 5.0, 0.6);

                {
                    // two vertex next
                    var iter = new DoubleEndedVertexIter(polyline.IterVertexes());
                    Assert.Equal(2, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(1.0, 2.0, 0.3), iter.Next());
                    Assert.Equal(1, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(4.0, 5.0, 0.6), iter.Next());
                    Assert.Equal(0, iter.SizeHint);
                    Assert.Null(iter.NextBack());
                    Assert.Null(iter.Next());
                }

                {
                    // two vertex next_back
                    var iter = new DoubleEndedVertexIter(polyline.IterVertexes());
                    Assert.Equal(2, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(4.0, 5.0, 0.6), iter.NextBack());
                    Assert.Equal(1, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(1.0, 2.0, 0.3), iter.NextBack());
                    Assert.Equal(0, iter.SizeHint);
                    Assert.Null(iter.NextBack());
                    Assert.Null(iter.Next());
                }

                {
                    // two vertex next and next_back
                    var iter = new DoubleEndedVertexIter(polyline.IterVertexes());
                    Assert.Equal(2, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(1.0, 2.0, 0.3), iter.Next());
                    Assert.Equal(1, iter.SizeHint);
                    AssertSome(new PlineVertex<double>(4.0, 5.0, 0.6), iter.NextBack());
                    Assert.Equal(0, iter.SizeHint);
                    Assert.Null(iter.NextBack());
                    Assert.Null(iter.Next());
                }
            }

            // should have same results for both open and closed polyline
            RunIterVertexesTests(false);
            RunIterVertexesTests(true);
        }

        // ------------------------------------------------------------------
        // iter_segments
        // ------------------------------------------------------------------

        [Fact]
        public void IterSegments()
        {
            var polyline = new Polyline<double>();
            AssertSizeHint(0, polyline.IterSegments().Count());
            Assert.Empty(polyline.IterSegments().ToList());

            polyline.Add(1.0, 2.0, 0.3);
            AssertSizeHint(0, polyline.IterSegments().Count());
            Assert.Empty(polyline.IterSegments().ToList());

            polyline.Add(4.0, 5.0, 0.6);
            AssertSizeHint(1, polyline.IterSegments().Count());
            var oneSeg = polyline.IterSegments().ToList();
            Assert.Single(oneSeg);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), oneSeg[0].V1);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), oneSeg[0].V2);

            polyline.SetIsClosed(true);
            AssertSizeHint(2, polyline.IterSegments().Count());
            var twoSeg = polyline.IterSegments().ToList();
            Assert.Equal(2, twoSeg.Count);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), twoSeg[0].V1);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), twoSeg[0].V2);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), twoSeg[1].V1);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), twoSeg[1].V2);

            polyline.Add(0.5, 0.5, 0.5);
            AssertSizeHint(3, polyline.IterSegments().Count());
            var threeSeg = polyline.IterSegments().ToList();
            Assert.Equal(3, threeSeg.Count);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), threeSeg[0].V1);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), threeSeg[0].V2);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), threeSeg[1].V1);
            Assert.Equal(new PlineVertex<double>(0.5, 0.5, 0.5), threeSeg[1].V2);
            Assert.Equal(new PlineVertex<double>(0.5, 0.5, 0.5), threeSeg[2].V1);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), threeSeg[2].V2);

            polyline.SetIsClosed(false);
            AssertSizeHint(2, polyline.IterSegments().Count());
            var twoSegOpen = polyline.IterSegments().ToList();
            Assert.Equal(2, twoSegOpen.Count);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), twoSegOpen[0].V1);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), twoSegOpen[0].V2);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), twoSegOpen[1].V1);
            Assert.Equal(new PlineVertex<double>(0.5, 0.5, 0.5), twoSegOpen[1].V2);
        }

        // ------------------------------------------------------------------
        // iter_segment_indexes
        // ------------------------------------------------------------------

        [Fact]
        public void IterSegmentIndexes()
        {
            var polyline = new Polyline<double>();
            AssertSizeHint(0, polyline.IterSegmentIndexes().Count());
            Assert.Empty(polyline.IterSegmentIndexes().ToList());

            polyline.Add(1.0, 2.0, 0.3);
            AssertSizeHint(0, polyline.IterSegmentIndexes().Count());
            Assert.Empty(polyline.IterSegmentIndexes().ToList());

            polyline.Add(4.0, 5.0, 0.6);
            AssertSizeHint(1, polyline.IterSegmentIndexes().Count());
            var oneSeg = polyline.IterSegmentIndexes().ToList();
            Assert.Equal(new[] { (0, 1) }, oneSeg);

            polyline.SetIsClosed(true);
            AssertSizeHint(2, polyline.IterSegmentIndexes().Count());
            var twoSeg = polyline.IterSegmentIndexes().ToList();
            Assert.Equal(new[] { (0, 1), (1, 0) }, twoSeg);

            polyline.Add(0.5, 0.5, 0.5);
            AssertSizeHint(3, polyline.IterSegmentIndexes().Count());
            var threeSeg = polyline.IterSegmentIndexes().ToList();
            Assert.Equal(new[] { (0, 1), (1, 2), (2, 0) }, threeSeg);

            polyline.SetIsClosed(false);
            AssertSizeHint(2, polyline.IterSegmentIndexes().Count());
            var twoSegOpen = polyline.IterSegmentIndexes().ToList();
            Assert.Equal(new[] { (0, 1), (1, 2) }, twoSegOpen);
        }

        // ------------------------------------------------------------------
        // invert_direction_mut
        // ------------------------------------------------------------------

        [Fact]
        public void InvertDirectionMut()
        {
            var polyline = new Polyline<double>(true);
            polyline.Add(0.0, 0.0, 0.1);
            polyline.Add(2.0, 0.0, 0.2);
            polyline.Add(2.0, 2.0, 0.3);
            polyline.Add(0.0, 2.0, 0.4);

            polyline.InvertDirection();

            AssertFuzzyEq(new PlineVertex<double>(0.0, 2.0, -0.3), polyline.Get(0));
            AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, -0.2), polyline.Get(1));
            AssertFuzzyEq(new PlineVertex<double>(2.0, 0.0, -0.1), polyline.Get(2));
            AssertFuzzyEq(new PlineVertex<double>(0.0, 0.0, -0.4), polyline.Get(3));
        }

        // ------------------------------------------------------------------
        // remove_repeat
        // ------------------------------------------------------------------

        [Fact]
        public void RemoveRepeat()
        {
            {
                // empty polyline
                var polyline = new Polyline<double>(true);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.Null(result);
            }

            {
                // single vertex
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.Null(result);
            }

            {
                // two repeats, closed polyline
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(2.0, 2.0, 1.0);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(3.0, 3.0, 0.5);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, 3.0, 0.5), result[1]);
            }

            {
                // two repeats, open polyline
                var polyline = new Polyline<double>();
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(2.0, 2.0, 1.0);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(3.0, 3.0, 0.5);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.False(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, 3.0, 0.5), result[1]);
            }

            {
                // no repeats, closed polyline
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.Null(result);
            }

            {
                // no repeats, open polyline
                var polyline = new Polyline<double>();
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(4.0, 3.0, 1.0);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.Null(result);
            }

            {
                // last repeats position on first for closed polyline
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(2.0, 2.0, 1.0);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 0.5), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, 3.0, 1.0), result[1]);
            }

            {
                // last repeats position on first for open polyline
                var polyline = new Polyline<double>();
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(2.0, 2.0, 1.0);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.Null(result);
            }

            {
                // catches case where prev position is updated even when vertex is skipped causing
                // the end result to actually have a repeat position
                var polyline = BuildRepeatPosRegressionPline();

                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(7, result!.VertexCount);
                Assert.False(result.IsClosed);
                AssertRepeatPosRegressionResult(result);
            }
        }

        private static Polyline<double> BuildRepeatPosRegressionPline()
        {
            var polyline = new Polyline<double>();
            polyline.Add(149.75759744152376, 2753.341034622115, 0.0);
            polyline.Add(149.75761269666256, 2753.341034955893, -0.000000016806842584315973);
            polyline.Add(149.75760725254852, 2753.341034836777, -0.000000026349436410555433);
            polyline.Add(149.75759871737387, 2753.3410346500286, -0.0000000059965514775939255);
            polyline.Add(149.7576044186626, 2753.341034774772, -0.000000017257169693252198);
            polyline.Add(149.7576208261107, 2753.3410351337648, -0.00000001907759705765955);
            polyline.Add(149.75762700577317, 2753.3410352689743, -0.0024145466234173404);
            polyline.Add(176.35224446582103, 2753.7944419559553, -0.000000003667288472897212);
            polyline.Add(176.35224565393378, 2753.7944419704727, 0.0);
            polyline.Add(176.35227673059205, 2753.794442350188, 0.0);
            polyline.Add(176.35229710705553, 2753.794442599162, 0.0);
            return polyline;
        }

        private static void AssertRepeatPosRegressionResult(Polyline<double> result)
        {
            AssertFuzzyEq(new PlineVertex<double>(149.75759744152376, 2753.341034622115, 0.0), result[0]);
            AssertFuzzyEq(
                new PlineVertex<double>(149.75761269666256, 2753.341034955893, -0.000000026349436410555433),
                result[1]);
            AssertFuzzyEq(
                new PlineVertex<double>(149.75759871737387, 2753.3410346500286, -0.000000017257169693252198),
                result[2]);
            AssertFuzzyEq(
                new PlineVertex<double>(149.7576208261107, 2753.3410351337648, -0.0024145466234173404),
                result[3]);
            AssertFuzzyEq(new PlineVertex<double>(176.35224446582103, 2753.7944419559553, 0.0), result[4]);
            AssertFuzzyEq(new PlineVertex<double>(176.35227673059205, 2753.794442350188, 0.0), result[5]);
            AssertFuzzyEq(new PlineVertex<double>(176.35229710705553, 2753.794442599162, 0.0), result[6]);
        }

        // ------------------------------------------------------------------
        // remove_redundant_removes_repeat_pos
        //
        // NOTE: the Rust test mixes remove_redundant and remove_repeat_pos calls; the calls are
        // transcribed exactly as they appear in the Rust source.
        // ------------------------------------------------------------------

        [Fact]
        public void RemoveRedundantRemovesRepeatPos()
        {
            {
                // empty polyline
                var polyline = new Polyline<double>(true);
                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // single vertex
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // two repeats, closed polyline
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(2.0, 2.0, 1.0);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(3.0, 3.0, 0.5);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, 3.0, 0.5), result[1]);
            }

            {
                // two repeats, open polyline
                var polyline = new Polyline<double>();
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(2.0, 2.0, 1.0);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(3.0, 3.0, 0.5);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.False(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, 3.0, 0.5), result[1]);
            }

            {
                // no repeats, closed polyline
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // no repeats, open polyline
                var polyline = new Polyline<double>();
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(4.0, 3.0, 1.0);
                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // last repeats position on first for closed polyline
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(2.0, 2.0, 1.0);
                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 0.5), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, 3.0, 1.0), result[1]);
            }

            {
                // last repeats position on first for open polyline
                var polyline = new Polyline<double>();
                polyline.Add(2.0, 2.0, 0.5);
                polyline.Add(3.0, 3.0, 1.0);
                polyline.Add(2.0, 2.0, 1.0);
                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // catches case where prev position is updated even when vertex is skipped causing
                // the end result to actually have a repeat position
                var polyline = BuildRepeatPosRegressionPline();

                var result = polyline.RemoveRepeatPos(Eps);
                Assert.NotNull(result);
                Assert.Equal(7, result!.VertexCount);
                Assert.False(result.IsClosed);
                AssertRepeatPosRegressionResult(result);
            }
        }

        // ------------------------------------------------------------------
        // remove_redundant
        // ------------------------------------------------------------------

        [Fact]
        public void RemoveRedundant()
        {
            {
                // redundant point on line and repeat position
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.0);
                polyline.Add(3.0, 3.0, 0.0);
                polyline.Add(3.0, 3.0, 0.0);
                polyline.Add(4.0, 4.0, 0.0);
                polyline.Add(2.0, 4.0, 0.0);
                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(3, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 0.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(4.0, 4.0, 0.0), result[1]);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 4.0, 0.0), result[2]);
            }

            {
                // self intersecting points along line (collinear but opposing direction, points
                // should not be removed)
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.0);
                polyline.Add(3.0, 3.0, 0.0);
                polyline.Add(2.5, 2.5, 0.0);
                polyline.Add(4.0, 4.0, 0.0);
                polyline.Add(2.0, 4.0, 0.0);
                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // simple counter clockwise circle with extra vertex along one arc
                double bulge = Math.Tan((Math.PI / 2.0) / 4.0);
                var polyline = new Polyline<double>(true);
                polyline.Add(0.0, 0.0, -bulge);
                polyline.Add(1.0, 1.0, -bulge);
                polyline.Add(2.0, 0.0, -1.0);
                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(0.0, 0.0, -1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 0.0, -1.0), result[1]);
            }

            {
                // arcs along greater arc
                const double radius = 5.0;
                const double maxAngle = Math.PI / 2.0;
                const int count = 4;
                const double subAngle = (1.0 / count) * maxAngle;
                double bulge = BaseMath.BulgeFromAngle(subAngle);

                var polyline = new Polyline<double>();
                for (int i = 0; i <= count; i++)
                {
                    double angle = i * subAngle;
                    polyline.Add(radius * Math.Cos(angle), radius * Math.Sin(angle), bulge);
                }

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.False(result.IsClosed);
                AssertFuzzyEq(
                    new PlineVertex<double>(radius, 0.0, BaseMath.BulgeFromAngle(maxAngle)),
                    result[0]);
                AssertFuzzyEq(new PlineVertex<double>(0.0, radius, bulge), result[1]);
            }

            {
                // arcs along circle
                const double radius = 5.0;
                const double maxAngle = Math.Tau;
                const int count = 10;
                const double subAngle = (1.0 / count) * maxAngle;
                double bulge = BaseMath.BulgeFromAngle(subAngle);

                var polyline = new Polyline<double>(true);
                for (int i = 0; i < count; i++)
                {
                    double angle = i * subAngle;
                    polyline.Add(radius * Math.Cos(angle), radius * Math.Sin(angle), bulge);
                }

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(radius, 0.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(-radius, 0.0, 1.0), result[1]);
            }

            {
                // arcs along circle open polyline
                const double radius = 5.0;
                const double maxAngle = Math.Tau;
                const int count = 10;
                const double subAngle = (1.0 / count) * maxAngle;
                double bulge = BaseMath.BulgeFromAngle(subAngle);

                var polyline = new Polyline<double>();
                for (int i = 0; i <= count; i++)
                {
                    double angle = i * subAngle;
                    polyline.Add(radius * Math.Cos(angle), radius * Math.Sin(angle), bulge);
                }

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(3, result!.VertexCount);
                Assert.False(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(radius, 0.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(-radius, 0.0, 1.0), result[1]);
                AssertFuzzyEq(new PlineVertex<double>(radius, 0.0, bulge), result[2]);
            }

            {
                // already minimum circle
                const double radius = 5.0;

                var polyline = new Polyline<double>(true);
                polyline.Add(0.0, -radius, 1.0);
                polyline.Add(0.0, radius, 1.0);

                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // closed half circle with arc that causes first vertex to be redundant
                const double radius = 5.0;

                double bulge = BaseMath.BulgeFromAngle(-(Math.PI / 2.0));

                var polyline = new Polyline<double>(true);
                polyline.Add(0.0, radius, bulge);
                polyline.Add(radius, 0.0, 0.0);
                polyline.Add(-radius, 0.0, bulge);

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(-radius, 0.0, -1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(radius, 0.0, 0.0), result[1]);
            }

            {
                // open polyline with bulge values that would cause first vertex to be redundant if
                // polyline were closed
                const double radius = 5.0;

                double bulge = BaseMath.BulgeFromAngle(-(Math.PI / 2.0));

                var polyline = new Polyline<double>();
                polyline.Add(0.0, radius, bulge);
                polyline.Add(radius, 0.0, 0.0);
                polyline.Add(-radius, 0.0, bulge);

                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // closed path with redundant first vertex point along line
                var polyline = new Polyline<double>(true);
                polyline.Add(2.0, 2.0, 0.0);
                polyline.Add(3.0, 3.0, 0.0);
                polyline.Add(3.0, -2.0, 0.0);
                polyline.Add(-2.0, -2.0, 0.0);
                polyline.Add(-1.0, -1.0, 0.0);

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(3, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(-2.0, -2.0, 0.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, 3.0, 0.0), result[1]);
                AssertFuzzyEq(new PlineVertex<double>(3.0, -2.0, 0.0), result[2]);
            }

            {
                // open polyline with values that would cause first vertex to be redundant due to
                // being collinear if polyline were closed
                var polyline = new Polyline<double>();
                polyline.Add(2.0, 2.0, 0.0);
                polyline.Add(3.0, 3.0, 0.0);
                polyline.Add(3.0, -2.0, 0.0);
                polyline.Add(-2.0, -2.0, 0.0);
                polyline.Add(-1.0, -1.0, 0.0);

                var result = polyline.RemoveRedundant(Eps);
                Assert.Null(result);
            }

            {
                // circle defined by 4 vertexes
                double bulge = Math.Tan(Math.PI / 8.0);
                var polyline = new Polyline<double>(true);
                polyline.Add(-0.5, 0.0, bulge);
                polyline.Add(0.0, -0.5, bulge);
                polyline.Add(0.5, 0.0, bulge);
                polyline.Add(0.0, 0.5, bulge);

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(-0.5, 0.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(0.5, 0.0, 1.0), result[1]);
            }

            {
                // rounded rectangle collapsed into circle
                double bulge = Math.Tan(Math.PI / 8.0);
                var polyline = new Polyline<double>(true);
                polyline.Add(-0.5, 0.0, bulge);
                polyline.Add(0.0, -0.5, 0.0);
                polyline.Add(0.0, -0.5, bulge);
                polyline.Add(0.5, 0.0, 0.0);
                polyline.Add(0.5, 0.0, bulge);
                polyline.Add(0.0, 0.5, 0.0);
                polyline.Add(0.0, 0.5, bulge);
                polyline.Add(-0.5, 0.0, 0.0);

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(-0.5, 0.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(0.5, 0.0, 1.0), result[1]);
            }

            {
                // rounded rectangle collapsed into circle shifted vertex positions
                double bulge = Math.Tan(Math.PI / 8.0);
                var polyline = new Polyline<double>(true);
                polyline.Add(-0.5, 0.0, 0.0);
                polyline.Add(-0.5, 0.0, bulge);
                polyline.Add(0.0, -0.5, 0.0);
                polyline.Add(0.0, -0.5, bulge);
                polyline.Add(0.5, 0.0, 0.0);
                polyline.Add(0.5, 0.0, bulge);
                polyline.Add(0.0, 0.5, 0.0);
                polyline.Add(0.0, 0.5, bulge);

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(-0.5, 0.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(0.5, 0.0, 1.0), result[1]);
            }

            {
                // rounded rectangle collapsed into circle (but kept as open polyline)
                double bulge = Math.Tan(Math.PI / 8.0);
                var polyline = new Polyline<double>();
                polyline.Add(-0.5, 0.0, bulge);
                polyline.Add(0.0, -0.5, 0.0);
                polyline.Add(0.0, -0.5, bulge);
                polyline.Add(0.5, 0.0, 0.0);
                polyline.Add(0.5, 0.0, bulge);
                polyline.Add(0.0, 0.5, 0.0);
                polyline.Add(0.0, 0.5, bulge);
                polyline.Add(-0.5, 0.0, 0.0);

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(3, result!.VertexCount);
                Assert.False(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(-0.5, 0.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(0.5, 0.0, 1.0), result[1]);
                AssertFuzzyEq(new PlineVertex<double>(-0.5, 0.0, 0.0), result[2]);
            }

            {
                // rounded rectangle collapsed into circle with many repeat vertex positions
                double bulge = Math.Tan(Math.PI / 8.0);
                var polyline = new Polyline<double>(true);
                polyline.Add(-0.5, 0.0, 0.0);
                polyline.Add(-0.5, 0.0, 0.0);
                polyline.Add(-0.5, 0.0, 0.0);
                polyline.Add(-0.5, 0.0, bulge);
                polyline.Add(-0.5, 0.0, bulge);
                polyline.Add(-0.5, 0.0, bulge);
                polyline.Add(0.0, -0.5, 0.0);
                polyline.Add(0.0, -0.5, 0.0);
                polyline.Add(0.0, -0.5, 0.0);
                polyline.Add(0.0, -0.5, bulge);
                polyline.Add(0.5, 0.0, 0.0);
                polyline.Add(0.5, 0.0, bulge);
                polyline.Add(0.0, 0.5, 0.0);
                polyline.Add(0.0, 0.5, bulge);
                polyline.Add(0.0, 0.5, bulge);
                polyline.Add(0.0, 0.5, bulge);

                var result = polyline.RemoveRedundant(Eps);
                Assert.NotNull(result);
                Assert.Equal(2, result!.VertexCount);
                Assert.True(result.IsClosed);
                AssertFuzzyEq(new PlineVertex<double>(-0.5, 0.0, 1.0), result[0]);
                AssertFuzzyEq(new PlineVertex<double>(0.5, 0.0, 1.0), result[1]);
            }

            {
                // n equal points
                var polyline1 = new Polyline<double>(3, false);
                polyline1.Add(0.0, 0.0, 0.0);
                polyline1.Add(0.0, 0.0, 0.0);
                polyline1.Add(0.0, 0.0, 0.0);
                var polyline2 = new Polyline<double>(3, false);
                polyline2.Add(1.0, 1.0, 0.0);
                polyline2.Add(1.0, 1.0, 0.0);
                polyline2.Add(1.0, 1.0, 1.0);
                var polyline3 = new Polyline<double>(2, false);
                polyline3.Add(2.0, 2.0, 0.0);
                polyline3.Add(2.0, 2.0, 1.0);

                var r1 = polyline1.RemoveRedundant(Eps);
                var r2 = polyline2.RemoveRedundant(Eps);
                var r3 = polyline3.RemoveRedundant(Eps);
                Assert.NotNull(r1);
                Assert.NotNull(r2);
                Assert.NotNull(r3);
                Assert.Equal(1, r1!.VertexCount);
                Assert.Equal(1, r2!.VertexCount);
                Assert.Equal(1, r3!.VertexCount);
                AssertFuzzyEq(new PlineVertex<double>(0.0, 0.0, 0.0), r1[0]);
                AssertFuzzyEq(new PlineVertex<double>(1.0, 1.0, 1.0), r2[0]);
                AssertFuzzyEq(new PlineVertex<double>(2.0, 2.0, 1.0), r3[0]);
            }
        }

        // ------------------------------------------------------------------
        // rotate_start
        // ------------------------------------------------------------------

        [Fact]
        public void RotateStart()
        {
            {
                // empty polyline
                var polyline = new Polyline<double>(true);
                Assert.Null(polyline.RotateStart(0, new Vector2<double>(0.0, 0.0), Eps));
            }

            {
                // single vertex polyline
                var polyline = PlineBuilder.Closed((1.0, 0.0, 0.0));
                Assert.Null(polyline.RotateStart(0, new Vector2<double>(0.0, 0.0), Eps));
            }

            {
                // open polyline
                var polyline = PlineBuilder.Open(
                    (0.0, 0.0, 0.0),
                    (1.0, 0.0, 0.5),
                    (1.0, 1.0, 0.2),
                    (0.0, 1.0, -0.1));
                Assert.Null(polyline.RotateStart(0, new Vector2<double>(0.0, 0.0), Eps));
            }

            {
                // no change
                var polyline = PlineBuilder.Closed(
                    (0.0, 0.0, 0.0),
                    (1.0, 0.0, 0.5),
                    (1.0, 1.0, 0.2),
                    (0.0, 1.0, -0.1));

                var rotNoChange = polyline.RotateStart(0, new Vector2<double>(0.0, 0.0), Eps);
                Assert.NotNull(rotNoChange);
                Assert.True(rotNoChange!.FuzzyEq(polyline));
            }

            {
                // end becomes start
                var polyline = PlineBuilder.Closed(
                    (0.0, 0.0, 0.0),
                    (1.0, 0.0, 0.5),
                    (1.0, 1.0, 0.2),
                    (0.0, 1.0, -0.1));

                var rotEndIsStart = polyline.RotateStart(
                    polyline.VertexCount - 1,
                    new Vector2<double>(0.0, 1.0),
                    Eps);
                Assert.NotNull(rotEndIsStart);

                var expectedEndAsStart = PlineBuilder.Closed(
                    (0.0, 1.0, -0.1),
                    (0.0, 0.0, 0.0),
                    (1.0, 0.0, 0.5),
                    (1.0, 1.0, 0.2));

                Assert.True(rotEndIsStart!.FuzzyEq(expectedEndAsStart));
            }

            {
                // split in middle of line segment
                var polyline = PlineBuilder.Closed(
                    (0.0, 0.0, 0.0),
                    (1.0, 0.0, 0.0),
                    (1.0, 1.0, 0.0),
                    (0.0, 1.0, 0.0));

                var rot = polyline.RotateStart(0, new Vector2<double>(0.5, 0.0), Eps);
                Assert.NotNull(rot);
                var expectedRot = PlineBuilder.Closed(
                    (0.5, 0.0, 0.0),
                    (1.0, 0.0, 0.0),
                    (1.0, 1.0, 0.0),
                    (0.0, 1.0, 0.0),
                    (0.0, 0.0, 0.0));
                Assert.True(rot!.FuzzyEq(expectedRot));
            }

            {
                // split in middle of arc segment
                var polyline = PlineBuilder.Closed(
                    (0.0, 0.0, 0.0),
                    (1.0, 0.0, 1.0),
                    (1.0, 1.0, 0.0),
                    (0.0, 1.0, 0.0));

                var rot = polyline.RotateStart(1, new Vector2<double>(1.5, 0.5), Eps);
                Assert.NotNull(rot);

                var expectedRot = PlineBuilder.Closed(
                    (1.5, 0.5, BaseMath.BulgeFromAngle(Math.PI / 2.0)),
                    (1.0, 1.0, 0.0),
                    (0.0, 1.0, 0.0),
                    (0.0, 0.0, 0.0),
                    (1.0, 0.0, BaseMath.BulgeFromAngle(Math.PI / 2.0)));
                Assert.True(rot!.FuzzyEq(expectedRot));
            }
        }

        // ------------------------------------------------------------------
        // area
        // ------------------------------------------------------------------

        [Fact]
        public void Area()
        {
            {
                var circle = new Polyline<double>(true);
                circle.Add(0.0, 0.0, 1.0);
                circle.Add(2.0, 0.0, 1.0);
                Assert.True(circle.Area().FuzzyEq(Math.PI));
                circle.InvertDirection();
                Assert.True(circle.Area().FuzzyEq(-Math.PI));
            }

            {
                var halfCircle = new Polyline<double>(true);
                halfCircle.Add(0.0, 0.0, -1.0);
                halfCircle.Add(2.0, 0.0, 0.0);
                Assert.True(halfCircle.Area().FuzzyEq(-0.5 * Math.PI));
                halfCircle.InvertDirection();
                Assert.True(halfCircle.Area().FuzzyEq(0.5 * Math.PI));
            }

            {
                var rectangle = new Polyline<double>(true);
                rectangle.Add(0.0, 0.0, 0.0);
                rectangle.Add(3.0, 0.0, 0.0);
                rectangle.Add(3.0, 2.0, 0.0);
                rectangle.Add(0.0, 2.0, 0.0);
                Assert.True(rectangle.Area().FuzzyEq(6.0));
                rectangle.InvertDirection();
                Assert.True(rectangle.Area().FuzzyEq(-6.0));
            }

            {
                var openPolyline = new Polyline<double>();
                openPolyline.Add(0.0, 0.0, 0.0);
                openPolyline.Add(2.0, 0.0, 0.0);
                openPolyline.Add(2.0, 2.0, 0.0);
                openPolyline.Add(0.0, 2.0, 0.0);
                Assert.True(openPolyline.Area().FuzzyEq(0.0));
                openPolyline.InvertDirection();
                Assert.True(openPolyline.Area().FuzzyEq(0.0));
            }

            {
                var emptyOpenPolyline = new Polyline<double>();
                Assert.True(emptyOpenPolyline.Area().FuzzyEq(0.0));
            }

            {
                var emptyClosedPolyline = new Polyline<double>(true);
                Assert.True(emptyClosedPolyline.Area().FuzzyEq(0.0));
            }

            {
                var oneVertexOpenPolyline = new Polyline<double>();
                oneVertexOpenPolyline.Add(1.0, 1.0, 0.0);
                Assert.True(oneVertexOpenPolyline.Area().FuzzyEq(0.0));
            }

            {
                var oneVertexClosedPolyline = new Polyline<double>(true);
                oneVertexClosedPolyline.Add(1.0, 1.0, 0.0);
                Assert.True(oneVertexClosedPolyline.Area().FuzzyEq(0.0));
            }
        }

        // ------------------------------------------------------------------
        // path_length
        // ------------------------------------------------------------------

        [Fact]
        public void PathLength()
        {
            {
                var emptyOpenPolyline = new Polyline<double>();
                Assert.True(emptyOpenPolyline.PathLength().FuzzyEq(0.0));
            }

            {
                // NOTE: upstream creates an open polyline here despite the variable name
                var emptyClosedPolyline = new Polyline<double>();
                Assert.True(emptyClosedPolyline.PathLength().FuzzyEq(0.0));
            }

            {
                var oneVertexOpenPolyline = new Polyline<double>();
                oneVertexOpenPolyline.Add(1.0, 1.0, 0.0);
                Assert.True(oneVertexOpenPolyline.PathLength().FuzzyEq(0.0));
            }

            {
                var oneVertexClosedPolyline = new Polyline<double>(true);
                oneVertexClosedPolyline.Add(1.0, 1.0, 0.0);
                Assert.True(oneVertexClosedPolyline.PathLength().FuzzyEq(0.0));
            }

            {
                var circle = new Polyline<double>(true);
                circle.Add(0.0, 0.0, 1.0);
                circle.Add(2.0, 0.0, 1.0);
                Assert.True(circle.PathLength().FuzzyEq(Math.Tau));
                circle.InvertDirection();
                Assert.True(circle.PathLength().FuzzyEq(Math.Tau));
            }

            {
                var halfCircle = new Polyline<double>(true);
                halfCircle.Add(0.0, 0.0, -1.0);
                halfCircle.Add(2.0, 0.0, 0.0);
                Assert.True(halfCircle.PathLength().FuzzyEq(Math.PI + 2.0));
                halfCircle.InvertDirection();
                Assert.True(halfCircle.PathLength().FuzzyEq(Math.PI + 2.0));
            }

            {
                var rectangle = new Polyline<double>(true);
                rectangle.Add(0.0, 0.0, 0.0);
                rectangle.Add(3.0, 0.0, 0.0);
                rectangle.Add(3.0, 2.0, 0.0);
                rectangle.Add(0.0, 2.0, 0.0);
                Assert.True(rectangle.PathLength().FuzzyEq(10.0));
                rectangle.InvertDirection();
                Assert.True(rectangle.PathLength().FuzzyEq(10.0));
            }

            {
                var openPolyline = new Polyline<double>();
                openPolyline.Add(0.0, 0.0, 0.0);
                openPolyline.Add(3.0, 0.0, 0.0);
                openPolyline.Add(3.0, 2.0, 0.0);
                openPolyline.Add(0.0, 2.0, 0.0);
                Assert.True(openPolyline.PathLength().FuzzyEq(8.0));
                openPolyline.InvertDirection();
                Assert.True(openPolyline.PathLength().FuzzyEq(8.0));
            }
        }

        // ------------------------------------------------------------------
        // extents
        // ------------------------------------------------------------------

        [Fact]
        public void Extents()
        {
            {
                var emptyPline = new Polyline<double>();
                Assert.Null(emptyPline.Extents());
            }

            {
                var oneVertexPline = new Polyline<double>();
                oneVertexPline.Add(1.0, 1.0, 0.0);
                Assert.Null(oneVertexPline.Extents());
            }

            {
                // basic line
                var pline = PlineBuilder.Open((-2.0, -1.0, 0.0), (3.0, 4.0, 0.0));
                var extents = pline.Extents()!.Value;
                Assert.Equal(-2.0, extents.MinX);
                Assert.Equal(-1.0, extents.MinY);
                Assert.Equal(3.0, extents.MaxX);
                Assert.Equal(4.0, extents.MaxY);

                pline.SetIsClosed(true);
                extents = pline.Extents()!.Value;
                Assert.Equal(-2.0, extents.MinX);
                Assert.Equal(-1.0, extents.MinY);
                Assert.Equal(3.0, extents.MaxX);
                Assert.Equal(4.0, extents.MaxY);
            }

            {
                // axis aligned circle
                var pline = PlineBuilder.Closed((-1.0, 0.0, 1.0), (1.0, 0.0, 1.0));
                var extents = pline.Extents()!.Value;
                Assert.Equal(-1.0, extents.MinX);
                Assert.Equal(-1.0, extents.MinY);
                Assert.Equal(1.0, extents.MaxX);
                Assert.Equal(1.0, extents.MaxY);

                // half circle
                pline.SetIsClosed(false);
                extents = pline.Extents()!.Value;
                Assert.Equal(-1.0, extents.MinX);
                Assert.Equal(-1.0, extents.MinY);
                Assert.Equal(1.0, extents.MaxX);
                Assert.Equal(0.0, extents.MaxY);
            }

            {
                // axis aligned circle
                var pline = PlineBuilder.Closed((0.0, -1.0, 1.0), (0.0, 1.0, 1.0));
                var extents = pline.Extents()!.Value;
                Assert.Equal(-1.0, extents.MinX);
                Assert.Equal(-1.0, extents.MinY);
                Assert.Equal(1.0, extents.MaxX);
                Assert.Equal(1.0, extents.MaxY);

                // half circle
                pline.SetIsClosed(false);
                extents = pline.Extents()!.Value;
                Assert.Equal(0.0, extents.MinX);
                Assert.Equal(-1.0, extents.MinY);
                Assert.Equal(1.0, extents.MaxX);
                Assert.Equal(1.0, extents.MaxY);
            }

            {
                // handles repeat position vertexes
                var pline = PlineBuilder.Closed(
                    (-1.0, 0.0, 0.0),
                    (-1.0, 0.0, 1.0),
                    (-1.0, 0.0, 0.0),
                    (-1.0, 0.0, 1.0),
                    (1.0, 0.0, 1.0),
                    (1.0, 0.0, 1.0));
                var extents = pline.Extents()!.Value;
                Assert.Equal(-1.0, extents.MinX);
                Assert.Equal(-1.0, extents.MinY);
                Assert.Equal(1.0, extents.MaxX);
                Assert.Equal(1.0, extents.MaxY);
            }
        }

        // ------------------------------------------------------------------
        // find_point_at_path_length
        // ------------------------------------------------------------------

        /// <summary>
        /// Equivalent of the Rust <c>assert_path_length_result_eq!</c> macro for the Ok case.
        /// </summary>
        private static void AssertPathLengthOk(
            (bool Success, int SegIndex, Vector2<double> Point, double AccLength) actual,
            int expectedIndex,
            Vector2<double> expectedPoint)
        {
            Assert.True(actual.Success, "expected Ok result but got Err");
            Assert.Equal(expectedIndex, actual.SegIndex);
            Assert.True(
                actual.Point.FuzzyEqEps(expectedPoint, Eps),
                $"expected point {expectedPoint} but got {actual.Point}");
        }

        /// <summary>
        /// Equivalent of the Rust <c>assert_path_length_result_eq!</c> macro for the Err case.
        /// </summary>
        private static void AssertPathLengthErr(
            (bool Success, int SegIndex, Vector2<double> Point, double AccLength) actual,
            double expectedPathLength)
        {
            Assert.False(actual.Success, "expected Err result but got Ok");
            Assert.True(
                actual.AccLength.FuzzyEq(expectedPathLength, Eps),
                $"expected path length {expectedPathLength} but got {actual.AccLength}");
        }

        [Fact]
        public void FindPointAtPathLength()
        {
            var pline = PlineBuilder.Closed(
                (0.0, 0.0, 1.0),
                (1.0, 0.0, -1.0),
                (1.0, 1.0, 0.0),
                (1.0, 2.0, 0.0));
            double plinePathLength = pline.PathLength();

            // 0 path length (point at very start)
            {
                var r = pline.FindPointAtPathLength(0.0);
                AssertPathLengthOk(r, 0, new Vector2<double>(0.0, 0.0));
            }

            // total path length (point at very end)
            {
                var r = pline.FindPointAtPathLength(plinePathLength);
                AssertPathLengthOk(r, 3, new Vector2<double>(0.0, 0.0));
            }

            // negative path length
            {
                var r = pline.FindPointAtPathLength(-1.0);
                AssertPathLengthOk(r, 0, new Vector2<double>(0.0, 0.0));
            }

            // target path length greater than total
            {
                var r = pline.FindPointAtPathLength(plinePathLength + 1.0);
                AssertPathLengthErr(r, plinePathLength);
            }

            // half path length of first seg
            {
                double targetPathLength = PlineSeg.SegLength(pline[0], pline[1]) / 2.0;
                var r = pline.FindPointAtPathLength(targetPathLength);
                AssertPathLengthOk(r, 0, new Vector2<double>(0.5, -0.5));
            }

            // full path length of first seg
            {
                double targetPathLength = PlineSeg.SegLength(pline[0], pline[1]);
                var r = pline.FindPointAtPathLength(targetPathLength);
                AssertPathLengthOk(r, 0, new Vector2<double>(1.0, 0.0));
            }

            // half path length into second seg
            {
                double targetPathLength = PlineSeg.SegLength(pline[0], pline[1])
                    + PlineSeg.SegLength(pline[1], pline[2]) / 2.0;
                var r = pline.FindPointAtPathLength(targetPathLength);
                AssertPathLengthOk(r, 1, new Vector2<double>(0.5, 0.5));
            }

            // half path length into third seg
            {
                double targetPathLength = PlineSeg.SegLength(pline[0], pline[1])
                    + PlineSeg.SegLength(pline[1], pline[2])
                    + PlineSeg.SegLength(pline[2], pline[3]) / 2.0;
                var r = pline.FindPointAtPathLength(targetPathLength);
                AssertPathLengthOk(r, 2, new Vector2<double>(1.0, 1.5));
            }

            // sub slice tests (mostly to validate segment index offset)
            var subSlice = PlineViewData<double>.FromSlicePoints(
                pline,
                pline[2].Pos(),
                2,
                pline[3].Pos(),
                3,
                Eps);
            Assert.NotNull(subSlice);
            double subSliceLength = PlineSeg.SegLength(pline[2], pline[3]);
            var view = subSlice!.Value.View(pline);

            // 0 path length (point at very start)
            {
                var r = view.FindPointAtPathLength(0.0);
                AssertPathLengthOk(r, 0, new Vector2<double>(1.0, 1.0));
            }

            // total path length (point at very end)
            {
                var r = view.FindPointAtPathLength(subSliceLength);
                AssertPathLengthOk(r, 0, new Vector2<double>(1.0, 2.0));
            }
        }

        // ------------------------------------------------------------------
        // create_from_remove_repeat
        // ------------------------------------------------------------------

        [Fact]
        public void CreateFromRemoveRepeat()
        {
            var pline = PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (1.0, 0.0, 0.0),
                (1.0, 0.0, 0.0),
                (1.0, 1.0, 0.0),
                (0.0, 1.0, 0.0),
                (0.0, 1.0, 0.0),
                (0.0, 1.0, 0.0),
                (0.0, 0.0, 0.0));

            var result = PlineSourceExtensions.CreateFromRemoveRepeat<Polyline<double>, double>(pline, Eps);

            var expected = PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (1.0, 0.0, 0.0),
                (1.0, 1.0, 0.0),
                (0.0, 1.0, 0.0));

            Assert.True(result.FuzzyEq(expected));
        }

        // ------------------------------------------------------------------
        // Regression tests for the C# port (not present upstream).
        // ------------------------------------------------------------------

        [Fact]
        public void SetUserDataValuesFromOwnValuesKeepsData()
        {
            var pline = new Polyline<double>(true);
            pline.SetUserDataValues(new ulong[] { 1, 2, 3 });

            pline.SetUserDataValues(pline.UserDataValues);

            Assert.Equal(3, pline.UserDataCount);
            Assert.Equal(new ulong[] { 1, 2, 3 }, pline.UserDataValues);
        }

        [Fact]
        public void UserDataValuesIsNotCastableToMutableList()
        {
            var pline = new Polyline<double>(true);
            pline.SetUserDataValues(new ulong[] { 7 });

            Assert.IsNotType<System.Collections.Generic.List<ulong>>(pline.UserDataValues);
        }
    }
}
