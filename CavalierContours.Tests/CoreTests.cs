using System;
using Xunit;
using CavalierContours.Core;

namespace CavalierContours.Tests
{
    public class CoreTests
    {
        private const double Eps = 1e-5;

        // ==========================================
        // CIRCLE-CIRCLE INTERSECTION TESTS
        // ==========================================

        [Fact]
        public void CircleCircleNoIntersectOutside()
        {
            double r1 = 1.0;
            var c1 = new Vector2<double>(-1.0, -1.0);
            double r2 = 0.5;
            var c2 = new Vector2<double>(0.0, 5.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            Assert.Equal(CircleCircleIntrKind.NoIntersect, result.Kind);
        }

        [Fact]
        public void CircleCircleNoIntersectInside()
        {
            double r1 = 5.0;
            var c1 = new Vector2<double>(-1.0, -1.0);
            double r2 = 0.5;
            var c2 = new Vector2<double>(1.0, 1.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            Assert.Equal(CircleCircleIntrKind.NoIntersect, result.Kind);
        }

        [Fact]
        public void CircleCircleTangentIntersectOutside()
        {
            double r1 = 1.0;
            var c1 = new Vector2<double>(-1.0, 1.0);
            double r2 = 0.5;
            var c2 = new Vector2<double>(0.5, 1.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            Assert.Equal(CircleCircleIntrKind.TangentIntersect, result.Kind);
            Assert.True(result.Point1.FuzzyEq(new Vector2<double>(0.0, 1.0)));
        }

        [Fact]
        public void CircleCircleTangentIntersectInside()
        {
            double r1 = 3.0;
            var c1 = new Vector2<double>(0.0, 1.0);
            double r2 = 4.0;
            var c2 = new Vector2<double>(0.0, 0.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            Assert.Equal(CircleCircleIntrKind.TangentIntersect, result.Kind);
            Assert.True(result.Point1.FuzzyEq(new Vector2<double>(0.0, 4.0)));
        }

        [Fact]
        public void CircleCircleTwoIntersects()
        {
            double r1 = 3.0;
            var c1 = new Vector2<double>(0.0, 1.0);
            double r2 = 4.0;
            var c2 = new Vector2<double>(5.0, 5.0);
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            Assert.Equal(CircleCircleIntrKind.TwoIntersects, result.Kind);
            var expectedPoint1 = new Vector2<double>(2.945782625365772, 1.567771718292785);
            var expectedPoint2 = new Vector2<double>(1.2005588380488623, 3.749301452438922);
            Assert.True(result.Point1.FuzzyEq(expectedPoint1));
            Assert.True(result.Point2.FuzzyEq(expectedPoint2));
        }

        [Fact]
        public void CircleCircleOverlapping()
        {
            double r1 = 1.0;
            var c1 = new Vector2<double>(-1.0, 1.0);
            double r2 = r1;
            var c2 = c1;
            var result = CircleCircleIntersection.Intersect(r1, c1, r2, c2, Eps);
            Assert.Equal(CircleCircleIntrKind.Overlapping, result.Kind);
        }

        // ==========================================
        // LINE-CIRCLE INTERSECTION TESTS
        // ==========================================

        [Fact]
        public void LineCircleNoIntersect()
        {
            var p0 = new Vector2<double>(-1.0, -1.0);
            var p1 = new Vector2<double>(1.0, 1.0);
            var center = new Vector2<double>(0.0, 5.0);
            double radius = 0.5;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.NoIntersect, result.Kind);
        }

        [Fact]
        public void LineCircleNoIntersectVertical()
        {
            var p0 = new Vector2<double>(0.0, -1.0);
            var p1 = new Vector2<double>(0.0, 1.0);
            var center = new Vector2<double>(2.0, 0.0);
            double radius = 0.5;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.NoIntersect, result.Kind);
        }

        [Fact]
        public void LineCircleNoIntersectHorizontal()
        {
            var p0 = new Vector2<double>(1.0, 1.0);
            var p1 = new Vector2<double>(3.0, 1.0);
            var center = new Vector2<double>(2.0, -2.0);
            double radius = 0.5;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.NoIntersect, result.Kind);
        }

        [Fact]
        public void LineCircleTwoIntersectsTrue()
        {
            var p0 = new Vector2<double>(-1.0, -1.0);
            var p1 = new Vector2<double>(1.0, 1.0);
            double radius = 0.5;
            double offset = Math.Sqrt(radius * radius / 2.0);
            var center = new Vector2<double>(offset, offset);
            double expectedT1IntrPointX = 2.0 * offset;
            double expectedT1 = (expectedT1IntrPointX - p0.X) / (p1.X - p0.X);
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.TwoIntersects, result.Kind);
            Assert.True(result.T0.FuzzyEq(0.5));
            Assert.True(result.T1.FuzzyEq(expectedT1));
        }

