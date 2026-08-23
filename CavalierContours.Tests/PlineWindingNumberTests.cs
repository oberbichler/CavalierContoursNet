using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream cavalier_contours 0.7.0
    /// <c>cavalier_contours/tests/test_pline_winding_number.rs</c>.
    /// </summary>
    public class PlineWindingNumberTests
    {
        /// <summary>
        /// Equivalent of the Rust <c>let mut pl = pl.clone(); pl.invert_direction_mut();</c> pattern.
        /// </summary>
        private static Polyline<double> Inverted(IPlineSource<double> source)
        {
            var clone = PlineSourceExtensions.CreateFrom<Polyline<double>, double>(source);
            clone.InvertDirection();
            return clone;
        }

        [Fact]
        public void PointAndCircle()
        {
            var pl = PlineBuilder.Closed((0.0, 0.0, 1.0), (1.0, 0.0, 1.0));

            // inside the circle
            {
                var pt = new Vector2<double>(0.5, 0.0);
                Assert.Equal(1, pl.WindingNumber(pt));

                var inverted = Inverted(pl);
                Assert.Equal(-1, inverted.WindingNumber(pt));
            }

            // outside the circle
            {
                var pt = new Vector2<double>(2.0, 0.0);
                Assert.Equal(0, pl.WindingNumber(pt));

                var inverted = Inverted(pl);
                Assert.Equal(0, inverted.WindingNumber(pt));
            }
        }

        [Fact]
        public void PointAndRectangle()
        {
            var pl = PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (4.0, 0.0, 0.0),
                (4.0, 4.0, 0.0),
                (0.0, 4.0, 0.0));

            // inside the rectangle
            {
                var pt = new Vector2<double>(1.0, 1.0);
                Assert.Equal(1, pl.WindingNumber(pt));

                var inverted = Inverted(pl);
                Assert.Equal(-1, inverted.WindingNumber(pt));
            }

            // outside the rectangle
            {
                var pt = new Vector2<double>(-1.0, 1.0);
                Assert.Equal(0, pl.WindingNumber(pt));

                var inverted = Inverted(pl);
                Assert.Equal(0, inverted.WindingNumber(pt));
            }
        }

        [Fact]
        public void MultipleWindings()
        {
            // path forming circle overlapping itself
            var pl = PlineBuilder.Closed(
                (0.0, 0.0, 1.0),
                (2.0, 0.0, 1.0),
                (0.0, 0.0, 1.0),
                (2.0, 0.0, 1.0));

            // inside the circle
            {
                var pt = new Vector2<double>(0.5, 0.0);
                Assert.Equal(2, pl.WindingNumber(pt));

                var inverted = Inverted(pl);
                Assert.Equal(-2, inverted.WindingNumber(pt));
            }

            // outside the circle
            {
                var pt = new Vector2<double>(2.0, 0.0);
                Assert.Equal(0, pl.WindingNumber(pt));

                var inverted = Inverted(pl);
                Assert.Equal(0, inverted.WindingNumber(pt));
            }
        }

        [Fact]
        public void PointOutsideAlignedWithDirectionVectors1()
        {
            var pl = PlineBuilder.Closed(
                (-10.0, 0.0, 1.0),
                (10.0, 0.0, 0.0),
                (20.0, 0.0, 0.0),
                (20.0, -10.0, 0.0),
                (-20.0, -10.0, 0.0),
                (-20.0, 0.0, 0.0));

            var pt = Vector2<double>.Zero;

            Assert.Equal(0, pl.WindingNumber(pt));
        }

        [Fact]
        public void PointOutsideAlignedWithDirectionVectors2()
        {
            var pl = PlineBuilder.Closed(
                (-5.51073e-15, -30.0, 0.269712),
                (26.0788, -14.8288, 0.0),
                (76.0788, 73.104, 0.12998),
                (80.0, 87.9329, 0.0),
                (80.0, 130.0, 0.0),
                (50.0, 130.0, 0.0),
                (50.0, 95.0, -0.414214),
                (40.0, 85.0, 0.0),
                (0.0, 85.0, 0.0));

            var pt = new Vector2<double>(-20.0, 85.0);

            Assert.Equal(0, pl.WindingNumber(pt));
        }
    }
}
