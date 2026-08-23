using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    public class PlinePropertiesSelfTests
    {
        [Fact]
        public void FromPlineComputesCircleProperties()
        {
            var circle = PlineBuilder.Closed((-1.0, 0.0, 1.0), (1.0, 0.0, 1.0));
            var props = PlineProperties.FromPline(circle, invertArea: false);

            Assert.Equal(2, props.VertexCount);
            Assert.Equal(System.Math.PI, props.Area, 10);
            Assert.Equal(2.0 * System.Math.PI, props.PathLength, 10);
            Assert.Equal(-1.0, props.Extents.MinX, 10);
            Assert.Equal(-1.0, props.Extents.MinY, 10);
            Assert.Equal(1.0, props.Extents.MaxX, 10);
            Assert.Equal(1.0, props.Extents.MaxY, 10);
        }

        [Fact]
        public void PropertySetsMatchTreatsDuplicatesAsMultiset()
        {
            var a = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);
            var b = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);
            var c = new PlineProperties(4, 25.0, 20.0, 0, 0, 5, 5);

            // Two identical results must not both be matched by a single expected entry.
            Assert.False(PlineProperties.PropertySetsMatch(new[] { a, b }, new[] { a }));
            Assert.True(PlineProperties.PropertySetsMatch(new[] { a, b }, new[] { a, b }));
            Assert.False(PlineProperties.PropertySetsMatch(new[] { a, b }, new[] { a, c }));
        }

        [Fact]
        public void InvertAreaNegatesArea()
        {
            var square = PlineBuilder.Closed((0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0), (0.0, 10.0, 0.0));
            Assert.Equal(100.0, PlineProperties.FromPline(square, false).Area, 10);
            Assert.Equal(-100.0, PlineProperties.FromPline(square, true).Area, 10);
        }

        [Fact]
        public void FromPlineRemovesRedundantVertexesBeforeCounting()
        {
            // The midpoint of the bottom edge is collinear and must not be counted.
            var withRedundant = PlineBuilder.Closed(
                (0.0, 0.0, 0.0), (5.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0), (0.0, 10.0, 0.0));
            var without = PlineBuilder.Closed(
                (0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0), (0.0, 10.0, 0.0));

            var a = PlineProperties.FromPline(withRedundant, false);
            var b = PlineProperties.FromPline(without, false);

            Assert.Equal(4, a.VertexCount);
            Assert.True(a.FuzzyEqEps(b, PlineProperties.PropCmpEps));
        }

        [Fact]
        public void FuzzyEqEpsRejectsDifferencesInEveryComponent()
        {
            var baseline = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);
            const double eps = PlineProperties.PropCmpEps;

            Assert.False(baseline.FuzzyEqEps(new PlineProperties(5, 100.0, 40.0, 0, 0, 10, 10), eps));
            Assert.False(baseline.FuzzyEqEps(new PlineProperties(4, 100.1, 40.0, 0, 0, 10, 10), eps));
            Assert.False(baseline.FuzzyEqEps(new PlineProperties(4, 100.0, 40.1, 0, 0, 10, 10), eps));
            Assert.False(baseline.FuzzyEqEps(new PlineProperties(4, 100.0, 40.0, 0.1, 0, 10, 10), eps));
            Assert.False(baseline.FuzzyEqEps(new PlineProperties(4, 100.0, 40.0, 0, 0.1, 10, 10), eps));
            Assert.False(baseline.FuzzyEqEps(new PlineProperties(4, 100.0, 40.0, 0, 0, 10.1, 10), eps));
            Assert.False(baseline.FuzzyEqEps(new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10.1), eps));

            // Within epsilon in every component.
            Assert.True(baseline.FuzzyEqEps(
                new PlineProperties(4, 100.00005, 40.00005, 5e-5, 5e-5, 10.00005, 10.00005), eps));
        }

        [Fact]
        public void FuzzyEqEpsAbsAreaIgnoresOrientationButNotMagnitude()
        {
            var ccw = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);
            var cw = new PlineProperties(4, -100.0, 40.0, 0, 0, 10, 10);
            const double eps = PlineProperties.PropCmpEps;

            Assert.False(ccw.FuzzyEqEps(cw, eps));
            Assert.True(ccw.FuzzyEqEpsAbsArea(cw, eps));
            Assert.False(ccw.FuzzyEqEpsAbsArea(new PlineProperties(4, -25.0, 40.0, 0, 0, 10, 10), eps));
        }

        [Fact]
        public void UserDataSetsMatchIsAsymmetricLikeUpstream()
        {
            // Upstream only requires expected to be a subset of actual.
            Assert.True(PlineProperties.UserDataSetsMatch(new ulong[] { 1, 2, 3 }, new ulong[] { 1, 3 }));
            Assert.True(PlineProperties.UserDataSetsMatch(new ulong[] { 1 }, System.Array.Empty<ulong>()));
            Assert.False(PlineProperties.UserDataSetsMatch(new ulong[] { 1, 2 }, new ulong[] { 1, 9 }));
        }

        [Fact]
        public void PropertySetsMatchRequiresEqualCounts()
        {
            var a = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);

            Assert.False(PlineProperties.PropertySetsMatch(new[] { a }, System.Array.Empty<PlineProperties>()));
            Assert.False(PlineProperties.PropertySetsMatch(System.Array.Empty<PlineProperties>(), new[] { a }));
            Assert.True(PlineProperties.PropertySetsMatch(
                System.Array.Empty<PlineProperties>(), System.Array.Empty<PlineProperties>()));
        }
    }
}
