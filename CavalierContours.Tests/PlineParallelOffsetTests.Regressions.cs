using System;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Inputs for which the 0.8.0 offset algorithm returns a wrong result and upstream 0.9.0 does
    /// not. Not part of the upstream suite; the expected values follow cavalier_contours 0.9.0.
    /// </summary>
    public partial class PlineParallelOffsetTests
    {
        /// <summary>
        /// A two degree outer turn between segments of length 0.01 passes the line-line parallel
        /// test, which used to join the offset segments with a half circle across the gap
        /// instead of an arc about the source vertex.
        /// </summary>
        [Fact]
        public void OuterTurnBelowParallelEpsilon()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0),
                    (0.01, 0.0, 0.0),
                    (0.019994, 0.000349, 0.0)),
                -0.5,
                new[]
                {
                    new PlineProperties(4, 0.0, 0.0374534757979246, 0.0, -0.5, 0.03744383972395761, -0.49934641031873417, 4UL),
                },
                false);
        }

        /// <summary>
        /// Every corner of a fine polygon is a small outer turn when offset outward, so the
        /// result must be the polygon's perimeter plus a full circle of round joins.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FinePolygonOutwardOffsetJoinsWithArcs(bool handleSelfIntersects)
        {
            const int n = 628;
            const double offset = -0.5;
            var polygon = new Polyline<double>(true);
            for (int i = 0; i < n; i++)
            {
                double angle = i * 2.0 * Math.PI / n;
                polygon.Add(Math.Cos(angle), Math.Sin(angle), 0.0);
            }

            var options = new PlineOffsetOptions<double> { HandleSelfIntersects = handleSelfIntersects };
            var results = PlineOffset.ParallelOffset<Polyline<double>, double>(polygon, offset, options);

            var result = Assert.Single(results);
            double expectedLength = n * 2.0 * Math.Sin(Math.PI / n) + 2.0 * Math.PI * -offset;
            Assert.Equal(expectedLength, result.PathLength(), 1e-6);
            for (int i = 0; i < result.VertexCount; i++)
            {
                Assert.True(Math.Abs(result.Get(i).Bulge) < 0.01, $"vertex {i} has bulge {result.Get(i).Bulge}");
            }
        }

        /// <summary>
        /// Offset inward past its collapse, the raw offset of a thin triangle runs the other way
        /// round and does not cross itself; it used to be returned whole, crossing the source.
        /// </summary>
        [Fact]
        public void ThinTriangleOffsetInwardPastCollapse()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0),
                    (0.3, 0.5, 0.0),
                    (2.6, 4.4, 0.0)),
                1.0,
                Array.Empty<PlineProperties>(),
                false);
        }
    }
}
