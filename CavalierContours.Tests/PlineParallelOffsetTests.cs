using System.Collections.Generic;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream cavalier_contours 0.7.0
    /// <c>cavalier_contours/tests/test_pline_parallel_offset.rs</c>.
    /// </summary>
    /// <remarks>
    /// This file holds the harness (upstream lines 1-107). The individual cases live in the
    /// partial class files <c>PlineParallelOffsetTests.Simple.cs</c>,
    /// <c>PlineParallelOffsetTests.Specific.cs</c> and
    /// <c>PlineParallelOffsetTests.PastFailures.cs</c>.
    /// </remarks>
    public partial class PlineParallelOffsetTests
    {
        /// <summary>
        /// Port of the upstream <c>offset_into_properties_set</c> free function.
        /// </summary>
        private static List<PlineProperties> OffsetIntoPropertiesSet(
            Polyline<double> polyline,
            double offset,
            bool inverted,
            bool handleSelfIntersects)
        {
            double appliedOffset = inverted ? -offset : offset;

            // Upstream sets only handle_self_intersects and leaves every other field at its
            // Default::default() value.
            var options = new PlineOffsetOptions<double>
            {
                HandleSelfIntersects = handleSelfIntersects,
            };

            var offsetResults = PlineOffset.ParallelOffset<Polyline<double>, double>(
                polyline, appliedOffset, options);

            foreach (var r in offsetResults)
            {
                Assert.True(
                    r.RemoveRepeatPos(PlineProperties.PosEqEps) is null,
                    "offset result should not have repeat positioned vertexes");
            }

            return PlineProperties.CreatePropertySet(offsetResults, inverted);
        }

        /// <summary>
        /// Port of the upstream <c>PlineOffsetTestVisitor</c> + <c>run_pline_offset_tests</c>.
        /// </summary>
        private static void RunPlineOffsetTests(
            Polyline<double> input,
            double offset,
            IReadOnlyList<PlineProperties> expectedPropertiesSet,
            bool handleSelfIntersects)
        {
            var testSet = new ModifiedPlineSet(input, invertDirection: true, cycleIndexPositions: true);

            testSet.Accept((modifiedPline, plineState) =>
            {
                var offsetResults = OffsetIntoPropertiesSet(
                    modifiedPline,
                    offset,
                    plineState.InvertedDirection,
                    handleSelfIntersects);

                PlineProperties.AssertSetsMatch(
                    offsetResults,
                    expectedPropertiesSet,
                    $"modified state: {plineState}");

                // For closed polylines, also test with handle_self_intersects=true since it uses a
                // different code path (open polylines always use the same path regardless of this
                // flag). Note upstream does this unconditionally, not only when the case itself
                // was declared with handle_self_intersects=false.
                if (modifiedPline.IsClosed)
                {
                    var selfIntersectResults = OffsetIntoPropertiesSet(
                        modifiedPline,
                        offset,
                        plineState.InvertedDirection,
                        true);

                    PlineProperties.AssertSetsMatch(
                        selfIntersectResults,
                        expectedPropertiesSet,
                        $"handle_self_intersects set to true, modified state: {plineState}");
                }
            });
        }
    }
}
