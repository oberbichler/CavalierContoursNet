using System;
using Xunit;
using CavalierContours.Core;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of cavalier_contours 0.7.0 tests/test_line_circle_intersect.rs
    /// </summary>
    public class LineCircleIntersectTests
    {
        private const double Eps = 1e-5;

        private static Vector2<double> V(double x, double y) => new(x, y);

        /// <summary>
        /// Reproduction of the Rust `assert_case_eq!` macro: the discriminants must match and,
        /// for the variants carrying data, every field must be fuzzy equal.
        /// </summary>
        private static void AssertCaseEq(LineCircleIntr<double> left, LineCircleIntr<double> right)
        {
            Assert.Equal(right.Kind, left.Kind);
            switch (right.Kind)
            {
                case LineCircleIntrKind.NoIntersect:
                    break;
                case LineCircleIntrKind.TangentIntersect:
                    Assert.Equal(right.T0, left.T0, 10);
                    break;
                case LineCircleIntrKind.TwoIntersects:
                    Assert.Equal(right.T0, left.T0, 10);
                    Assert.Equal(right.T1, left.T1, 10);
                    break;
                default:
                    throw new InvalidOperationException($"unhandled kind: {right.Kind}");
            }
        }

        [Fact]
        public void NoIntersect()
        {
            var p0 = V(-1.0, -1.0);
            var p1 = V(1.0, 1.0);
            var circleCenter = V(0.0, 5.0);
            double radius = 0.5;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.NoIntersect);
        }

        [Fact]
        public void NoIntersectVertical()
        {
            var p0 = V(0.0, -1.0);
            var p1 = V(0.0, 1.0);
            var circleCenter = V(2.0, 0.0);
            double radius = 0.5;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.NoIntersect);
        }

        [Fact]
        public void NoIntersectHorizontal()
        {
            var p0 = V(1.0, 1.0);
            var p1 = V(3.0, 1.0);
            var circleCenter = V(2.0, -2.0);
            double radius = 0.5;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.NoIntersect);
        }

        [Fact]
        public void TwoIntersectsTrue()
        {
            var p0 = V(-1.0, -1.0);
            var p1 = V(1.0, 1.0);
            // placing edge of circle at (0, 0)
            double radius = 0.5;
            double offset = Math.Sqrt(radius * radius / 2.0);
            var circleCenter = V(offset, offset);
            double expectedT1IntrPointX = 2.0 * offset;
            double expectedT1 = (expectedT1IntrPointX - p0.X) / (p1.X - p0.X);
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.TwoIntersects(0.5, expectedT1));
        }

        [Fact]
        public void TwoIntersectsSegInsideVertical()
        {
            var p0 = V(0.0, -1.0);
            var p1 = V(0.0, 1.0);
            var circleCenter = V(0.0, 0.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.TwoIntersects(0.0, 1.0));
        }

        [Fact]
        public void TwoIntersectsSegInsideHorizontal()
        {
            var p0 = V(-1.0, 0.0);
            var p1 = V(1.0, 0.0);
            var circleCenter = V(0.0, 0.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.TwoIntersects(0.0, 1.0));
        }

        [Fact]
        public void TwoIntersectsSegTouching()
        {
            var p0 = V(0.0, -1.0);
            var p1 = V(0.0, 1.0);
            var circleCenter = V(0.0, 0.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.TwoIntersects(0.0, 1.0));
        }

        [Fact]
        public void TangentIntersectVertical()
        {
            var p0 = V(0.0, -1.0);
            var p1 = V(0.0, 1.0);
            var circleCenter = V(1.0, 0.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.TangentIntersect(0.5));
        }

        [Fact]
        public void TangentIntersectHorizontal()
        {
            var p0 = V(-1.0, 0.0);
            var p1 = V(1.0, 0.0);
            var circleCenter = V(0.0, -1.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.TangentIntersect(0.5));
        }

        [Fact]
        public void TangentAtStartPoint()
        {
            // this is a case that previously failed due to numeric stability issues
            var p0 = V(161.29, 113.665);
            var p1 = V(167.64, 113.665);
            var circleCenter = V(161.29, 114.30000000000001);
            double radius = 0.634999999999998;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, circleCenter, Eps);
            AssertCaseEq(result, LineCircleIntr<double>.TangentIntersect(0.0));
        }
        // ------------------------------------------------------------------
        // Added in upstream 0.8.0 together with the behaviour change in
        // line_circle_intersect.rs. Ported verbatim from
        // tests/test_line_circle_intersect.rs at tag 0.8.0.
        // ------------------------------------------------------------------

        [Fact]
        public void NearTangentIntersectionsUsePositionEpsilonAtDifferentRadii()
        {
            var p0 = V(-1.0, 0.0);
            var p1 = V(1.0, 0.0);
            const double halfIntersectSpacing = 5e-4;

            foreach (double radius in new[] { 0.01, 1.0, 100.0 })
            {
                double centerY = System.Math.Sqrt((radius * radius) - (halfIntersectSpacing * halfIntersectSpacing));
                var result = LineCircleIntersection.Intersect(p0, p1, radius, V(0.0, centerY), Eps);

                // Upstream asserts with fuzzy_eq, i.e. a tolerance of 1e-8. At radius 100 the
                // computed t is 0.49974999995768304; Rust 0.8.0 produces that same value
                // bit-for-bit, so the deviation from the analytic 0.49975 is inherent to the
                // algorithm, not to this port.
                AssertCaseEq(
                    result,
                    LineCircleIntr<double>.TwoIntersects(
                        0.5 - (halfIntersectSpacing / 2.0),
                        0.5 + (halfIntersectSpacing / 2.0)));
            }
        }

        [Fact]
        public void NearTangentIntersectionsSnapWhenPositionsAreEqual()
        {
            var p0 = V(-1.0, 0.0);
            var p1 = V(1.0, 0.0);
            const double halfIntersectSpacing = 2e-6;

            foreach (double radius in new[] { 0.01, 1.0, 100.0 })
            {
                double centerY = System.Math.Sqrt((radius * radius) - (halfIntersectSpacing * halfIntersectSpacing));
                var result = LineCircleIntersection.Intersect(p0, p1, radius, V(0.0, centerY), Eps);

                AssertCaseEq(result, LineCircleIntr<double>.TangentIntersect(0.5));
            }
        }

        [Fact]
        public void PointSegmentUsesPositionEpsilon()
        {
            var point = V(0.0, 100.001);
            var result = LineCircleIntersection.Intersect(point, point, 100.0, V(0.0, 0.0), 0.01);

            AssertCaseEq(result, LineCircleIntr<double>.TangentIntersect(0.0));
        }

    }
}
