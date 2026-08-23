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
    }
}
