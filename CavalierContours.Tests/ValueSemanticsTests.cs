using CavalierContours.Core;
using CavalierContours.Polyline;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Pins the value semantics of the geometry primitives against the Rust reference.
    /// Rust derives PartialEq for these types, which is IEEE 754 comparison: NaN never equals
    /// itself and +0.0 equals -0.0.
    /// </summary>
    public class ValueSemanticsTests
    {
        private const double Eps = 1e-5;

        /// <summary>
        /// Upstream 0.8.0 uses `diff &lt; 0` for this branch, not `&lt;=`. The zero case is
        /// result-equivalent because h is then 0, so point1 == point2 exactly and the following
        /// FuzzyEqEps branch returns the same midpoint. This test pins that equivalence.
        /// </summary>
        [Fact]
        public void ExternallyTangentCirclesReturnSingleTouchPoint()
        {
            var result = CircleCircleIntersection.Intersect(
                1.0, new Vector2<double>(0.0, 0.0),
                1.0, new Vector2<double>(2.0, 0.0),
                Eps);

            Assert.Equal(CircleCircleIntrKind.TangentIntersect, result.Kind);
            Assert.Equal(1.0, result.Point1.X, 12);
            Assert.Equal(0.0, result.Point1.Y, 12);
        }

        [Fact]
        public void Vector2EqualityMatchesIeee754ForNaN()
        {
            var a = new Vector2<double>(double.NaN, 0.0);
            var b = new Vector2<double>(double.NaN, 0.0);

            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void Vector2EqualityForNormalValues()
        {
            Assert.True(new Vector2<double>(1.0, 2.0) == new Vector2<double>(1.0, 2.0));
            Assert.False(new Vector2<double>(1.0, 2.0) == new Vector2<double>(1.0, 2.5));
            // Rust: 0.0 == -0.0 is true for f64.
            Assert.True(new Vector2<double>(0.0, 0.0) == new Vector2<double>(-0.0, -0.0));
            Assert.Equal(
                new Vector2<double>(0.0, 0.0).GetHashCode(),
                new Vector2<double>(-0.0, -0.0).GetHashCode());
        }

        [Fact]
        public void AabbEqualityMatchesIeee754ForNaN()
        {
            var a = new AABB<double>(double.NaN, 0.0, 1.0, 1.0);
            var b = new AABB<double>(double.NaN, 0.0, 1.0, 1.0);

            Assert.False(a.Equals(b));
            Assert.True(new AABB<double>(0.0, 0.0, 1.0, 1.0) == new AABB<double>(-0.0, -0.0, 1.0, 1.0));
        }

        [Fact]
        public void PlineVertexEqualityMatchesIeee754ForNaN()
        {
            var a = new PlineVertex<double>(double.NaN, 0.0, 0.0);
            var b = new PlineVertex<double>(double.NaN, 0.0, 0.0);

            Assert.False(a.Equals(b));
            Assert.True(new PlineVertex<double>(1.0, 2.0, 0.5) == new PlineVertex<double>(1.0, 2.0, 0.5));
            Assert.True(new PlineVertex<double>(0.0, 0.0, 0.0) == new PlineVertex<double>(-0.0, -0.0, -0.0));
        }
    }
}
