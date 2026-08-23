using System;
using System.Collections.Generic;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream cavalier_contours 0.7.0
    /// <c>cavalier_contours/tests/test_pline_view.rs</c>.
    /// </summary>
    public class PlineViewTests
    {
        private const double PosEqEps = 1e-5;

        /// <summary>Epsilon used by the Rust <c>assert_fuzzy_eq!</c> macro for f64.</summary>
        private const double FuzzyEps = 1e-8;

        private const double FracPi2 = Math.PI / 2.0;
        private const double FracPi3 = Math.PI / 3.0;

        private static PlineViewData<double> Unwrap(PlineViewData<double>? data, string what)
        {
            Assert.True(data.HasValue, $"{what}: expected a view but got none");
            return data!.Value;
        }

        private static void AssertFuzzyEq(double expected, double actual, string what)
        {
            Assert.True(
                expected.FuzzyEq(actual, FuzzyEps),
                $"{what}: expected {expected:R} but got {actual:R}");
        }

        private static void AssertFuzzyEqVertex(PlineVertex<double> expected, PlineVertex<double> actual, string what)
        {
            Assert.True(
                expected.FuzzyEqEps(actual, FuzzyEps),
                $"{what}: expected vertex {expected} but got {actual}");
        }

        private static void AssertFuzzyEqPoint(Vector2<double> expected, Vector2<double> actual, string what)
        {
            Assert.True(
                expected.FuzzyEqEps(actual, FuzzyEps),
                $"{what}: expected point {expected} but got {actual}");
        }

        /// <summary>Equivalent of Rust <c>assert_fuzzy_eq!(&amp;actual_pline, &amp;expected_pline)</c>.</summary>
        private static void AssertFuzzyEqPline(IPlineSource<double> expected, IPlineSource<double> actual, string what)
        {
            Assert.True(
                expected.IsClosed == actual.IsClosed,
                $"{what}: expected IsClosed {expected.IsClosed} but got {actual.IsClosed}");
            Assert.Equal(expected.VertexCount, actual.VertexCount);
            for (int i = 0; i < expected.VertexCount; i++)
            {
                AssertFuzzyEqVertex(expected.Get(i), actual.Get(i), $"{what}: vertex {i}");
            }
        }

        private static void AssertFuzzyEqGeometry(IPlineSource<double> expected, IPlineSource<double> actual, string what)
        {
            AssertFuzzyEq(expected.PathLength(), actual.PathLength(), $"{what}: path length");

            AABB<double>? expectedExtents = expected.Extents();
            AABB<double>? actualExtents = actual.Extents();
            Assert.True(expectedExtents.HasValue, $"{what}: expected extents are missing");
            Assert.True(actualExtents.HasValue, $"{what}: actual extents are missing");
            AssertFuzzyEq(expectedExtents!.Value.MinX, actualExtents!.Value.MinX, $"{what}: extents MinX");
            AssertFuzzyEq(expectedExtents.Value.MinY, actualExtents.Value.MinY, $"{what}: extents MinY");
            AssertFuzzyEq(expectedExtents.Value.MaxX, actualExtents.Value.MaxX, $"{what}: extents MaxX");
            AssertFuzzyEq(expectedExtents.Value.MaxY, actualExtents.Value.MaxY, $"{what}: extents MaxY");
        }

        /// <summary>
        /// Equivalent of the Rust
        /// <c>let pline_from_slice = Polyline::create_from(&amp;slice.view(&amp;pline));
        /// assert_fuzzy_eq!(&amp;pline_from_slice, &amp;expected);</c> pattern, with additional
        /// <c>VertexCount</c> / <c>PathLength</c> / <c>Extents</c> checks against the view itself.
        /// </summary>
        private static void AssertViewMatches(
            PlineViewData<double>? sliceOpt,
            IPlineSource<double> source,
            IPlineSource<double> expected,
            string what)
        {
            PlineViewData<double> slice = Unwrap(sliceOpt, what);
            PlineView<double> view = slice.View(source);

            Assert.Equal(expected.VertexCount, view.VertexCount);

            var plineFromSlice = PlineSourceExtensions.CreateFrom<Polyline<double>, double>(view);
            AssertFuzzyEqPline(expected, plineFromSlice, what);
            AssertFuzzyEqGeometry(expected, view, what);
        }

        /// <summary>
        /// Equivalent of the Rust segment-by-segment comparison used by <c>from_new_start</c>:
        /// full vertex compare for <c>v1</c>, position-only compare for <c>v2</c>.
        /// </summary>
        private static void AssertViewSegmentsMatch(
            PlineViewData<double>? viewDataOpt,
            IPlineSource<double> source,
            IPlineSource<double> expected,
            string what)
        {
            PlineViewData<double> viewData = Unwrap(viewDataOpt, what);
            PlineView<double> view = viewData.View(source);

            Assert.Equal(expected.SegmentCount(), view.SegmentCount());
            // the view is always an open polyline, so it repeats the start position as its last vertex
            Assert.Equal(expected.SegmentCount() + 1, view.VertexCount);

            var expectedSegs = new List<(PlineVertex<double> V1, PlineVertex<double> V2)>(expected.IterSegments());
            var viewSegs = new List<(PlineVertex<double> V1, PlineVertex<double> V2)>(view.IterSegments());
            Assert.Equal(expectedSegs.Count, viewSegs.Count);

            for (int i = 0; i < expectedSegs.Count; i++)
            {
                AssertFuzzyEqVertex(expectedSegs[i].V1, viewSegs[i].V1, $"{what}: segment {i} v1");
                AssertFuzzyEqPoint(expectedSegs[i].V2.Pos(), viewSegs[i].V2.Pos(), $"{what}: segment {i} v2 position");
            }

            AssertFuzzyEqGeometry(expected, view, what);
        }

        [Fact]
        public void FromSlicePointsSingleSeg()
        {
            var pline = PlineBuilder.Open((0.0, 0.0, 1.0), (1.0, 0.0, 0.0));

            // complete polyline
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 0.0),
                    0,
                    PosEqEps);

                AssertViewMatches(slice, pline, pline, "complete polyline");
            }

            // complete polyline (end segment index on top of final vertex)
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 0.0),
                    1,
                    PosEqEps);

                AssertViewMatches(slice, pline, pline, "complete polyline (end index on final vertex)");
            }

            // slice from start to middle
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open((0.0, 0.0, bulge), (0.5, -0.5, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from start to middle");
            }

            // slice from middle to end
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    new Vector2<double>(1.0, 0.0),
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open((0.5, -0.5, bulge), (1.0, 0.0, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from middle to end");
            }

            // slice from middle to end (end segment index on top of final vertex)
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    new Vector2<double>(1.0, 0.0),
                    1,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open((0.5, -0.5, bulge), (1.0, 0.0, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from middle to end (end index on final vertex)");
            }

            // slice from first third to second third of the segment
            {
                Vector2<double> startPoint = BaseMath.PointOnCircle(0.5, new Vector2<double>(0.5, 0.0), Math.PI + FracPi3);
                Vector2<double> endPoint = BaseMath.PointOnCircle(0.5, new Vector2<double>(0.5, 0.0), Math.PI + 2.0 * FracPi3);
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    startPoint,
                    0,
                    endPoint,
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi3);
                var expected = new Polyline<double>();
                expected.AddVertex(PlineVertex<double>.FromVector2(startPoint, bulge));
                expected.AddVertex(PlineVertex<double>.FromVector2(endPoint, 0.0));

                AssertViewMatches(slice, pline, expected, "slice from first third to second third");
            }

            // collapsed slice at start
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    PosEqEps);

                Assert.False(slice.HasValue, "collapsed slice at start should produce no view");
            }

            // collapsed slice at end
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(1.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 0.0),
                    0,
                    PosEqEps);

                Assert.False(slice.HasValue, "collapsed slice at end should produce no view");
            }

            var closedPline = PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (5.0, 0.0, 0.0),
                (5.0, 5.0, 0.0),
                (0.0, 5.0, 0.0));

            // collapsed closed polyline (by having start and end point same with same segment index)
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    closedPline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    PosEqEps);

                Assert.False(slice.HasValue, "collapsed closed polyline should produce no view");
            }

            // complete closed polyline (by having end point be at end of last segment)
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    closedPline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(0.0, 0.0),
                    3,
                    PosEqEps);

                PlineViewData<double> sliceData = Unwrap(slice, "complete closed polyline");
                PlineView<double> view = sliceData.View(closedPline);

                var expectedSegs = new List<(PlineVertex<double> V1, PlineVertex<double> V2)>(closedPline.IterSegments());
                var viewSegs = new List<(PlineVertex<double> V1, PlineVertex<double> V2)>(view.IterSegments());
                Assert.Equal(expectedSegs.Count, viewSegs.Count);
                Assert.Equal(closedPline.VertexCount + 1, view.VertexCount);

                for (int i = 0; i < expectedSegs.Count; i++)
                {
                    AssertFuzzyEqVertex(expectedSegs[i].V1, viewSegs[i].V1, $"complete closed polyline: segment {i} v1");
                    AssertFuzzyEqVertex(expectedSegs[i].V2, viewSegs[i].V2, $"complete closed polyline: segment {i} v2");
                }

                AssertFuzzyEqGeometry(closedPline, view, "complete closed polyline");
            }
        }

        [Fact]
        public void FromSlicePointsMultiSeg()
        {
            var pline = PlineBuilder.Closed((0.0, 0.0, 1.0), (1.0, 0.0, 0.0), (1.0, 1.0, 0.0));

            // complete polyline
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 1.0),
                    1,
                    PosEqEps);

                var expected = PlineBuilder.Open((0.0, 0.0, 1.0), (1.0, 0.0, 0.0), (1.0, 1.0, 0.0));
                AssertViewMatches(slice, pline, expected, "complete polyline");
            }

            // complete polyline (end segment index on top of last vertex)
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 1.0),
                    2,
                    PosEqEps);

                var expected = PlineBuilder.Open((0.0, 0.0, 1.0), (1.0, 0.0, 0.0), (1.0, 1.0, 0.0));
                AssertViewMatches(slice, pline, expected, "complete polyline (end index on last vertex)");
            }

            // slice from start to middle of first segment
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open((0.0, 0.0, bulge), (0.5, -0.5, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from start to middle of first segment");
            }

            // slice from middle to end of first segment
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    new Vector2<double>(1.0, 0.0),
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open((0.5, -0.5, bulge), (1.0, 0.0, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from middle to end of first segment");
            }

            // slice from start to second vertex
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 0.0),
                    0,
                    PosEqEps);

                var expected = PlineBuilder.Open((0.0, 0.0, 1.0), (1.0, 0.0, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from start to second vertex");
            }

            // slice from start to middle of second segment
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 0.5),
                    1,
                    PosEqEps);

                var expected = PlineBuilder.Open((0.0, 0.0, 1.0), (1.0, 0.0, 0.0), (1.0, 0.5, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from start to middle of second segment");
            }

            // slice from second vertex to middle of second segment
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(1.0, 0.0),
                    1,
                    new Vector2<double>(1.0, 0.5),
                    1,
                    PosEqEps);

                var expected = PlineBuilder.Open((1.0, 0.0, 0.0), (1.0, 0.5, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from second vertex to middle of second segment");
            }

            // slice from second vertex to middle of second segment (using previous index for start)
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(1.0, 0.0),
                    0,
                    new Vector2<double>(1.0, 0.5),
                    1,
                    PosEqEps);

                var expected = PlineBuilder.Open((1.0, 0.0, 0.0), (1.0, 0.5, 0.0));
                AssertViewMatches(
                    slice,
                    pline,
                    expected,
                    "slice from second vertex to middle of second segment (previous index for start)");
            }

            // slice from middle of first segment to last vertex position
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    new Vector2<double>(1.0, 1.0),
                    1,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open((0.5, -0.5, bulge), (1.0, 0.0, 0.0), (1.0, 1.0, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from middle of first segment to last vertex position");
            }

            // slice from middle of end segment to middle of first segment (wrapping)
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(1.0, 0.5),
                    1,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open(
                    (1.0, 0.5, 0.0),
                    (1.0, 1.0, 0.0),
                    (0.0, 0.0, bulge),
                    (0.5, -0.5, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from middle of end segment to middle of first segment");
            }

            // collapsed slice at start
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    PosEqEps);

                Assert.False(slice.HasValue, "collapsed slice at start should produce no view");
            }

            // collapsed slice at midpoint of second segment
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(1.0, 0.5),
                    1,
                    new Vector2<double>(1.0, 0.5),
                    1,
                    PosEqEps);

                Assert.False(slice.HasValue, "collapsed slice at midpoint of second segment should produce no view");
            }

            // slice from middle of first segment wrapping back to start of first segment
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open(
                    (0.5, -0.5, bulge),
                    (1.0, 0.0, 0.0),
                    (1.0, 1.0, 0.0),
                    (0.0, 0.0, 0.0));
                AssertViewMatches(slice, pline, expected, "slice from middle of first segment wrapping back to start");
            }

            // slice from middle of second segment wrapping back to middle of first segment
            {
                var slice = PlineViewData<double>.FromSlicePoints(
                    pline,
                    new Vector2<double>(1.0, 0.5),
                    1,
                    new Vector2<double>(0.5, -0.5),
                    0,
                    PosEqEps);

                double bulge = BaseMath.BulgeFromAngle(FracPi2);
                var expected = PlineBuilder.Open(
                    (1.0, 0.5, 0.0),
                    (1.0, 1.0, 0.0),
                    (0.0, 0.0, bulge),
                    (0.5, -0.5, 0.0));
                AssertViewMatches(
                    slice,
                    pline,
                    expected,
                    "slice from middle of second segment wrapping back to middle of first segment");
            }
        }

