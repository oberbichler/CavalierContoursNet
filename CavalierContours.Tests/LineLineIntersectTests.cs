using System;
using Xunit;
using CavalierContours.Core;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of cavalier_contours 0.7.0 tests/test_line_line_intersect.rs
    /// </summary>
    public class LineLineIntersectTests
    {
        private const double Eps = 1e-5;

        private static readonly double[] TestRotationAngles =
        {
            Math.PI / 8.0,
            Math.PI / 6.0,
            Math.PI / 4.0,
            Math.PI / 3.0,
            Math.PI / 2.0
        };

        private static Vector2<double> V(double x, double y) => new(x, y);

        /// <summary>
        /// Reproduction of the Rust `assert_case_eq!` macro: the discriminants must match and,
        /// for the variants carrying data, every field must be fuzzy equal.
        /// </summary>
        private static void AssertCaseEq(LineLineIntr<double> left, LineLineIntr<double> right)
        {
            Assert.Equal(right.Kind, left.Kind);
            switch (right.Kind)
            {
                case LineLineIntrKind.NoIntersect:
                    break;
                case LineLineIntrKind.TrueIntersect:
                case LineLineIntrKind.FalseIntersect:
                    Assert.Equal(right.Seg1T, left.Seg1T, 10);
                    Assert.Equal(right.Seg2T, left.Seg2T, 10);
                    break;
                case LineLineIntrKind.Overlapping:
                    // seg2_t0 is stored in Seg2T, seg2_t1 in Seg2T1
                    Assert.Equal(right.Seg2T, left.Seg2T, 10);
                    Assert.Equal(right.Seg2T1, left.Seg2T1, 10);
                    break;
                default:
                    throw new InvalidOperationException($"unhandled kind: {right.Kind}");
            }
        }

        [Fact]
        public void TrueIntersect()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = V(-1.0, 1.0);
            var v2 = V(1.0, -1.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.5, 0.5));
        }

        [Fact]
        public void EndPointStartPointTouchSameDirection()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = V(1.0, 1.0);
            var v2 = V(2.0, 2.0);

            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(1.0, 0.0));

            // flip argument order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 1.0));

            // rotate v1->v2 should get same result
            foreach (double angle in TestRotationAngles)
            {
                var v2Rot = v2.RotateAbout(v1, angle);
                var rotResult = LineLineIntersection.Intersect(u1, u2, v1, v2Rot, Eps);
                AssertCaseEq(rotResult, LineLineIntr<double>.TrueIntersect(1.0, 0.0));
            }
        }

        [Fact]
        public void StartPointsTouchOpposingDirection()
        {
            var u1 = V(0.0, 0.0);
            var u2 = V(1.0, 1.0);
            var v1 = V(0.0, 0.0);
            var v2 = V(-1.0, -1.0);

            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 0.0));

            // flip argument order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 0.0));

            // rotate v1->v2 should get same result
            foreach (double angle in TestRotationAngles)
            {
                var v2Rot = v2.RotateAbout(v1, angle);
                var rotResult = LineLineIntersection.Intersect(u1, u2, v1, v2Rot, Eps);
                AssertCaseEq(rotResult, LineLineIntr<double>.TrueIntersect(0.0, 0.0));
            }
        }

        [Fact]
        public void FalseIntersect()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(-0.5, -0.5);
            var v1 = V(-1.0, 1.0);
            var v2 = V(1.0, -1.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.FalseIntersect(2.0, 0.5));
        }

        [Fact]
        public void NoIntersect()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = V(0.0, 1.0);
            var v2 = V(1.0, 2.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.NoIntersect);
        }

        [Fact]
        public void NoIntersectVertical()
        {
            var u1 = V(2.0, 0.0);
            var u2 = V(2.0, 1.0);
            var v1 = V(-1.0, -1.0);
            var v2 = V(-1.0, -2.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.NoIntersect);
        }

        [Fact]
        public void NoIntersectHorizontal()
        {
            var u1 = V(-2.0, -1.0);
            var u2 = V(2.0, -1.0);
            var v1 = V(-1.0, 5.0);
            var v2 = V(1.0, 5.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.NoIntersect);
        }

        [Fact]
        public void OverlappingIntersect()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = V(0.0, 0.0);
            var v2 = V(0.5, 0.5);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.Overlapping(0.0, 1.0));
        }

        [Fact]
        public void PointIntersect()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = V(0.0, 0.0);
            var v2 = V(0.0, 0.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.5, 0.0));

            // flip arg order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 0.5));
        }

        [Fact]
        public void PointIntersectVertical()
        {
            var u1 = V(0.0, -1.0);
            var u2 = V(0.0, 1.0);
            var v1 = V(0.0, 0.0);
            var v2 = V(0.0, 0.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.5, 0.0));

            // flip arg order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 0.5));
        }

        [Fact]
        public void PointIntersectHorizontal()
        {
            var u1 = V(-1.0, 0.0);
            var u2 = V(1.0, 0.0);
            var v1 = V(0.0, 0.0);
            var v2 = V(0.0, 0.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.5, 0.0));

            // flip arg order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 0.5));
        }

        [Fact]
        public void PointIntersectAtEnd()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = u1;
            var v2 = u1;
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 0.0));

            // flip arg order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 0.0));

            // other end
            v1 = u2;
            v2 = u2;
            result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(1.0, 0.0));

            // flip arg order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.TrueIntersect(0.0, 1.0));
        }

        [Fact]
        public void EntirelyOverlappingSameDirection()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = u1;
            var v2 = u2;
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.Overlapping(0.0, 1.0));

            // rotate both lines together
            foreach (double angle in TestRotationAngles)
            {
                var u2Rot = u2.RotateAbout(u1, angle);
                var v2Rot = v2.RotateAbout(v1, angle);
                var rotResult = LineLineIntersection.Intersect(u1, u2Rot, v1, v2Rot, Eps);
                AssertCaseEq(rotResult, LineLineIntr<double>.Overlapping(0.0, 1.0));
            }
        }

        [Fact]
        public void EntirelyOverlappingOpposingDirection()
        {
            var u1 = V(-1.0, -1.0);
            var u2 = V(1.0, 1.0);
            var v1 = u2;
            var v2 = u1;
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.Overlapping(0.0, 1.0));

            // flip arg order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            AssertCaseEq(result, LineLineIntr<double>.Overlapping(0.0, 1.0));

            // rotate both lines together
            foreach (double angle in TestRotationAngles)
            {
                var u2Rot = u2.RotateAbout(u1, angle);
                var v1Rot = v1.RotateAbout(v2, angle);
                var rotResult = LineLineIntersection.Intersect(u1, u2Rot, v1Rot, v2, Eps);
                AssertCaseEq(rotResult, LineLineIntr<double>.Overlapping(0.0, 1.0));
            }
        }
    }
}
