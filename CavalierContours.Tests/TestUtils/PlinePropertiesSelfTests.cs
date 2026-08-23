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

            Assert.True(PlineProperties.PropertySetsMatch(new[] { a, b }, new[] { a, b }));
            Assert.False(PlineProperties.PropertySetsMatch(new[] { a, b }, new[] { a, c }));
        }

        /// <summary>
        /// Pins the injectivity of the matching. Two expected entries that are both within
        /// epsilon of the *same* result entry must not both consume it — otherwise a completely
        /// unrelated second result would slip through.
        /// </summary>
        [Fact]
        public void PropertySetsMatchIsInjective()
        {
            var a = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);
            // Within PropCmpEps of a, so it also matches a.
            var nearA = new PlineProperties(4, 100.00005, 40.0, 0, 0, 10, 10);
            var bogus = new PlineProperties(4, 7.0, 3.0, 0, 0, 1, 1);

            Assert.True(a.FuzzyEqEps(nearA, PlineProperties.PropCmpEps), "precondition: nearA matches a");

            // Without the consumed-tracking both expected entries would match result[0].
            Assert.False(PlineProperties.PropertySetsMatch(new[] { a, bogus }, new[] { a, nearA }));
            Assert.False(PlineProperties.PropertySetsMatchAbsArea(new[] { a, bogus }, new[] { a, nearA }));
        }

        /// <summary>
        /// PropertySetsMatchAbsArea is the comparator upstream uses for every boolean operation
        /// test. It needs the same guarantees as PropertySetsMatch.
        /// </summary>
        [Fact]
        public void PropertySetsMatchAbsAreaRejectsMismatches()
        {
            var a = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);
            var c = new PlineProperties(4, 25.0, 20.0, 0, 0, 5, 5);

            Assert.True(PlineProperties.PropertySetsMatchAbsArea(new[] { a }, new[] { a }));
            Assert.False(PlineProperties.PropertySetsMatchAbsArea(new[] { a }, new[] { c }));
            Assert.False(PlineProperties.PropertySetsMatchAbsArea(new[] { a }, System.Array.Empty<PlineProperties>()));
            Assert.False(PlineProperties.PropertySetsMatchAbsArea(new[] { a, c }, new[] { a, a }));

            // Only the area sign is ignored, nothing else.
            var negArea = new PlineProperties(4, -100.0, 40.0, 0, 0, 10, 10);
            Assert.True(PlineProperties.PropertySetsMatchAbsArea(new[] { negArea }, new[] { a }));
            var negAreaWrongLength = new PlineProperties(4, -100.0, 41.0, 0, 0, 10, 10);
            Assert.False(PlineProperties.PropertySetsMatchAbsArea(new[] { negAreaWrongLength }, new[] { a }));
        }

        /// <summary>
        /// The comparator must be invoked as expected.FuzzyEqEps(result), matching upstream's
        /// properties_expected.fuzzy_eq_eps(properties_result). The direction is only observable
        /// through the deliberately asymmetric userdata check, so this pins it.
        /// </summary>
        [Fact]
        public void PropertySetsMatchCallsComparatorInUpstreamArgumentOrder()
        {
            var expected = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10, 1, 2);
            var resultWithExtra = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10, 1, 2, 3);

            // upstream: every *result* datum must appear in the *expected* list. 3 does not.
            Assert.False(PlineProperties.PropertySetsMatch(new[] { resultWithExtra }, new[] { expected }));
            Assert.False(PlineProperties.PropertySetsMatchAbsArea(new[] { resultWithExtra }, new[] { expected }));

            // Swapping the roles is accepted, which is what makes the direction observable.
            Assert.True(PlineProperties.PropertySetsMatch(new[] { expected }, new[] { resultWithExtra }));
        }

        /// <summary>
        /// Pins the upstream userdata semantics, including the blind spot: the check is
        /// deliberately asymmetric, so a result that *lost* its userdata passes. Do not
        /// "fix" this without re-verifying every ported expectation.
        /// </summary>
        [Fact]
        public void UserDataComparisonKeepsTheUpstreamBlindSpot()
        {
            var expected = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10, 4, 117);
            var resultWithoutUserData = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10);

            // Known upstream gap: lost userdata is invisible to this comparator.
            Assert.True(PlineProperties.PropertySetsMatch(new[] { resultWithoutUserData }, new[] { expected }));

            // Unexpected extra userdata is caught.
            var resultWithForeign = new PlineProperties(4, 100.0, 40.0, 0, 0, 10, 10, 4, 117, 999);
            Assert.False(PlineProperties.PropertySetsMatch(new[] { resultWithForeign }, new[] { expected }));
        }

        [Fact]
        public void FromPlineCarriesUserDataIntoTheProperties()
        {
            var square = PlineBuilder.Closed((0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0), (0.0, 10.0, 0.0));
            square.SetUserDataValues(new ulong[] { 4, 117 });

            var props = PlineProperties.FromPline(square, invertArea: false);

            Assert.Equal(new ulong[] { 4, 117 }, props.UserData);
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
