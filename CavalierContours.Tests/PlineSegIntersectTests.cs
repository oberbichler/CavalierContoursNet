using System;
using Xunit;
using CavalierContours.Core;
using CavalierContours.Polyline;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of the Rust integration test suite
    /// <c>cavalier_contours/tests/test_pline_seg_intersect.rs</c> (upstream tag 0.7.0).
    /// One <see cref="FactAttribute"/> per Rust <c>#[test]</c>, Rust name in PascalCase.
    /// </summary>
    public class PlineSegIntersectTests
    {
        private const double Eps = 1e-5;

        private static PlineVertex<double> V(double x, double y, double bulge) => new(x, y, bulge);

        // ------------------------------------------------------------------
        // Reproduction of the Rust `assert_case_eq!` macro.
        //
        // The macro only accepts a match when BOTH the enum variant matches AND
        // every contained point is `fuzzy_eq`. Any other combination panics.
        // Point ORDER is therefore part of the asserted contract.
        // ------------------------------------------------------------------

        private static void AssertPointEq(Vector2<double> expected, Vector2<double> actual)
        {
            Assert.Equal(expected.X, actual.X, 10);
            Assert.Equal(expected.Y, actual.Y, 10);
        }

        private static void AssertNoIntersect(PlineSegIntr<double> actual)
        {
            Assert.Equal(PlineSegIntrKind.NoIntersect, actual.Kind);
        }

        private static void AssertTangentIntersect(Vector2<double> point, PlineSegIntr<double> actual)
        {
            Assert.Equal(PlineSegIntrKind.TangentIntersect, actual.Kind);
            AssertPointEq(point, actual.Point1);
        }

        private static void AssertOneIntersect(Vector2<double> point, PlineSegIntr<double> actual)
        {
            Assert.Equal(PlineSegIntrKind.OneIntersect, actual.Kind);
            AssertPointEq(point, actual.Point1);
        }

        private static void AssertTwoIntersects(Vector2<double> point1, Vector2<double> point2, PlineSegIntr<double> actual)
        {
            Assert.Equal(PlineSegIntrKind.TwoIntersects, actual.Kind);
            AssertPointEq(point1, actual.Point1);
            AssertPointEq(point2, actual.Point2);
        }

        private static void AssertOverlappingLines(Vector2<double> point1, Vector2<double> point2, PlineSegIntr<double> actual)
        {
            Assert.Equal(PlineSegIntrKind.OverlappingLines, actual.Kind);
            AssertPointEq(point1, actual.Point1);
            AssertPointEq(point2, actual.Point2);
        }

        private static void AssertOverlappingArcs(Vector2<double> point1, Vector2<double> point2, PlineSegIntr<double> actual)
        {
            Assert.Equal(PlineSegIntrKind.OverlappingArcs, actual.Kind);
            AssertPointEq(point1, actual.Point1);
            AssertPointEq(point2, actual.Point2);
        }

        // ------------------------------------------------------------------
        // Tests
        // ------------------------------------------------------------------

        [Fact]
        public void ArcLineNoIntersect()
        {
            var v1 = V(0.0, 0.0, 1.0);
            var v2 = V(2.0, 0.0, 0.0);
            var u1 = V(0.0, 1.0, 0.0);
            var u2 = V(2.0, 3.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertNoIntersect(result);
        }

        [Fact]
        public void LineArcNoIntersect()
        {
            var v1 = V(0.0, 1.0, 0.0);
            var v2 = V(2.0, 3.0, 0.0);
            var u1 = V(0.0, 0.0, 1.0);
            var u2 = V(2.0, 0.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertNoIntersect(result);
        }

        [Fact]
        public void OverlappingLines()
        {
            var v1 = V(3.0, 3.0, 0.0);
            var v2 = V(1.0, 1.0, 0.0);
            var u1 = V(1.0, 1.0, 0.0);
            var u2 = V(2.0, 2.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingLines(
                new Vector2<double>(1.0, 1.0),
                new Vector2<double>(2.0, 2.0),
                result);
        }

        [Fact]
        public void OverlappingLinesReverseDir()
        {
            var v1 = V(1.0, 1.0, 0.0);
            var v2 = V(3.0, 3.0, 0.0);
            var u1 = V(2.0, 2.0, 0.0);
            var u2 = V(1.0, 1.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingLines(
                new Vector2<double>(2.0, 2.0),
                new Vector2<double>(1.0, 1.0),
                result);
        }

        [Fact]
        public void OverlappingSameArcs()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 3.0, 0.0);
            var u1 = V(1.0, 1.0, 1.0);
            var u2 = V(3.0, 3.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(1.0, 1.0),
                new Vector2<double>(3.0, 3.0),
                result);
        }

        [Fact]
        public void OverlappingSameArcsReverseDir()
        {
            var v1 = V(3.0, 3.0, -1.0);
            var v2 = V(1.0, 1.0, 0.0);
            var u1 = V(1.0, 1.0, 1.0);
            var u2 = V(3.0, 3.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(1.0, 1.0),
                new Vector2<double>(3.0, 3.0),
                result);
        }

        [Fact]
        public void ArcArcEndPointsTouch()
        {
            var v1 = V(3.0, 3.0, 1.0);
            var v2 = V(1.0, 1.0, 0.0);
            var u1 = V(1.0, 1.0, 1.0);
            var u2 = V(3.0, 3.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertTwoIntersects(
                new Vector2<double>(1.0, 1.0),
                new Vector2<double>(3.0, 3.0),
                result);
        }

        [Fact]
        public void ArcArcEndPointsTouchReverseDir()
        {
            var v1 = V(1.0, 1.0, -1.0);
            var v2 = V(3.0, 3.0, 0.0);
            var u1 = V(1.0, 1.0, 1.0);
            var u2 = V(3.0, 3.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertTwoIntersects(
                new Vector2<double>(1.0, 1.0),
                new Vector2<double>(3.0, 3.0),
                result);

            // reverse parameter order should yield the same result
            result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertTwoIntersects(
                new Vector2<double>(1.0, 1.0),
                new Vector2<double>(3.0, 3.0),
                result);

            // changing direction of arc2 should yield the same result BUT point1/point2 ordered
            // according to second segment direction
            var u1b = V(3.0, 3.0, -1.0);
            var u2b = V(1.0, 1.0, 0.0);
            result = PlineSegIntersection.Intersect(v1, v2, u1b, u2b, Eps);
            AssertTwoIntersects(
                new Vector2<double>(3.0, 3.0),
                new Vector2<double>(1.0, 1.0),
                result);
        }

        [Fact]
        public void Arc2WithinArc1Overlapping()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            double bulge = BaseMath.BulgeFromAngle(Math.PI / 2.0);
            var u1 = V(2.0, 0.0, bulge);
            var u2 = V(3.0, 1.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(2.0, 0.0),
                new Vector2<double>(3.0, 1.0),
                result);
        }

        [Fact]
        public void Arc1WithinArc2Overlapping()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            double bulge = BaseMath.BulgeFromAngle(Math.PI / 2.0);
            var u1 = V(2.0, 0.0, bulge);
            var u2 = V(3.0, 1.0, 0.0);
            var result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(2.0, 0.0),
                new Vector2<double>(3.0, 1.0),
                result);
        }

        [Fact]
        public void Arc2WithinArc1OverlappingReverseDir()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            double bulge = BaseMath.BulgeFromAngle(Math.PI / 2.0);
            var u1 = V(3.0, 1.0, -bulge);
            var u2 = V(2.0, 0.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(3.0, 1.0),
                new Vector2<double>(2.0, 0.0),
                result);
        }

        [Fact]
        public void Arc1WithinArc2OverlappingReverseDir()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            double bulge = BaseMath.BulgeFromAngle(Math.PI / 2.0);
            var u1 = V(3.0, 1.0, -bulge);
            var u2 = V(2.0, 0.0, 0.0);
            var result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(2.0, 0.0),
                new Vector2<double>(3.0, 1.0),
                result);
        }

        [Fact]
        public void ArcArcPartialOverlap()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            var u1 = V(2.0, 0.0, 1.0);
            var u2 = V(2.0, 2.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(2.0, 0.0),
                new Vector2<double>(3.0, 1.0),
                result);
        }

        [Fact]
        public void ArcArcPartialOverlapFlipped()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            var u1 = V(2.0, 0.0, 1.0);
            var u2 = V(2.0, 2.0, 0.0);
            var result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(2.0, 0.0),
                new Vector2<double>(3.0, 1.0),
                result);
        }

        [Fact]
        public void ArcArcPartialOverlapArc2ReverseDir()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            var u1 = V(2.0, 2.0, -1.0);
            var u2 = V(2.0, 0.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(3.0, 1.0),
                new Vector2<double>(2.0, 0.0),
                result);
        }

        [Fact]
        public void ArcArcPartialOverlapArc2ReverseDirFlipped()
        {
            var v1 = V(1.0, 1.0, 1.0);
            var v2 = V(3.0, 1.0, 0.0);

            var u1 = V(2.0, 2.0, -1.0);
            var u2 = V(2.0, 0.0, 0.0);
            var result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(2.0, 0.0),
                new Vector2<double>(3.0, 1.0),
                result);
        }

        [Fact]
        public void ArcArcPartialOverlapArc1ReverseDir()
        {
            var v1 = V(3.0, 1.0, -1.0);
            var v2 = V(1.0, 1.0, 0.0);

            var u1 = V(2.0, 0.0, 1.0);
            var u2 = V(2.0, 2.0, 0.0);
            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(2.0, 0.0),
                new Vector2<double>(3.0, 1.0),
                result);
        }

        [Fact]
        public void ArcArcPartialOverlapArc1ReverseDirFlipped()
        {
            var v1 = V(3.0, 1.0, -1.0);
            var v2 = V(1.0, 1.0, 0.0);

            var u1 = V(2.0, 0.0, 1.0);
            var u2 = V(2.0, 2.0, 0.0);
            var result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertOverlappingArcs(
                new Vector2<double>(3.0, 1.0),
                new Vector2<double>(2.0, 0.0),
                result);
        }

        [Fact]
        public void ArcArcOppositeDirectionTouchAtEndsBug()
        {
            // This test case reproduces the bug where arcs have the same radius and center but
            // opposite directions and only touch at the end points.
            // The bug was that when same_direction_arcs = false, the code would return u1.pos()
            // as the intersection point, but after direction adjustment, u1.pos() is actually
            // the END of arc2, not the start. The actual intersection should be at u2.pos().
            //
            // Original issue that found it:
            // https://github.com/jbuckmccready/cavalier_contours/issues/42

            // Arc1
            var v1 = V(-189.0, -196.91384910249, 0.553407781718062);
            var v2 = V(-170.999999999999, -225.631646989572, -0.553407781718061);

            // Arc2
            var u1 = V(-153.0, -196.91384910249, -0.553407781718061);
            var u2 = V(-171.0, -225.631646989571, -0.553407781718061);

            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);

            // The arcs should intersect at u2.pos() (where arc1 and arc2 ends),
            // NOT at u1.pos() (which is ~34 units away from the actual intersection)
            AssertOneIntersect(
                new Vector2<double>(-171.0, -225.631646989571), // u2.pos()
                result);

            // reverse parameter order should yield the same result
            result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertOneIntersect(
                new Vector2<double>(-171.0, -225.631646989571), // u2.pos()
                result);

            // changing direction of arc2 should yield the same result
            var u1b = V(-171.0, -225.631646989571, 0.553407781718062);
            var u2b = V(-153.0, -196.91384910249, -0.553407781718061);
            result = PlineSegIntersection.Intersect(v1, v2, u1b, u2b, Eps);
            AssertOneIntersect(
                new Vector2<double>(-171.0, -225.631646989571),
                result);
        }

        // ------------------------------------------------------------------
        // NOT part of test_pline_seg_intersect.rs.
        //
        // The 0.7.0 Rust integration test file never produces the
        // TangentIntersect variant. This test closes that coverage gap. The
        // expected values are NOT hand-derived: they were obtained by executing
        // pline_seg_intr from the unmodified cavalier_contours 0.7.0 crate,
        // which printed for all three cases below:
        //     TangentIntersect { point: Vector2 { x: 1.0, y: -1.0 } }
        // ------------------------------------------------------------------
        [Fact]
        public void TangentIntersectCases()
        {
            // arc: (0,0) bulge 1 -> (2,0) is the lower semicircle, center (1,0), radius 1.
            var v1 = V(0.0, 0.0, 1.0);
            var v2 = V(2.0, 0.0, 0.0);

            // line: (0,-1) -> (2,-1) touches that circle exactly at (1,-1).
            var u1 = V(0.0, -1.0, 0.0);
            var u2 = V(2.0, -1.0, 0.0);

            var result = PlineSegIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertTangentIntersect(new Vector2<double>(1.0, -1.0), result);

            // reverse parameter order (line first) yields the same result
            result = PlineSegIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertTangentIntersect(new Vector2<double>(1.0, -1.0), result);

            // arc-arc tangent: (0,-2) bulge -1 -> (2,-2) is the upper semicircle of the
            // circle with center (1,-2) and radius 1, touching arc1 at (1,-1).
            var w1 = V(0.0, -2.0, -1.0);
            var w2 = V(2.0, -2.0, 0.0);
            result = PlineSegIntersection.Intersect(v1, v2, w1, w2, Eps);
            AssertTangentIntersect(new Vector2<double>(1.0, -1.0), result);
        }
    }
}
