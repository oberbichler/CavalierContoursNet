using System;
using System.Collections.Generic;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Pins the pruning selection rules of <c>PlineBoolean.PruneSlices</c> against the four modes
    /// that exist upstream (<c>PruneMode::{Union, Intersection, FirstMinusSecond,
    /// SecondMinusFirst}</c> in <c>pline_boolean.rs</c>).
    ///
    /// The invariant checked is that the direction handling
    /// (<c>set_opposing_direction</c>) is fully determined by the slice selection:
    /// it is <c>false</c> exactly when both polylines contribute the same kind of slices
    /// (both "outside the other" = union, both "inside the other" = intersection) and
    /// <c>true</c> exactly when they contribute opposite kinds (a difference operation).
    ///
    /// A union-style selection combined with <c>set_opposing_direction == true</c> does not exist
    /// upstream and must not be requestable.
    /// </summary>
    public class PruneSlicesModeTests
    {
        private static Polyline<double> Pline1() => PlineBuilder.Closed(
            (0.0, 0.0, 0.0),
            (10.0, 0.0, 0.0),
            (10.0, 10.0, 0.0),
            (0.0, 10.0, 0.0));

        private static Polyline<double> Pline2Ccw() => PlineBuilder.Closed(
            (5.0, 5.0, 0.0),
            (15.0, 5.0, 0.0),
            (15.0, 15.0, 0.0),
            (5.0, 15.0, 0.0));

        private static Polyline<double> Pline2Cw() => PlineBuilder.Closed(
            (5.0, 15.0, 0.0),
            (15.0, 15.0, 0.0),
            (15.0, 5.0, 0.0),
            (5.0, 5.0, 0.0));

        [Fact]
        public void PruneSelectionAndDirectionAlwaysAgree()
        {
            foreach (PruneMode mode in Enum.GetValues<PruneMode>())
            {
                AssertSelectionAndDirectionAgree(mode, pline2Reversed: false);
                AssertSelectionAndDirectionAgree(mode, pline2Reversed: true);
            }
        }

        private static void AssertSelectionAndDirectionAgree(PruneMode mode, bool pline2Reversed)
        {
            const double posEqualEps = 1e-5;

            var pline1 = Pline1();
            var pline2 = pline2Reversed ? Pline2Cw() : Pline2Ccw();

            var booleanInfo = PlineBoolean.ProcessForBoolean(
                pline1,
                pline2,
                pline1.CreateApproxAabbIndex(),
                posEqualEps);

            var pruned = PlineBoolean.PruneSlices(pline1, pline2, booleanInfo, mode, posEqualEps);

            var slices = pruned.SlicesRemaining;
            int pline2Start = pruned.Starts.Pline2;
            int pline1OverlappingStart = pruned.Starts.Pline1Overlapping;

            Assert.True(pline2Start > 0, "expected at least one non-overlapping slice from pline1");
            Assert.True(pline1OverlappingStart > pline2Start, "expected at least one non-overlapping slice from pline2");

            bool pline1SlicesAreOutsidePline2 = AllSlicesOutside(slices, 0, pline2Start, pline1, pline2);
            bool pline2SlicesAreOutsidePline1 = AllSlicesOutside(slices, pline2Start, pline1OverlappingStart, pline2, pline1);

            bool invertedPline1Slices = AllInverted(slices, 0, pline2Start);

            // PruneSlices flips the pline1 slice directions iff the requested
            // set_opposing_direction differs from the actual relative orientation.
            bool setOpposingDirection = booleanInfo.OpposingDirections() ^ invertedPline1Slices;

            bool expectedOpposingDirection = pline1SlicesAreOutsidePline2 != pline2SlicesAreOutsidePline1;

            Assert.True(
                setOpposingDirection == expectedOpposingDirection,
                $"mode {mode} (pline2Reversed={pline2Reversed}): pline1 slices outside pline2 = "
                    + $"{pline1SlicesAreOutsidePline2}, pline2 slices outside pline1 = {pline2SlicesAreOutsidePline1}, "
                    + $"therefore set_opposing_direction must be {expectedOpposingDirection} but was {setOpposingDirection}");
        }

        private static bool AllInverted(List<BooleanPlineSlice<double>> slices, int start, int end)
        {
            bool first = slices[start].ViewData.InvertedDirection;
            for (int i = start + 1; i < end; i++)
            {
                Assert.True(
                    slices[i].ViewData.InvertedDirection == first,
                    "expected all pline1 slices to share the same inverted direction flag");
            }
            return first;
        }

        private static bool AllSlicesOutside(
            List<BooleanPlineSlice<double>> slices,
            int start,
            int end,
            Polyline<double> source,
            Polyline<double> other)
        {
            bool? result = null;
            for (int i = start; i < end; i++)
            {
                var view = slices[i].View(source);
                var midpoint = PlineSeg.SegMidpoint(view.Get(0), view.Get(1));
                bool outside = other.WindingNumber(midpoint) == 0;
                if (result is null)
                {
                    result = outside;
                }
                else
                {
                    Assert.True(
                        result.Value == outside,
                        "expected all slices from one polyline to be classified the same way");
                }
            }

            Assert.NotNull(result);
            return result!.Value;
        }
    }
}
