using System;
using CavalierContours.Core;
using Xunit;

namespace CavalierContours.Tests
{
    public class BaseMathTests
    {
        [Fact]
        public void QuadraticSolutionsAcceptsValidDiscriminant()
        {
            // x^2 - 3x + 2 = 0 -> roots 2 and 1, discriminant 1, sqrt(discriminant) 1.
            var (sol1, sol2) = BaseMath.QuadraticSolutions(1.0, -3.0, 2.0, 1.0);

            Assert.Equal(2.0, sol1, 12);
            Assert.Equal(1.0, sol2, 12);
        }

        [Fact]
        public void QuadraticSolutionsWithLargeCoefficients()
        {
            // 2x^2 + 1000x + 3 = 0, discriminant 1000000 - 24 = 999976.
            double sqrtDisc = Math.Sqrt(999976.0);
            var (sol1, sol2) = BaseMath.QuadraticSolutions(2.0, 1000.0, 3.0, sqrtDisc);

            Assert.Equal(0.0, (2.0 * sol1 * sol1) + (1000.0 * sol1) + 3.0, 9);
            Assert.Equal(0.0, (2.0 * sol2 * sol2) + (1000.0 * sol2) + 3.0, 9);
        }

        [Fact]
        public void MinMaxOrdersNormalValues()
        {
            Assert.Equal((1.0, 2.0), BaseMath.MinMax(1.0, 2.0));
            Assert.Equal((1.0, 2.0), BaseMath.MinMax(2.0, 1.0));
            Assert.Equal((1.0, 1.0), BaseMath.MinMax(1.0, 1.0));
        }

        [Fact]
        public void MinMaxMatchesRustNaNOrdering()
        {
            // Rust: `if v1 < v2 { (v1, v2) } else { (v2, v1) }`. Any comparison with NaN is
            // false, so the else branch is taken and NaN ends up in the second slot when it is
            // the first argument.
            var (a, b) = BaseMath.MinMax(double.NaN, 1.0);
            Assert.Equal(1.0, a);
            Assert.True(double.IsNaN(b));

            var (c, d) = BaseMath.MinMax(1.0, double.NaN);
            Assert.True(double.IsNaN(c));
            Assert.Equal(1.0, d);
        }
        /// <summary>
        /// Ported from the <c>#[cfg(test)]</c> module added to base_math.rs in upstream 0.8.0
        /// together with the scale-invariant rewrite of point_within_arc_sweep.
        /// </summary>
        [Fact]
        public void SmallArcSweepExcludesOppositePointAtDifferentScales()
        {
            var center = new Vector2<double>(27.4604, 13.6769);
            var arcStart = new Vector2<double>(27.455462563050666, 13.676111510068951);
            var arcEnd = new Vector2<double>(27.45546524477275, 13.676094896995954);
            var oppositePoint = new Vector2<double>(27.465336109592272, 13.67769680169451);

            foreach (double scale in new[] { 0.001, 1.0, 1000.0 })
            {
                Vector2<double> ScalePoint(Vector2<double> p) => center + (p - center).Scale(scale);

                Assert.False(
                    BaseMath.PointWithinArcSweep(
                        center,
                        ScalePoint(arcStart),
                        ScalePoint(arcEnd),
                        isClockwise: false,
                        ScalePoint(oppositePoint),
                        1e-5 * scale),
                    $"opposite point must be excluded at scale {scale}");
            }
        }

        /// <summary>
        /// The tolerance must be positional, not a raw cross product test. Expectations verified
        /// against the 0.8.0 crate.
        /// </summary>
        [Fact]
        public void ArcSweepToleranceIsPositionBasedNotAngleBased()
        {
            const double eps = 1e-5;
            var center = new Vector2<double>(0.0, 0.0);

            foreach (double radius in new[] { 0.001, 1.0, 1000.0 })
            {
                var arcStart = new Vector2<double>(radius, 0.0);
                var arcEnd = new Vector2<double>(0.0, radius);

                // Half an epsilon below the start ray, i.e. just outside the CCW sweep.
                Assert.True(
                    BaseMath.PointWithinArcSweep(
                        center, arcStart, arcEnd, false, new Vector2<double>(radius, -0.5 * eps), eps),
                    $"point 0.5*eps outside must be included at radius {radius}");

                // Two epsilon outside must be excluded regardless of radius.
                Assert.False(
                    BaseMath.PointWithinArcSweep(
                        center, arcStart, arcEnd, false, new Vector2<double>(radius, -2.0 * eps), eps),
                    $"point 2*eps outside must be excluded at radius {radius}");
            }
        }

        /// <summary>
        /// The center is the sweep apex; points within the position tolerance of it are inside.
        /// </summary>
        [Fact]
        public void ArcSweepIncludesPointsAtTheApex()
        {
            var center = new Vector2<double>(5.0, 5.0);
            var arcStart = new Vector2<double>(6.0, 5.0);
            var arcEnd = new Vector2<double>(5.0, 6.0);

            Assert.True(BaseMath.PointWithinArcSweep(center, arcStart, arcEnd, false, center, 1e-5));
            Assert.True(BaseMath.PointWithinArcSweep(
                center, arcStart, arcEnd, false, new Vector2<double>(5.0, 5.0 - 5e-6), 1e-5));
        }

    }
}