        [Fact]
        public void LineCircleTwoIntersectsSegInsideVertical()
        {
            var p0 = new Vector2<double>(0.0, -1.0);
            var p1 = new Vector2<double>(0.0, 1.0);
            var center = new Vector2<double>(0.0, 0.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.TwoIntersects, result.Kind);
            Assert.True(result.T0.FuzzyEq(0.0));
            Assert.True(result.T1.FuzzyEq(1.0));
        }

        [Fact]
        public void LineCircleTwoIntersectsSegInsideHorizontal()
        {
            var p0 = new Vector2<double>(-1.0, 0.0);
            var p1 = new Vector2<double>(1.0, 0.0);
            var center = new Vector2<double>(0.0, 0.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.TwoIntersects, result.Kind);
            Assert.True(result.T0.FuzzyEq(0.0));
            Assert.True(result.T1.FuzzyEq(1.0));
        }

        [Fact]
        public void LineCircleTangentIntersectVertical()
        {
            var p0 = new Vector2<double>(0.0, -1.0);
            var p1 = new Vector2<double>(0.0, 1.0);
            var center = new Vector2<double>(1.0, 0.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.TangentIntersect, result.Kind);
            Assert.True(result.T0.FuzzyEq(0.5));
        }

        [Fact]
        public void LineCircleTangentIntersectHorizontal()
        {
            var p0 = new Vector2<double>(-1.0, 0.0);
            var p1 = new Vector2<double>(1.0, 0.0);
            var center = new Vector2<double>(0.0, -1.0);
            double radius = 1.0;
            var result = LineCircleIntersection.Intersect(p0, p1, radius, center, Eps);
            Assert.Equal(LineCircleIntrKind.TangentIntersect, result.Kind);
            Assert.True(result.T0.FuzzyEq(0.5));
        }

        // ==========================================
        // LINE-LINE INTERSECTION TESTS
        // ==========================================

        [Fact]
        public void LineLineTrueIntersect()
        {
            var u1 = new Vector2<double>(-1.0, -1.0);
            var u2 = new Vector2<double>(1.0, 1.0);
            var v1 = new Vector2<double>(-1.0, 1.0);
            var v2 = new Vector2<double>(1.0, -1.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            Assert.Equal(LineLineIntrKind.TrueIntersect, result.Kind);
            Assert.True(result.Seg1T.FuzzyEq(0.5));
            Assert.True(result.Seg2T.FuzzyEq(0.5));
        }

        [Fact]
        public void LineLineEndPointStartPointTouchSameDirection()
        {
            var u1 = new Vector2<double>(-1.0, -1.0);
            var u2 = new Vector2<double>(1.0, 1.0);
            var v1 = new Vector2<double>(1.0, 1.0);
            var v2 = new Vector2<double>(2.0, 2.0);

            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            Assert.Equal(LineLineIntrKind.TrueIntersect, result.Kind);
            Assert.True(result.Seg1T.FuzzyEq(1.0));
            Assert.True(result.Seg2T.FuzzyEq(0.0));

            // flip argument order
            result = LineLineIntersection.Intersect(v1, v2, u1, u2, Eps);
            Assert.Equal(LineLineIntrKind.TrueIntersect, result.Kind);
            Assert.True(result.Seg1T.FuzzyEq(0.0));
            Assert.True(result.Seg2T.FuzzyEq(1.0));
        }

        [Fact]
        public void LineLineFalseIntersect()
        {
            var u1 = new Vector2<double>(-1.0, -1.0);
            var u2 = new Vector2<double>(-0.5, -0.5);
            var v1 = new Vector2<double>(-1.0, 1.0);
            var v2 = new Vector2<double>(1.0, -1.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            Assert.Equal(LineLineIntrKind.FalseIntersect, result.Kind);
            Assert.True(result.Seg1T.FuzzyEq(2.0));
            Assert.True(result.Seg2T.FuzzyEq(0.5));
        }

        [Fact]
        public void LineLineNoIntersect()
        {
            var u1 = new Vector2<double>(-1.0, -1.0);
            var u2 = new Vector2<double>(1.0, 1.0);
            var v1 = new Vector2<double>(0.0, 1.0);
            var v2 = new Vector2<double>(1.0, 2.0);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            Assert.Equal(LineLineIntrKind.NoIntersect, result.Kind);
        }

        [Fact]
        public void LineLineOverlappingIntersect()
        {
            var u1 = new Vector2<double>(-1.0, -1.0);
            var u2 = new Vector2<double>(1.0, 1.0);
            var v1 = new Vector2<double>(0.0, 0.0);
            var v2 = new Vector2<double>(0.5, 0.5);
            var result = LineLineIntersection.Intersect(u1, u2, v1, v2, Eps);
            Assert.Equal(LineLineIntrKind.Overlapping, result.Kind);
            Assert.True(result.Seg2T.FuzzyEq(0.0));
            Assert.True(result.Seg2T1.FuzzyEq(1.0));
        }
    }
}
