using System;
using System.Collections.Generic;
using System.Linq;
using CavalierContours.Polyline;
using CavalierContours.Shape;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream cavalier_contours 0.7.0
    /// <c>cavalier_contours/tests/test_shape_parallel_offset.rs</c>.
    /// </summary>
    public class ShapeParallelOffsetTests
    {
        /// <summary>
        /// Port of the upstream <c>run_shape_offset_tests</c> harness: build a shape from the
        /// input polylines, offset it with default options and compare the property set of the
        /// resulting loops (counter clockwise loops first, then clockwise loops) against the
        /// expected set. Areas are not inverted.
        /// </summary>
        private static void RunShapeOffsetTests(
            IEnumerable<Polyline<double>> input,
            double offset,
            PlineProperties[] expectedPropertiesSet,
            string context)
        {
            var s = Shape<double>.FromPlines(input);
            var result = s.ParallelOffset(offset, new ShapeOffsetOptions<double>());
            var plines = result.CcwPlines.Concat(result.CwPlines).Select(p => p.Polyline);
            var resultProperties = PlineProperties.CreatePropertySet(plines, false);

            PlineProperties.AssertSetsMatch(resultProperties, expectedPropertiesSet, context);
        }

        // ----------------------------------------------------------------------------------
        // mod test_simple
        // ----------------------------------------------------------------------------------

        [Fact]
        public void EmptyReturnsEmpty()
        {
            RunShapeOffsetTests(
                new List<Polyline<double>>(),
                5.0,
                Array.Empty<PlineProperties>(),
                "empty_returns_empty");
        }

        [Fact]
        public void SetOfEmptyReturnsEmpty()
        {
            RunShapeOffsetTests(
                new[] { new Polyline<double>(true), new Polyline<double>(true) },
                5.0,
                Array.Empty<PlineProperties>(),
                "set_of_empty_returns_empty");
        }

        [Fact]
        public void RectangleInsideShape()
        {
            RunShapeOffsetTests(
                new[]
                {
                    PlineBuilder.ClosedWithUserData(
                        new ulong[] { 4 },
                        (100.0, 100.0, -0.5),
                        (80.0, 90.0, 0.374794619217547),
                        (210.0, 0.0, 0.0),
                        (230.0, 0.0, 1.0),
                        (320.0, 0.0, -0.5),
                        (280.0, 0.0, 0.5),
                        (390.0, 210.0, 0.0),
                        (280.0, 120.0, 0.5)),
                    PlineBuilder.ClosedWithUserData(
                        new ulong[] { 117 },
                        (150.0, 50.0, 0.0),
                        (150.0, 100.0, 0.0),
                        (200.0, 100.0, 0.0),
                        (200.0, 50.0, 0.0)),
                },
                3.0,
                new[]
                {
                    new PlineProperties(12, 40977.79061358948, 998.5536075336107, 84.32384698504309, -41.99999999999997, 401.41586988912127, 205.22199935960901, 4),
                    new PlineProperties(8, -3128.274333882308, 218.84955592153878, 147.0, 47.0, 203.0, 103.0, 117),
                },
                "rectangle_inside_shape");
        }

        // ----------------------------------------------------------------------------------
        // mod test_specific
        // ----------------------------------------------------------------------------------

        [Fact]
        public void Case1()
        {
            RunShapeOffsetTests(
                new[]
                {
                    PlineBuilder.ClosedWithUserData(
                        new ulong[] { 4 },
                        (100.0, 100.0, -0.5),
                        (80.0, 90.0, 0.374794619217547),
                        (210.0, 0.0, 0.0),
                        (230.0, 0.0, 1.0),
                        (320.0, 0.0, -0.5),
                        (280.0, 0.0, 0.5),
                        (390.0, 210.0, 0.0),
                        (280.0, 120.0, 0.5)),
                    PlineBuilder.ClosedWithUserData(
                        new ulong[] { 117 },
                        (150.0, 50.0, 0.0),
                        (146.32758944101474, 104.13867601941358, 0.0),
                        (200.0, 100.0, 0.0),
                        (200.0, 50.0, 0.0)),
                },
                17.0,
                new[]
                {
                    new PlineProperties(22, 20848.93377998434, 1149.2701898185926, 102.79564651409214, -28.000000000000004, 387.41586988912127, 181.8843855860552, 4, 117),
                },
                "case1");
        }

        [Fact]
        public void Case2()
        {
            RunShapeOffsetTests(
                new[]
                {
                    PlineBuilder.ClosedWithUserData(
                        new ulong[] { 4 },
                        (160.655879768138, 148.75471430537402, -0.5),
                        (80.0, 90.0, 0.374794619217547),
                        (210.0, 0.0, 0.0),
                        (230.0, 0.0, 1.0),
                        (320.0, 0.0, -0.5),
                        (280.0, 0.0, 0.5),
                        (390.0, 210.0, 0.0),
                        (280.0, 120.0, 0.5)),
                    PlineBuilder.ClosedWithUserData(
                        new ulong[] { 117 },
                        (150.0, 50.0, 0.0),
                        (192.62381977774953, 130.82800839110848, 0.0),
                        (200.0, 100.0, 0.0),
                        (200.0, 50.0, 0.0)),
                },
                17.0,
                new[]
                {
                    new PlineProperties(20, 20135.256681247833, 1053.2414865948808, 105.64684517241575, -28.000000000000004, 387.41586988912127, 181.8843855860552, 4, 117),
                    new PlineProperties(4, 2.091291658768, 9.557331573939933, 176.64810774674345, 136.97815392110508, 178.9335673169721, 140.906549335123, 4, 117),
                },
                "case2");
        }

        /// <summary>
        /// Test case for issue fixed:
        /// https://github.com/jbuckmccready/cavalier_contours/issues/66
        /// </summary>
        [Fact]
        public void Case3()
        {
            RunShapeOffsetTests(
                new[]
                {
                    PlineBuilder.ClosedWithUserData(
                        Array.Empty<ulong>(),
                        (511.25220437557994, 328.84948025435654, 0.0),
                        (561.2119896118824, 328.84948025435654, 0.0),
                        (561.2119896118824, 363.8703101013724, 0.0),
                        (511.25220437557994, 363.8703101013724, 0.0)),
                    PlineBuilder.ClosedWithUserData(
                        Array.Empty<ulong>(),
                        (540.0335350561843, 343.6169427142472, -0.2382488851276809),
                        (537.4421349268171, 345.12517844750175, -0.009889532389405053),
                        (537.3232102367999, 345.3220639672001, 0.0),
                        (535.3578079577983, 348.7262405716385, 0.0),
                        (535.32462892643, 348.7834560296831, -0.011646639385887355),
                        (535.2271073347746, 348.9562631479, -0.25910007835503845),
                        (535.2805330874, 352.1843242602999, 0.0),
                        (537.0000084336, 355.1625429223, 0.0),
                        (543.6691202685, 343.6113023833, 0.0),
                        (540.2257285374734, 343.6113053474554, 0.0),
                        (540.16153451323, 343.6120105305873, 0.0)),
                    PlineBuilder.ClosedWithUserData(
                        Array.Empty<ulong>(),
                        (535.4816659760771, 346.2417647657877, -0.23822264248219718),
                        (535.4722003945319, 343.2448614622984, -0.009951542143624231),
                        (535.3601905012999, 343.0416179385001, 0.0),
                        (533.3951100416035, 339.6379987413748, 0.0),
                        (533.3623248097243, 339.5809609408148, -0.011710653864622287),
                        (533.2619655102294, 339.4110081813505, -0.11747560444569163),
                        (532.1675755117166, 338.3268271911126, -0.13757287100703242),
                        (530.4835288827071, 337.8423335382348, 0.0),
                        (530.438959126, 337.8420348466, 0.0),
                        (527.0000084336, 337.8420348466, 0.0),
                        (533.6691202683, 349.3932753857, 0.0),
                        (535.3908302312102, 346.4111802353502, 0.0),
                        (535.4223097645596, 346.3552449203639, 0.0)),
                },
                0.8,
                new[]
                {
                    new PlineProperties(4, 1616.2241538207163, 163.56123016663673, 512.0522043755799, 329.64948025435655, 560.4119896118824, 363.0703101013724),
                    new PlineProperties(28, -148.47469897242397, 61.018056828113345, 526.2000084335999, 337.04203484659996, 544.4691202685001, 355.96254292230003),
                },
                "case3");
        }

        // ==================================================================================
        // The tests below are NOT part of the upstream test file. They cover behaviour that
        // is documented in / implied by cavalier_contours 0.7.0
        // `src/shape_algorithms/mod.rs`. All expected values were produced by running the
        // equivalent scenario against the real Rust 0.7.0 crate (examples printing vertex
        // count, area, path length and extents with {:.17} after applying the same
        // `remove_redundant(1e-4)` that the test property helper applies).
        // ==================================================================================

        /// <summary>Counter clockwise axis aligned square, area <c>+(max-min)^2</c>.</summary>
        private static Polyline<double> CcwSquare(double min, double max)
            => PlineBuilder.Closed((min, min, 0.0), (max, min, 0.0), (max, max, 0.0), (min, max, 0.0));

        /// <summary>Clockwise axis aligned square, area <c>-(max-min)^2</c>.</summary>
        private static Polyline<double> CwSquare(double min, double max)
            => PlineBuilder.Closed((min, min, 0.0), (min, max, 0.0), (max, max, 0.0), (max, min, 0.0));

        /// <summary>Clockwise (hole) circle built from two semi circle arcs.</summary>
        private static Polyline<double> CwCircle(double cx, double cy, double r)
            => PlineBuilder.Closed((cx - r, cy, -1.0), (cx + r, cy, -1.0));

        private static List<PlineProperties> PropertiesOf(Shape<double> shape)
            => PlineProperties.CreatePropertySet(
                shape.CcwPlines.Concat(shape.CwPlines).Select(p => p.Polyline),
                false);

        /// <summary>
        /// NOT ported from the upstream test file.
        /// <c>Shape::from_plines</c> skips polylines with <c>vertex_count() &lt;= 1</c>
        /// ("skip empty polylines"). A two vertex closed polyline (a full circle from two
        /// semi circle arcs) is a legitimate loop and must be kept.
        /// Expected values from Rust 0.7.0.
        /// </summary>
        [Fact]
        public void FromPlinesSkipsPolylinesWithOneOrFewerVertexes()
        {
            var empty = new Polyline<double>(true);
            var oneVertex = PlineBuilder.Closed((5.0, 5.0, 0.0));
            var circle = PlineBuilder.Closed((0.0, 0.0, 1.0), (10.0, 0.0, 1.0));

            var s = Shape<double>.FromPlines(new[] { empty, oneVertex, CcwSquare(0.0, 100.0), circle });

            Assert.Equal(2, s.CcwPlines.Count);
            Assert.Empty(s.CwPlines);

            PlineProperties.AssertSetsMatch(
                PropertiesOf(s),
                new[]
                {
                    new PlineProperties(4, 10000.0, 400.0, 0.0, 0.0, 100.0, 100.0),
                    new PlineProperties(2, 78.53981633974483145, 31.41592653589793116, 0.0, -5.0, 10.0, 5.0),
                },
                "from_plines skips vertex_count <= 1");
        }

        /// <summary>
        /// NOT ported from the upstream test file.
        /// <c>Shape::from_plines</c> partitions by <c>orientation()</c>: positive area
        /// (counter clockwise) loops become filled areas, negative area (clockwise) loops
        /// become holes. Expected values from Rust 0.7.0.
        /// </summary>
        [Fact]
        public void FromPlinesPartitionsClosedPolylinesByAreaSign()
        {
            var s = Shape<double>.FromPlines(new[]
            {
                CcwSquare(0.0, 100.0),
                CwSquare(40.0, 60.0),
                CcwSquare(200.0, 300.0),
            });

            Assert.Equal(2, s.CcwPlines.Count);
            Assert.Single(s.CwPlines);

            PlineProperties.AssertSetsMatch(
                PropertiesOf(s),
                new[]
                {
                    new PlineProperties(4, 10000.0, 400.0, 0.0, 0.0, 100.0, 100.0),
                    new PlineProperties(4, 10000.0, 400.0, 200.0, 200.0, 300.0, 300.0),
                    new PlineProperties(4, -400.0, 80.0, 40.0, 40.0, 60.0, 60.0),
                },
                "from_plines ccw/cw partitioning");
        }

        /// <summary>
        /// NOT ported from the upstream test file.
        /// <c>orientation()</c> returns <c>Open</c> (not <c>CounterClockwise</c>) for open
        /// polylines, so <c>Shape::from_plines</c> puts them in the clockwise/hole list.
        /// Verified against Rust 0.7.0, which yields ccw=1, cw=1 with the open polyline as
        /// <c>cw_plines[0]</c> (vc 3, area 0, path length 20).
        /// </summary>
        [Fact]
        public void FromPlinesPutsOpenPolylinesIntoCwPlines()
        {
            var open = PlineBuilder.Open((0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0));

            var s = Shape<double>.FromPlines(new[] { open, CcwSquare(0.0, 100.0) });

            Assert.Single(s.CcwPlines);
            Assert.Single(s.CwPlines);
            Assert.False(s.CwPlines[0].Polyline.IsClosed);

            PlineProperties.AssertSetsMatch(
                PropertiesOf(s),
                new[]
                {
                    new PlineProperties(4, 10000.0, 400.0, 0.0, 0.0, 100.0, 100.0),
                    new PlineProperties(3, 0.0, 20.0, 0.0, 0.0, 10.0, 10.0),
                },
                "from_plines puts open polylines in cw_plines");
        }

        /// <summary>
        /// NOT ported from the upstream test file.
        /// <c>Shape::empty</c> returns a shape with 0 polylines and an empty spatial index
        /// (<c>bounds()</c> is <c>None</c>), and offsetting it returns an empty shape again
        /// (the early return in <c>parallel_offset</c>). Verified against Rust 0.7.0.
        /// </summary>
        [Fact]
        public void EmptyShapeHasNoPolylinesAndOffsetsToEmpty()
        {
            var s = Shape<double>.Empty();

            Assert.Empty(s.CcwPlines);
            Assert.Empty(s.CwPlines);
            Assert.Null(s.PlinesIndex.Bounds);

            var result = s.ParallelOffset(5.0, new ShapeOffsetOptions<double>());

            Assert.Empty(result.CcwPlines);
            Assert.Empty(result.CwPlines);
            Assert.Null(result.PlinesIndex.Bounds);
        }

        /// <summary>
        /// NOT ported from the upstream test file.
        /// A hole (clockwise loop) shrinks as the shape is offset with a negative distance.
        /// Once the offset distance reaches/exceeds the hole's inradius the hole no longer
        /// produces any offset loop and disappears from the result, leaving only the grown
        /// outer boundary.
        /// </summary>
        /// <remarks>
        /// Ground truth from Rust 0.7.0 for a ccw square [0,100]^2 with a cw circle hole of
        /// radius 10 at (50,50):
        /// <list type="bullet">
        /// <item>offset -9: ccw=1 (vc 8, area 13854.46900494077272015, len 456.54866776461631162,
        /// extents [-9, -9, 109, 109]), cw=1 (vc 2, area -3.14159265358973094,
        /// len 6.28318530717958623, extents [49, 49, 51, 51]).</item>
        /// <item>offset -12: ccw=1 (vc 8, area 15252.38934211692867393,
        /// len 475.39822368615500636, extents [-12, -12, 112, 112]), cw=0.</item>
        /// </list>
        /// Note on the mechanism: the Rust run shows <c>create_offset_loops_with_index</c>
        /// already receives zero raw offset polylines for the collapsed hole
        /// (<c>parallel_offset_for_shape</c> returns an empty vec at -12), so the explicit
        /// "offset &lt; 0 and area &gt; 0 =&gt; skip" inversion guard is defensive here rather
        /// than the acting code path. The observable behaviour asserted below is identical.
        /// </remarks>
        [Fact]
        public void HoleDisappearsWhenNegativeOffsetCollapsesIt()
        {
            RunShapeOffsetTests(
                new[] { CcwSquare(0.0, 100.0), CwCircle(50.0, 50.0, 10.0) },
                -9.0,
                new[]
                {
                    new PlineProperties(8, 13854.46900494077272015, 456.54866776461631162, -9.0, -9.0, 109.0, 109.0),
                    new PlineProperties(2, -3.14159265358973094, 6.28318530717958623, 49.0, 49.0, 51.0, 51.0),
                },
                "hole survives at offset -9");

            RunShapeOffsetTests(
                new[] { CcwSquare(0.0, 100.0), CwCircle(50.0, 50.0, 10.0) },
                -12.0,
                new[]
                {
                    new PlineProperties(8, 15252.38934211692867393, 475.39822368615500636, -12.0, -12.0, 112.0, 112.0),
                },
                "hole removed at offset -12");
        }
    }
}
