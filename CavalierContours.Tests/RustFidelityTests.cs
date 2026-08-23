using System;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Pins places where this port used to deviate from the Rust reference without reason.
    /// Each test names the upstream location it mirrors.
    /// </summary>
    public class RustFidelityTests
    {
        /// <summary>
        /// Upstream traits.rs: <c>result.add_vertex(v1)</c> for the zero-bulge branch, which keeps
        /// a bulge that is fuzzy zero but not exactly zero. Writing a hard zero loses it.
        /// </summary>
        [Fact]
        public void ArcsToApproxLinesKeepsNonZeroBulgeOnLineSegments()
        {
            var pline = PlineBuilder.Closed((0.0, 0.0, 1e-12), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0));

            var approx = pline.ArcsToApproxLines(0.01);

            Assert.NotNull(approx);
            Assert.Equal(1e-12, approx!.Get(0).Bulge, 15);
        }

        /// <summary>
        /// Upstream traits.rs remove_redundant builds the reduced polyline via
        /// <c>OutputPolyline::with_capacity</c> / <c>from_iter</c> and never calls
        /// set_userdata_values, so the result carries no userdata.
        /// </summary>
        [Fact]
        public void RemoveRedundantDoesNotCopyUserData()
        {
            var pline = PlineBuilder.ClosedWithUserData(
                [42],
                (0.0, 0.0, 0.0), (5.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0), (0.0, 10.0, 0.0));

            var reduced = pline.RemoveRedundant(1e-5);

            Assert.NotNull(reduced);
            Assert.Equal(4, reduced!.VertexCount);
            Assert.Equal(0, reduced.UserDataCount);
        }

        /// <summary>
        /// Same as <see cref="RemoveRedundantDoesNotCopyUserData"/> for the early-return branch
        /// that collapses a two vertex polyline with coincident positions.
        /// </summary>
        [Fact]
        public void RemoveRedundantDoesNotCopyUserDataOnCollapse()
        {
            var pline = PlineBuilder.ClosedWithUserData([42], (0.0, 0.0, 0.0), (0.0, 0.0, 0.0));

            var reduced = pline.RemoveRedundant(1e-5);

            Assert.NotNull(reduced);
            Assert.Equal(0, reduced!.UserDataCount);
        }

        /// <summary>Upstream traits.rs rotate_start likewise does not carry userdata over.</summary>
        [Fact]
        public void RotateStartDoesNotCopyUserData()
        {
            var pline = PlineBuilder.ClosedWithUserData(
                [42],
                (0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0), (0.0, 10.0, 0.0));

            var rotated = pline.RotateStart(1, new Vector2<double>(10.0, 5.0), 1e-5);

            Assert.NotNull(rotated);
            Assert.Equal(0, rotated!.UserDataCount);
        }

        /// <summary>
        /// RemoveRepeatPos already matched upstream and must keep doing so; this pins the
        /// consistency of the three sibling methods.
        /// </summary>
        [Fact]
        public void RemoveRepeatPosDoesNotCopyUserData()
        {
            var pline = PlineBuilder.ClosedWithUserData(
                [42],
                (0.0, 0.0, 0.0), (0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0));

            var cleaned = pline.RemoveRepeatPos(1e-5);

            Assert.NotNull(cleaned);
            Assert.Equal(0, cleaned!.UserDataCount);
        }

        /// <summary>
        /// Upstream pline_view.rs from_new_start uses <c>source.last()?</c>, so an empty open
        /// polyline yields None rather than panicking.
        /// </summary>
        [Fact]
        public void FromNewStartOnEmptyOpenPlineReturnsNull()
        {
            var empty = new Polyline<double>(false);

            var data = PlineViewData<double>.FromNewStart(empty, new Vector2<double>(0.0, 0.0), 0, 1e-5);

            Assert.Null(data);
        }

        /// <summary>
        /// Upstream traits.rs accumulates the shoelace term as <c>acc + a - b</c>, i.e.
        /// <c>(acc + a) - b</c>. The C# form <c>acc += (a - b)</c> rounds differently, and
        /// Orientation() decides on the sign of this sum.
        /// </summary>
        [Fact]
        public void AreaUsesUpstreamAccumulationOrder()
        {
            // Coordinates found by search: the two groupings differ in the last bit here.
            // acc += (t1 - t2)     gives -66277889406.12097
            // acc = acc + t1 - t2  gives -66277889406.12098   <- upstream
            var pline = PlineBuilder.Closed(
                (-421920.105, -859553.001, 0.0),
                (532575.773, -199200.39, 0.0),
                (693167.244, -226972.937, 0.0));

            double upstreamGrouping = 0.0;
            double naiveGrouping = 0.0;
            foreach (var (v1, v2) in pline.IterSegments())
            {
                upstreamGrouping = upstreamGrouping + (v1.X * v2.Y) - (v1.Y * v2.X);
                naiveGrouping += (v1.X * v2.Y) - (v1.Y * v2.X);
            }

            Assert.True(
                BitConverter.DoubleToInt64Bits(upstreamGrouping) != BitConverter.DoubleToInt64Bits(naiveGrouping),
                "precondition: this input must distinguish the two accumulation orders");

            Assert.True(
                BitConverter.DoubleToInt64Bits(upstreamGrouping / 2.0) == BitConverter.DoubleToInt64Bits(pline.Area()),
                $"expected the upstream grouping {upstreamGrouping / 2.0:G17}, got {pline.Area():G17}");
        }
    }
}
