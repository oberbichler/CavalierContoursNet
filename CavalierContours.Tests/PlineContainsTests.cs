using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream cavalier_contours 0.7.0
    /// <c>cavalier_contours/tests/test_pline_contains.rs</c>.
    /// </summary>
    public class PlineContainsTests
    {
        private const double PosEqEps = 1e-5;

        /// <summary>
        /// Equivalent of the Rust <c>PlineSource::contains</c> convenience method
        /// (i.e. <c>contains_opt</c> with default options).
        /// </summary>
        private static PlineContainsResult Contains(IPlineSource<double> pline1, IPlineSource<double> pline2)
        {
            return PlineContains.PolylineContains(pline1, pline2, new PlineContainsOptions<double>());
        }

        /// <summary>
        /// Equivalent of the Rust <c>PlineSource::scan_for_self_intersect</c> convenience method:
        /// <c>scan_for_self_intersect_opt</c> with default options visits local and global self
        /// intersects (<c>SelfIntersectsInclude::All</c>) over an approximate AABB index and returns
        /// true as soon as one is found. The C# port exposes no scanning wrapper, so the same
        /// visiting is done through <see cref="PlineIntersects.AllSelfIntersectsAsBasic{T}"/>.
        /// </summary>
        private static bool ScanForSelfIntersect(IPlineSource<double> pline)
        {
            if (pline.VertexCount < 2)
            {
                return false;
            }

            var intrs = PlineIntersects.AllSelfIntersectsAsBasic(
                pline,
                pline.CreateApproxAabbIndex(),
                includeOverlapping: true,
                posEqualEps: PosEqEps);

            return intrs.Count != 0;
        }

        [Fact]
        public void TestRectangleContainsCircle()
        {
            var rectangle = PlineBuilder.Closed(
                (-2.0, -2.0, 0.0),
                (2.0, -2.0, 0.0),
                (2.0, 2.0, 0.0),
                (-2.0, 2.0, 0.0));

            var circle = PlineBuilder.Closed((-1.0, 0.0, 1.0), (1.0, 0.0, 1.0));

            Assert.Equal(PlineContainsResult.Pline2InsidePline1, Contains(rectangle, circle));
            Assert.Equal(PlineContainsResult.Pline1InsidePline2, Contains(circle, rectangle));
        }

        [Fact]
        public void TestRectangleIntersectsCircle()
        {
            var rectangle = PlineBuilder.Closed(
                (-2.0, -2.0, 0.0),
                (0.5, -2.0, 0.0),
                (0.5, 2.0, 0.0),
                (-2.0, 2.0, 0.0));

            var circle = PlineBuilder.Closed((-1.0, 0.0, 1.0), (1.0, 0.0, 1.0));

            Assert.Equal(PlineContainsResult.Intersected, Contains(rectangle, circle));
            Assert.Equal(PlineContainsResult.Intersected, Contains(circle, rectangle));
        }

        [Fact]
        public void TestDisjoint()
        {
            var rectangle = PlineBuilder.Closed(
                (-2.0, -2.0, 0.0),
                (2.0, -2.0, 0.0),
                (2.0, 2.0, 0.0),
                (-2.0, 2.0, 0.0));

            var circle = PlineBuilder.Closed((4.0, 0.0, 1.0), (5.0, 0.0, 1.0));

            Assert.Equal(PlineContainsResult.Disjoint, Contains(rectangle, circle));
            Assert.Equal(PlineContainsResult.Disjoint, Contains(circle, rectangle));
        }

        [Fact]
        public void TestCopy()
        {
            var rectangle = PlineBuilder.Closed(
                (-2.0, -2.0, 0.0),
                (2.0, -2.0, 0.0),
                (2.0, 2.0, 0.0),
                (-2.0, 2.0, 0.0));

            var copy = PlineSourceExtensions.CreateFrom<Polyline<double>, double>(rectangle);

            Assert.Equal(PlineContainsResult.Intersected, Contains(rectangle, copy));
        }

        [Fact]
        public void TestInvalid()
        {
            var bad1 = PlineBuilder.Open((0.0, 0.0, 0.0));
            var bad2 = PlineBuilder.Open((-2.0, -2.0, 0.0));

            Assert.Equal(PlineContainsResult.InvalidInput, Contains(bad1, bad2));
            Assert.Equal(PlineContainsResult.InvalidInput, Contains(bad2, bad1));
        }

        [Fact]
        public void TestSelfIntersectScan()
        {
            var hourglass = PlineBuilder.Closed(
                (0.0, 2.0, 0.0),
                (1.0, 1.0, 0.0),
                (0.0, 1.0, 0.0),
                (1.0, 2.0, 0.0));

            Assert.True(ScanForSelfIntersect(hourglass));
        }
    }
}