#if DEBUG
        [Fact]
#else
        [Fact(Skip = "deviation under -c Release: Rust pline_view.rs:507 uses debug_assert! with the "
            + "message \"start index should be less than or equal to end index if polyline is open\" and "
            + "the upstream test is #[should_panic]. The C# port mirrors this with Debug.Assert in "
            + "PlineViewData<T>.FromSlicePoints, which the compiler strips in Release. Expected: an "
            + "exception is thrown. Actual (-c Release): no exception, a PlineViewData is returned. "
            + "The test passes under `dotnet test -c Debug`; upstream `cargo test --release` would not "
            + "panic either, so this is a build-configuration difference, not a logic deviation.")]
#endif
        public void AttemptingToWrapSliceOnOpenPline()
        {
            var pline = PlineBuilder.Open((0.0, 0.0, 1.0), (1.0, 0.0, 0.0), (1.0, 1.0, 0.0));

            // Rust: #[should_panic(expected = "start index should be less than or equal to end index
            // if polyline is open")]
            Assert.ThrowsAny<Exception>(() => PlineViewData<double>.FromSlicePoints(
                pline,
                new Vector2<double>(1.0, 0.5),
                1,
                new Vector2<double>(0.5, -0.5),
                0,
                PosEqEps));
        }

        [Fact]
        public void FromNewStart()
        {
            var closedPline = PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (5.0, 0.0, 0.0),
                (5.0, 5.0, 0.0),
                (0.0, 5.0, 0.0));

            var closedPlineWithBulges = PlineBuilder.Closed(
                (0.0, 0.0, 0.1),
                (5.0, 0.0, 0.2),
                (5.0, 5.0, 0.3),
                (0.0, 5.0, 0.4));

            // change start on first segment of closed polyline
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPline,
                    new Vector2<double>(1.5, 0.0),
                    0,
                    PosEqEps);

                var expected = PlineBuilder.Closed(
                    (1.5, 0.0, 0.0),
                    (5.0, 0.0, 0.0),
                    (5.0, 5.0, 0.0),
                    (0.0, 5.0, 0.0),
                    (0.0, 0.0, 0.0));

                AssertViewSegmentsMatch(viewData, closedPline, expected, "change start on first segment");
            }

            // change start on top of first vertex of closed polyline (no change)
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPline,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    PosEqEps);

                AssertViewSegmentsMatch(viewData, closedPline, closedPline, "change start on top of first vertex");
            }

            // change start on top of first vertex of closed polyline with bulge values (no change)
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPlineWithBulges,
                    new Vector2<double>(0.0, 0.0),
                    0,
                    PosEqEps);

                AssertViewSegmentsMatch(
                    viewData,
                    closedPlineWithBulges,
                    closedPlineWithBulges,
                    "change start on top of first vertex (with bulges)");
            }

            // change start on top of first vertex of closed polyline (no change) (using last segment
            // index that puts point on end of segment)
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPline,
                    new Vector2<double>(0.0, 0.0),
                    3,
                    PosEqEps);

                AssertViewSegmentsMatch(
                    viewData,
                    closedPline,
                    closedPline,
                    "change start on top of first vertex (last segment index)");
            }

            // change start on top of second vertex of closed polyline
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPline,
                    new Vector2<double>(5.0, 0.0),
                    1,
                    PosEqEps);

                var expected = PlineBuilder.Closed(
                    (5.0, 0.0, 0.0),
                    (5.0, 5.0, 0.0),
                    (0.0, 5.0, 0.0),
                    (0.0, 0.0, 0.0));

                AssertViewSegmentsMatch(viewData, closedPline, expected, "change start on top of second vertex");
            }

            // change start on top of second vertex of closed polyline with bulge values
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPlineWithBulges,
                    new Vector2<double>(5.0, 0.0),
                    1,
                    PosEqEps);

                var expected = PlineBuilder.Closed(
                    (5.0, 0.0, 0.2),
                    (5.0, 5.0, 0.3),
                    (0.0, 5.0, 0.4),
                    (0.0, 0.0, 0.1));

                AssertViewSegmentsMatch(
                    viewData,
                    closedPlineWithBulges,
                    expected,
                    "change start on top of second vertex (with bulges)");
            }

            // change start on top of second vertex of closed polyline (using last segment index
            // that puts point on end of segment)
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPline,
                    new Vector2<double>(5.0, 0.0),
                    0,
                    PosEqEps);

                var expected = PlineBuilder.Closed(
                    (5.0, 0.0, 0.0),
                    (5.0, 5.0, 0.0),
                    (0.0, 5.0, 0.0),
                    (0.0, 0.0, 0.0));

                AssertViewSegmentsMatch(
                    viewData,
                    closedPline,
                    expected,
                    "change start on top of second vertex (previous segment index)");
            }

            // change start on second segment of closed polyline
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPline,
                    new Vector2<double>(5.0, 2.22),
                    1,
                    PosEqEps);

                var expected = PlineBuilder.Closed(
                    (5.0, 2.22, 0.0),
                    (5.0, 5.0, 0.0),
                    (0.0, 5.0, 0.0),
                    (0.0, 0.0, 0.0),
                    (5.0, 0.0, 0.0));

                AssertViewSegmentsMatch(viewData, closedPline, expected, "change start on second segment");
            }

            // change start on last segment of closed polyline
            {
                var viewData = PlineViewData<double>.FromNewStart(
                    closedPline,
                    new Vector2<double>(0.0, 2.22),
                    3,
                    PosEqEps);

                var expected = PlineBuilder.Closed(
                    (0.0, 2.22, 0.0),
                    (0.0, 0.0, 0.0),
                    (5.0, 0.0, 0.0),
                    (5.0, 5.0, 0.0),
                    (0.0, 5.0, 0.0));

                AssertViewSegmentsMatch(viewData, closedPline, expected, "change start on last segment");
            }
        }
        /// <summary>
        /// Added in upstream 0.8.0 together with the collapsed-slice branch in from_slice_points.
        /// Ported verbatim from tests/test_pline_view.rs at tag 0.8.0.
        /// </summary>
        [Fact]
        public void FromSlicePointsCollapsedAcrossNearVertex()
        {
            const double posEqEps = 1e-5;
            var pline = PlineBuilder.Open(
                (0.0, 0.0, 0.0),
                (0.0, posEqEps * 1.1, 0.0),
                (0.0, -1.0, 0.0));

            var slice = PlineViewData<double>.FromSlicePoints(
                pline,
                new Vector2<double>(0.0, 0.0),
                0,
                new Vector2<double>(0.0, posEqEps * 0.55),
                1,
                posEqEps);

            Assert.Null(slice);
        }

    }
}
