using System;
using CavalierContours.Core;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Pins the value semantics of the core intersection result structs. Rust derives
    /// PartialEq for the corresponding enums, which is IEEE 754 comparison on the payload:
    /// NaN never equals itself and +0.0 equals -0.0.
    /// </summary>
    public class CoreIntrValueSemanticsTests
    {
        // ---------------------------------------------------------------- IEquatable<T>

        [Fact]
        public void LineLineIntrImplementsIEquatable()
        {
            Assert.True(typeof(IEquatable<LineLineIntr<double>>).IsAssignableFrom(typeof(LineLineIntr<double>)));
        }

        [Fact]
        public void LineCircleIntrImplementsIEquatable()
        {
            Assert.True(typeof(IEquatable<LineCircleIntr<double>>).IsAssignableFrom(typeof(LineCircleIntr<double>)));
        }

        [Fact]
        public void CircleCircleIntrImplementsIEquatable()
        {
            Assert.True(typeof(IEquatable<CircleCircleIntr<double>>).IsAssignableFrom(typeof(CircleCircleIntr<double>)));
        }

        // ---------------------------------------------------------------- LineLineIntr

        [Fact]
        public void LineLineIntrEqualValuesAreEqual()
        {
            var a = LineLineIntr<double>.TrueIntersect(0.25, 0.75);
            var b = LineLineIntr<double>.TrueIntersect(0.25, 0.75);

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals((object)b));
        }

        [Fact]
        public void LineLineIntrDifferentPayloadIsNotEqual()
        {
            var a = LineLineIntr<double>.TrueIntersect(0.25, 0.75);

            Assert.True(a != LineLineIntr<double>.TrueIntersect(0.25, 0.5));
            Assert.True(a != LineLineIntr<double>.TrueIntersect(0.5, 0.75));
        }

        [Fact]
        public void LineLineIntrDifferentKindIsNotEqual()
        {
            // Same payload fields, different discriminant.
            Assert.True(LineLineIntr<double>.TrueIntersect(0.25, 0.75)
                     != LineLineIntr<double>.FalseIntersect(0.25, 0.75));
            // Overlapping stores its parameters in Seg2T/Seg2T1, TrueIntersect in Seg1T/Seg2T,
            // so Kind is what keeps these apart.
            Assert.True(LineLineIntr<double>.Overlapping(0.25, 0.75)
                     != LineLineIntr<double>.TrueIntersect(0.25, 0.75));
            Assert.True(LineLineIntr<double>.NoIntersect != LineLineIntr<double>.TrueIntersect(0.0, 0.0));
        }

        [Fact]
        public void LineLineIntrOverlappingComparesBothParameters()
        {
            Assert.True(LineLineIntr<double>.Overlapping(0.25, 0.75) == LineLineIntr<double>.Overlapping(0.25, 0.75));
            Assert.True(LineLineIntr<double>.Overlapping(0.25, 0.75) != LineLineIntr<double>.Overlapping(0.25, 0.8));
            Assert.True(LineLineIntr<double>.Overlapping(0.25, 0.75) != LineLineIntr<double>.Overlapping(0.3, 0.75));
        }

        [Fact]
        public void LineLineIntrNaNIsNotEqualToItself()
        {
            var a = LineLineIntr<double>.TrueIntersect(double.NaN, 0.0);
            var b = LineLineIntr<double>.TrueIntersect(double.NaN, 0.0);

            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);

            var c = LineLineIntr<double>.Overlapping(0.0, double.NaN);
            Assert.True(c != LineLineIntr<double>.Overlapping(0.0, double.NaN));
        }

        [Fact]
        public void LineLineIntrTreatsNegativeZeroAsZero()
        {
            var pos = LineLineIntr<double>.TrueIntersect(0.0, 0.0);
            var neg = LineLineIntr<double>.TrueIntersect(-0.0, -0.0);

            Assert.True(pos == neg);
            Assert.Equal(pos.GetHashCode(), neg.GetHashCode());
        }

        [Fact]
        public void LineLineIntrEqualValuesShareHashCode()
        {
            Assert.Equal(
                LineLineIntr<double>.TrueIntersect(0.25, 0.75).GetHashCode(),
                LineLineIntr<double>.TrueIntersect(0.25, 0.75).GetHashCode());
            Assert.Equal(
                LineLineIntr<double>.Overlapping(0.25, 0.75).GetHashCode(),
                LineLineIntr<double>.Overlapping(0.25, 0.75).GetHashCode());
        }

        [Fact]
        public void LineLineIntrIsNotEqualToOtherTypes()
        {
            Assert.False(LineLineIntr<double>.NoIntersect.Equals("not an intersection"));
            Assert.False(LineLineIntr<double>.NoIntersect.Equals(null));
        }

        // ------------------------------------------------- LineLineIntr named accessors

        [Fact]
        public void OverlapAccessorsAliasTheUnderlyingFields()
        {
            var r = LineLineIntr<double>.Overlapping(0.25, 0.75);

            Assert.Equal(LineLineIntrKind.Overlapping, r.Kind);
            Assert.Equal(0.25, r.OverlapSeg2T0);
            Assert.Equal(0.75, r.OverlapSeg2T1);
            Assert.Equal(r.Seg2T, r.OverlapSeg2T0);
            Assert.Equal(r.Seg2T1, r.OverlapSeg2T1);
            // Seg1T is unused for Overlapping.
            Assert.Equal(0.0, r.Seg1T);
        }

        // -------------------------------------------------------------- LineCircleIntr

        [Fact]
        public void LineCircleIntrEqualValuesAreEqual()
        {
            var a = LineCircleIntr<double>.TwoIntersects(0.25, 0.75);
            var b = LineCircleIntr<double>.TwoIntersects(0.25, 0.75);

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals((object)b));
        }

        [Fact]
        public void LineCircleIntrDifferentValuesAreNotEqual()
        {
            var a = LineCircleIntr<double>.TwoIntersects(0.25, 0.75);

            Assert.True(a != LineCircleIntr<double>.TwoIntersects(0.25, 0.5));
            Assert.True(a != LineCircleIntr<double>.NoIntersect);
            // Same payload in T0, different discriminant.
            Assert.True(LineCircleIntr<double>.TangentIntersect(0.25) != LineCircleIntr<double>.TwoIntersects(0.25, 0.0));
        }

        [Fact]
        public void LineCircleIntrNaNIsNotEqualToItself()
        {
            var a = LineCircleIntr<double>.TwoIntersects(double.NaN, 1.0);
            var b = LineCircleIntr<double>.TwoIntersects(double.NaN, 1.0);

            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void LineCircleIntrTreatsNegativeZeroAsZero()
        {
            var pos = LineCircleIntr<double>.TwoIntersects(0.0, 0.0);
            var neg = LineCircleIntr<double>.TwoIntersects(-0.0, -0.0);

            Assert.True(pos == neg);
            Assert.Equal(pos.GetHashCode(), neg.GetHashCode());
        }

        [Fact]
        public void LineCircleIntrEqualValuesShareHashCode()
        {
            Assert.Equal(
                LineCircleIntr<double>.TwoIntersects(0.25, 0.75).GetHashCode(),
                LineCircleIntr<double>.TwoIntersects(0.25, 0.75).GetHashCode());
        }

        [Fact]
        public void LineCircleIntrIsNotEqualToOtherTypes()
        {
            Assert.False(LineCircleIntr<double>.NoIntersect.Equals("not an intersection"));
            Assert.False(LineCircleIntr<double>.NoIntersect.Equals(null));
        }

        // ------------------------------------------------------------ CircleCircleIntr

        [Fact]
        public void CircleCircleIntrEqualValuesAreEqual()
        {
            var a = CircleCircleIntr<double>.TwoIntersects(new Vector2<double>(1.0, 2.0), new Vector2<double>(3.0, 4.0));
            var b = CircleCircleIntr<double>.TwoIntersects(new Vector2<double>(1.0, 2.0), new Vector2<double>(3.0, 4.0));

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.False(a != b);
            Assert.True(a.Equals((object)b));
        }

        [Fact]
        public void CircleCircleIntrDifferentValuesAreNotEqual()
        {
            var a = CircleCircleIntr<double>.TwoIntersects(new Vector2<double>(1.0, 2.0), new Vector2<double>(3.0, 4.0));

            Assert.True(a != CircleCircleIntr<double>.TwoIntersects(new Vector2<double>(1.0, 2.0), new Vector2<double>(3.0, 5.0)));
            Assert.True(CircleCircleIntr<double>.NoIntersect != CircleCircleIntr<double>.Overlapping);
            // Same payload, different discriminant.
            Assert.True(CircleCircleIntr<double>.TangentIntersect(new Vector2<double>(1.0, 2.0))
                     != CircleCircleIntr<double>.TwoIntersects(new Vector2<double>(1.0, 2.0), Vector2<double>.Zero));
        }

        [Fact]
        public void CircleCircleIntrNaNIsNotEqualToItself()
        {
            var a = CircleCircleIntr<double>.TangentIntersect(new Vector2<double>(double.NaN, 0.0));
            var b = CircleCircleIntr<double>.TangentIntersect(new Vector2<double>(double.NaN, 0.0));

            Assert.False(a.Equals(b));
            Assert.False(a == b);
            Assert.True(a != b);
        }

        [Fact]
        public void CircleCircleIntrTreatsNegativeZeroAsZero()
        {
            var pos = CircleCircleIntr<double>.TwoIntersects(new Vector2<double>(0.0, 0.0), new Vector2<double>(0.0, 0.0));
            var neg = CircleCircleIntr<double>.TwoIntersects(new Vector2<double>(-0.0, -0.0), new Vector2<double>(-0.0, -0.0));

            Assert.True(pos == neg);
            Assert.Equal(pos.GetHashCode(), neg.GetHashCode());
        }

        [Fact]
        public void CircleCircleIntrEqualValuesShareHashCode()
        {
            Assert.Equal(
                CircleCircleIntr<double>.TangentIntersect(new Vector2<double>(1.0, 2.0)).GetHashCode(),
                CircleCircleIntr<double>.TangentIntersect(new Vector2<double>(1.0, 2.0)).GetHashCode());
        }

        [Fact]
        public void CircleCircleIntrIsNotEqualToOtherTypes()
        {
            Assert.False(CircleCircleIntr<double>.NoIntersect.Equals("not an intersection"));
            Assert.False(CircleCircleIntr<double>.NoIntersect.Equals(null));
        }
    }
}
