using System;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Upstream guards three preconditions in pline_view.rs with <c>assert!</c>, which is active
    /// in release builds. Mapping them to Debug.Assert would let a release build carry on with a
    /// malformed view instead of failing.
    /// </summary>
    public class PlineViewGuardTests
    {
        [Fact]
        public void CreateRejectsZeroTraverseCount()
        {
            var source = PlineBuilder.Closed((0.0, 0.0, 0.0), (2.0, 0.0, 0.0), (2.0, 2.0, 0.0));

            var ex = Assert.Throws<InvalidOperationException>(() =>
                PlineViewData<double>.Create(
                    source,
                    startIndex: 0,
                    endIntersect: new Vector2<double>(2.0, 0.0),
                    intersectIndex: 0,
                    updatedStart: source.Get(0),
                    traverseCount: 0,
                    posEqualEps: 1e-5));

            Assert.Contains("traverse", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FromEntirePlineRejectsFewerThanTwoVertexes()
        {
            var single = new Polyline<double>(true);
            single.Add(1.0, 1.0, 0.0);

            var ex = Assert.Throws<InvalidOperationException>(
                () => PlineViewData<double>.FromEntirePline(single));

            Assert.Contains("2 vertexes", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FromNewStartRejectsFewerThanTwoVertexes()
        {
            var single = new Polyline<double>(true);
            single.Add(1.0, 1.0, 0.0);

            var ex = Assert.Throws<InvalidOperationException>(
                () => PlineViewData<double>.FromNewStart(single, new Vector2<double>(1.0, 1.0), 0, 1e-5));

            Assert.Contains("2 vertexes", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// GetVertex mirrors Rust's Option-returning get_vertex, where the index is a usize and
        /// cannot be negative. Out of range must yield null at both ends rather than an
        /// IndexOutOfRangeException from the backing array.
        /// </summary>
        [Fact]
        public void GetVertexReturnsNullForOutOfRangeIndexAtBothEnds()
        {
            var source = PlineBuilder.Closed((0.0, 0.0, 0.0), (2.0, 0.0, 0.0), (2.0, 2.0, 0.0));
            var data = PlineViewData<double>.FromEntirePline(source);

            Assert.NotNull(data.GetVertex(source, 0));
            Assert.Null(data.GetVertex(source, data.VertexCount));
            Assert.Null(data.GetVertex(source, -1));
        }
    }
}
