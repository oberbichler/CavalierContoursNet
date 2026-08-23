using System;
using Xunit;
using CavalierContours.Core;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of cavalier_contours 0.7.0 tests/test_circle_circle_intersect.rs
    /// </summary>
    public class CircleCircleIntersectTests
    {
        private const double Eps = 1e-5;

        private static Vector2<double> V(double x, double y) => new(x, y);

        /// <summary>
        /// Reproduction of the Rust `assert_case_eq!` macro: the discriminants must match and,
        /// for the variants carrying data, every field must be fuzzy equal.
        /// </summary>
        private static void AssertCaseEq(CircleCircleIntr<double> left, CircleCircleIntr<double> right)
        {
            Assert.Equal(right.Kind, left.Kind);
            switch (right.Kind)
            {
                case CircleCircleIntrKind.NoIntersect:
                case CircleCircleIntrKind.Overlapping:
                    break;
                case CircleCircleIntrKind.TangentIntersect:
                    AssertPointEq(right.Point1, left.Point1);
                    break;
                case CircleCircleIntrKind.TwoIntersects:
                    AssertPointEq(right.Point1, left.Point1);
                    AssertPointEq(right.Point2, left.Point2);
                    break;
                default:
                    throw new InvalidOperationException($"unhandled kind: {right.Kind}");
            }
        }

        private static void AssertPointEq(Vector2<double> expected, Vector2<double> actual)
        {
            Assert.Equal(expected.X, actual.X, 10);
            Assert.Equal(expected.Y, actual.Y, 10);
        }

        [Fact]
        public void NoIntersectOutside()
        {
            double r1 = 1.0;
            var c1 = V(-1.0, -1.0);
            double r2 = 0.5;
            var c2 = V(0.0, 5.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            AssertCaseEq(result, CircleCircleIntr<double>.NoIntersect);
        }

        [Fact]
        public void NoIntersectInside()
        {
            double r1 = 5.0;
            var c1 = V(-1.0, -1.0);
            double r2 = 0.5;
            var c2 = V(1.0, 1.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            AssertCaseEq(result, CircleCircleIntr<double>.NoIntersect);
        }

        [Fact]
        public void TangentIntersectOutside()
        {
            double r1 = 1.0;
            var c1 = V(-1.0, 1.0);
            double r2 = 0.5;
            var c2 = V(0.5, 1.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            AssertCaseEq(result, CircleCircleIntr<double>.TangentIntersect(V(0.0, 1.0)));
        }

        [Fact]
        public void TangentIntersectInside()
        {
            double r1 = 3.0;
            var c1 = V(0.0, 1.0);
            double r2 = 4.0;
            var c2 = V(0.0, 0.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            AssertCaseEq(result, CircleCircleIntr<double>.TangentIntersect(V(0.0, 4.0)));
        }

        [Fact]
        public void TwoIntersects()
        {
            double r1 = 3.0;
            var c1 = V(0.0, 1.0);
            double r2 = 4.0;
            var c2 = V(5.0, 5.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            var expectedPoint1 = V(2.945782625365772, 1.567771718292785);
            var expectedPoint2 = V(1.2005588380488623, 3.749301452438922);
            AssertCaseEq(result, CircleCircleIntr<double>.TwoIntersects(expectedPoint1, expectedPoint2));
        }

        [Fact]
        public void Overlapping()
        {
            double r1 = 1.0;
            var c1 = V(-1.0, 1.0);
            double r2 = r1;
            var c2 = c1;
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            AssertCaseEq(result, CircleCircleIntr<double>.Overlapping);
        }
    }
}
