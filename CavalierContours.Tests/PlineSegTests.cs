using System;
using CavalierContours.Core;
using CavalierContours.Polyline;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Tests ported from cavalier_contours 0.7.0
    /// <c>cavalier_contours/src/polyline/pline_seg.rs</c>.
    ///
    /// NOTE: that file contains no <c>#[cfg(test)] mod tests</c> block in 0.7.0. The entire
    /// executable specification for it lives in the rustdoc examples ("doc tests") attached to the
    /// public functions. Each doc test is ported below as one [Fact], with every assertion kept.
    /// </summary>
    public class PlineSegTests
    {
        private static PlineVertex<double> V(double x, double y, double bulge) => new(x, y, bulge);

        /// <summary>Rust <c>assert!(a.fuzzy_eq(b))</c> equivalent for scalars.</summary>
        private static void AssertFuzzyEq(double expected, double actual, int precision = 10)
        {
            Assert.Equal(expected, actual, precision);
        }

        /// <summary>Rust <c>assert!(a.fuzzy_eq(b))</c> equivalent for <see cref="Vector2{T}"/>.</summary>
        private static void AssertFuzzyEq(Vector2<double> expected, Vector2<double> actual, int precision = 10)
        {
            Assert.Equal(expected.X, actual.X, precision);
            Assert.Equal(expected.Y, actual.Y, precision);
        }

        /// <summary>Rust <c>assert!(a.fuzzy_eq(b))</c> equivalent for <see cref="PlineVertex{T}"/>.</summary>
        private static void AssertFuzzyEq(PlineVertex<double> expected, PlineVertex<double> actual, int precision = 10)
        {
            Assert.Equal(expected.X, actual.X, precision);
            Assert.Equal(expected.Y, actual.Y, precision);
            Assert.Equal(expected.Bulge, actual.Bulge, precision);
        }

        private static void AssertAabbEq(AABB<double> expected, AABB<double> actual, int precision = 10)
        {
            Assert.Equal(expected.MinX, actual.MinX, precision);
            Assert.Equal(expected.MinY, actual.MinY, precision);
            Assert.Equal(expected.MaxX, actual.MaxX, precision);
            Assert.Equal(expected.MaxY, actual.MaxY, precision);
        }

        // ==========================================================================================
        // Ported doc tests (Rust 0.7.0 pline_seg.rs)
        // ==========================================================================================

        /// <summary>
        /// Ported from the rustdoc example on <c>seg_arc_radius_and_center</c> (pline_seg.rs:17-27).
        /// <code>
        /// let v1 = PlineVertex::new(0.0, 0.0, 1.0);
        /// let v2 = PlineVertex::new(1.0, 0.0, 0.0);
        /// let (arc_radius, arc_center) = seg_arc_radius_and_center(v1, v2);
        /// assert!(arc_radius.fuzzy_eq(0.5));
        /// assert!(arc_center.fuzzy_eq(Vector2::new(0.5, 0.0)));
        /// </code>
        /// </summary>
        [Fact]
        public void SegArcRadiusAndCenterDocExample()
        {
            // arc half circle arc segment going from (0, 0) to (1, 0) counter clockwise
            var v1 = V(0.0, 0.0, 1.0);
            var v2 = V(1.0, 0.0, 0.0);
            (double arcRadius, Vector2<double> arcCenter) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
            AssertFuzzyEq(0.5, arcRadius);
            AssertFuzzyEq(new Vector2<double>(0.5, 0.0), arcCenter);
        }

        /// <summary>
        /// Ported from the rustdoc example on <c>seg_split_at_point</c> (pline_seg.rs:76-87).
        /// <code>
        /// let v1 = PlineVertex::new(0.0, 0.0, 1.0);
        /// let v2 = PlineVertex::new(1.0, 0.0, 0.0);
        /// let point = Vector2::new(0.5, -0.5);
        /// let SplitResult { updated_start, split_vertex } = seg_split_at_point(v1, v2, point, 1e-5);
        /// let quarter_circle_bulge = (std::f64::consts::PI / 8.0).tan();
        /// assert!(updated_start.fuzzy_eq(PlineVertex::new(v1.x, v1.y, quarter_circle_bulge)));
        /// assert!(split_vertex.fuzzy_eq(PlineVertex::new(point.x, point.y, quarter_circle_bulge)));
        /// </code>
        /// </summary>
        [Fact]
        public void SegSplitAtPointDocExample()
        {
            // arc half circle arc segment going from (0, 0) to (1, 0) counter clockwise
            var v1 = V(0.0, 0.0, 1.0);
            var v2 = V(1.0, 0.0, 0.0);
            var point = new Vector2<double>(0.5, -0.5);
            SplitResult<double> result = PlineSeg.SegSplitAtPoint(v1, v2, point, 1e-5);
            double quarterCircleBulge = Math.Tan(Math.PI / 8.0);
            AssertFuzzyEq(V(v1.X, v1.Y, quarterCircleBulge), result.UpdatedStart);
            AssertFuzzyEq(V(point.X, point.Y, quarterCircleBulge), result.SplitVertex);
        }

        /// <summary>
        /// Ported from the rustdoc example on <c>seg_tangent_vector</c> (pline_seg.rs:158-168).
        /// <code>
        /// let v1 = PlineVertex::new(2.0, 2.0, 1.0);
        /// let v2 = PlineVertex::new(4.0, 2.0, 0.0);
        /// let midpoint = Vector2::new(3.0, 1.0);
        /// assert!(seg_tangent_vector(v1, v2, midpoint).normalize().fuzzy_eq(Vector2::new(1.0, 0.0)));
        /// assert!(seg_tangent_vector(v1, v2, v1.pos()).normalize().fuzzy_eq(Vector2::new(0.0, -1.0)));
        /// assert!(seg_tangent_vector(v1, v2, v2.pos()).normalize().fuzzy_eq(Vector2::new(0.0, 1.0)));
        /// </code>
        /// </summary>
        [Fact]
        public void SegTangentVectorDocExample()
        {
            // counter clockwise half circle arc going from (2, 2) to (4, 2)
            var v1 = V(2.0, 2.0, 1.0);
            var v2 = V(4.0, 2.0, 0.0);
            var midpoint = new Vector2<double>(3.0, 1.0);
            AssertFuzzyEq(new Vector2<double>(1.0, 0.0), PlineSeg.SegTangentVector(v1, v2, midpoint).Normalize());
            AssertFuzzyEq(new Vector2<double>(0.0, -1.0), PlineSeg.SegTangentVector(v1, v2, v1.Pos()).Normalize());
            AssertFuzzyEq(new Vector2<double>(0.0, 1.0), PlineSeg.SegTangentVector(v1, v2, v2.Pos()).Normalize());
        }

        /// <summary>
        /// Ported from the rustdoc example on <c>seg_closest_point</c> (pline_seg.rs:204-218).
        /// <code>
        /// let v1 = PlineVertex::new(2.0, 2.0, 1.0);
        /// let v2 = PlineVertex::new(4.0, 2.0, 0.0);
        /// assert!(seg_closest_point(v1, v2, Vector2::new(3.0, 0.0), 1e-5).fuzzy_eq(Vector2::new(3.0, 1.0)));
        /// assert!(seg_closest_point(v1, v2, Vector2::new(3.0, 1.2), 1e-5).fuzzy_eq(Vector2::new(3.0, 1.0)));
        /// assert!(seg_closest_point(v1, v2, v1.pos(), 1e-5).fuzzy_eq(v1.pos()));
        /// assert!(seg_closest_point(v1, v2, v2.pos(), 1e-5).fuzzy_eq(v2.pos()));
        /// </code>
        /// </summary>
        [Fact]
        public void SegClosestPointDocExample()
        {
            // counter clockwise half circle arc going from (2, 2) to (4, 2)
            var v1 = V(2.0, 2.0, 1.0);
            var v2 = V(4.0, 2.0, 0.0);
            AssertFuzzyEq(
                new Vector2<double>(3.0, 1.0),
                PlineSeg.SegClosestPoint(v1, v2, new Vector2<double>(3.0, 0.0), 1e-5));
            AssertFuzzyEq(
                new Vector2<double>(3.0, 1.0),
                PlineSeg.SegClosestPoint(v1, v2, new Vector2<double>(3.0, 1.2), 1e-5));
            AssertFuzzyEq(v1.Pos(), PlineSeg.SegClosestPoint(v1, v2, v1.Pos(), 1e-5));
            AssertFuzzyEq(v2.Pos(), PlineSeg.SegClosestPoint(v1, v2, v2.Pos(), 1e-5));
        }

        /// <summary>
        /// Ported from the first rustdoc example on <c>seg_length</c> (pline_seg.rs:368-376).
        /// <code>
        /// let v1 = PlineVertex::new(2.0, 2.0, 1.0);
        /// let v2 = PlineVertex::new(4.0, 2.0, 0.0);
        /// assert!(seg_length(v1, v2).fuzzy_eq(std::f64::consts::PI));
        /// </code>
        /// </summary>
        [Fact]
        public void SegLengthDocExampleArcSegment()
        {
            // counter clockwise half circle arc going from (2, 2) to (4, 2)
            // arc radius = 1 so length should be PI
            var v1 = V(2.0, 2.0, 1.0);
            var v2 = V(4.0, 2.0, 0.0);
            AssertFuzzyEq(Math.PI, PlineSeg.SegLength(v1, v2));
        }

        /// <summary>
        /// Ported from the second rustdoc example on <c>seg_length</c> (pline_seg.rs:380-388).
        /// <code>
        /// let v1 = PlineVertex::new(2.0, 2.0, 0.0);
        /// let v2 = PlineVertex::new(4.0, 4.0, 0.0);
        /// assert!(seg_length(v1, v2).fuzzy_eq(2.0 * 2.0f64.sqrt()));
        /// </code>
        /// </summary>
        [Fact]
        public void SegLengthDocExampleLineSegment()
        {
            // line segment going from (2, 2) to (4, 4)
            var v1 = V(2.0, 2.0, 0.0);
            var v2 = V(4.0, 4.0, 0.0);
            AssertFuzzyEq(2.0 * Math.Sqrt(2.0), PlineSeg.SegLength(v1, v2));
        }

        /// <summary>
        /// Ported from the first rustdoc example on <c>seg_midpoint</c> (pline_seg.rs:411-418).
        /// <code>
        /// let v1 = PlineVertex::new(2.0, 2.0, 1.0);
        /// let v2 = PlineVertex::new(4.0, 2.0, 0.0);
        /// assert!(seg_midpoint(v1, v2).fuzzy_eq(Vector2::new(3.0, 1.0)));
        /// </code>
        /// </summary>
        [Fact]
        public void SegMidpointDocExampleArcSegment()
        {
            // counter clockwise half circle arc going from (2, 2) to (4, 2)
            var v1 = V(2.0, 2.0, 1.0);
            var v2 = V(4.0, 2.0, 0.0);
            AssertFuzzyEq(new Vector2<double>(3.0, 1.0), PlineSeg.SegMidpoint(v1, v2));
        }

        /// <summary>
        /// Ported from the second rustdoc example on <c>seg_midpoint</c> (pline_seg.rs:422-429).
        /// <code>
        /// let v1 = PlineVertex::new(2.0, 2.0, 0.0);
        /// let v2 = PlineVertex::new(4.0, 4.0, 0.0);
        /// assert!(seg_midpoint(v1, v2).fuzzy_eq(Vector2::new(3.0, 3.0)));
        /// </code>
        /// </summary>
        [Fact]
        public void SegMidpointDocExampleLineSegment()
        {
            // line segment going from (2, 2) to (4, 4)
            var v1 = V(2.0, 2.0, 0.0);
            var v2 = V(4.0, 4.0, 0.0);
            AssertFuzzyEq(new Vector2<double>(3.0, 3.0), PlineSeg.SegMidpoint(v1, v2));
        }

        // ==========================================================================================
        // Bounding box coverage.
        //
        // IMPORTANT: Rust 0.7.0 has NO tests and NO doc examples for seg_bounding_box,
        // seg_fast_approx_bounding_box or arc_seg_bounding_box. The expectations below are NOT
        // ported from Rust - they are derived analytically from the geometry described by the Rust
        // implementation (pline_seg.rs:261-362), by hand, without executing the C# code. They are
        // kept separate so the ported-vs-derived distinction stays visible.
        // ==========================================================================================

        /// <summary>
        /// Line segment (2,2)->(4,4): both bounding box functions must return the exact endpoint
        /// extents.
        /// </summary>
        [Fact]
        public void SegBoundingBoxLineSegmentDerived()
        {
            var v1 = V(2.0, 2.0, 0.0);
            var v2 = V(4.0, 4.0, 0.0);
            var expected = new AABB<double>(2.0, 2.0, 4.0, 4.0);
            AssertAabbEq(expected, PlineSeg.SegBoundingBox(v1, v2));
            AssertAabbEq(expected, PlineSeg.SegFastApproxBoundingBox(v1, v2));
        }

        /// <summary>
        /// CCW half circle from (2,2) to (4,2) with bulge 1: centre (3,2), radius 1, sweeping the
        /// lower half. True extents are therefore x in [2,4], y in [1,2]. For a half circle the
        /// chord+sagitta rectangle used by the fast approximation coincides with the true box.
        /// </summary>
        [Fact]
        public void SegBoundingBoxHalfCircleDerived()
        {
            var v1 = V(2.0, 2.0, 1.0);
            var v2 = V(4.0, 2.0, 0.0);
            var expected = new AABB<double>(2.0, 1.0, 4.0, 2.0);
            AssertAabbEq(expected, PlineSeg.SegBoundingBox(v1, v2));
            AssertAabbEq(expected, PlineSeg.SegFastApproxBoundingBox(v1, v2));
        }

        /// <summary>
        /// CCW quarter circle on the unit circle from (1,0) to (0,1) (bulge = tan(PI/8)). The arc
        /// stays inside the first quadrant, so the true bounding box is exactly the unit square.
        /// </summary>
        [Fact]
        public void SegBoundingBoxQuarterCircleDerived()
        {
            double quarterCircleBulge = Math.Tan(Math.PI / 8.0);
            var v1 = V(1.0, 0.0, quarterCircleBulge);
            var v2 = V(0.0, 1.0, 0.0);
            AssertAabbEq(new AABB<double>(0.0, 0.0, 1.0, 1.0), PlineSeg.SegBoundingBox(v1, v2));
        }

        /// <summary>
        /// Same quarter circle as <see cref="SegBoundingBoxQuarterCircleDerived"/>, but through the
        /// fast approximation. Following pline_seg.rs:280-295 by hand:
        /// offs_x = b*(v2.y-v1.y)/2 = b/2, offs_y = -b*(v2.x-v1.x)/2 = b/2, giving shifted points
        /// (1+b/2, b/2) and (b/2, 1+b/2). Combined with the endpoints this yields
        /// [0, 0, 1+b/2, 1+b/2] - strictly larger than the true box, as documented.
        /// </summary>
        [Fact]
        public void SegFastApproxBoundingBoxQuarterCircleIsLargerThanTrueDerived()
        {
            double b = Math.Tan(Math.PI / 8.0);
            var v1 = V(1.0, 0.0, b);
            var v2 = V(0.0, 1.0, 0.0);
            AssertAabbEq(
                new AABB<double>(0.0, 0.0, 1.0 + (b / 2.0), 1.0 + (b / 2.0)),
                PlineSeg.SegFastApproxBoundingBox(v1, v2));
        }
    }
}
