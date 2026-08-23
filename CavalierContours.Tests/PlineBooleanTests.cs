using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream cavalier_contours 0.7.0
    /// <c>cavalier_contours/tests/test_pline_boolean.rs</c>.
    /// </summary>
    /// <remarks>
    /// This file holds the harness (upstream lines 1-419); the declared cases live in the
    /// <c>PlineBooleanTests.Same.cs</c>, <c>PlineBooleanTests.Simple.cs</c> and
    /// <c>PlineBooleanTests.Specific.cs</c> partials.
    /// </remarks>
    public partial class PlineBooleanTests
    {
        private static BooleanResult<Polyline<double>, double> Boolean(
            Polyline<double> pline1,
            Polyline<double> pline2,
            BooleanOp op,
            PlineBooleanOptions<double> options)
            => PlineBoolean.PolylineBoolean<Polyline<double>, double>(pline1, pline2, op, options);

        /// <summary>
        /// Port of upstream <c>create_boolean_property_set</c>.
        /// </summary>
        private static List<PlineProperties> CreateBooleanPropertySet(
            IReadOnlyList<BooleanResultPline<Polyline<double>, double>> polylines)
        {
            foreach (var r in polylines)
            {
                Assert.True(
                    r.Pline.RemoveRepeatPos(PlineProperties.PosEqEps) is null,
                    "boolean result should not have repeat positioned vertexes");
            }

            return PlineProperties.CreatePropertySet(polylines.Select(p => p.Pline), false);
        }

        private static string StateContext(BooleanOp op, ModifiedPlineState state1, ModifiedPlineState state2)
            => string.Format(
                CultureInfo.InvariantCulture,
                "boolean op: {0}, modified state1: {1}, modified state2: {2}",
                op,
                state1,
                state2);

        // ----------------------------------------------------------------------------------
        // "same" tests (upstream lines 24-258)
        // ----------------------------------------------------------------------------------

        /// <summary>Upstream <c>translate_mut</c>, which the C# port does not expose.</summary>
        private static Polyline<double> Translated(Polyline<double> pline, double dx, double dy)
        {
            var result = new Polyline<double>(pline.VertexCount, pline.IsClosed);
            for (int i = 0; i < pline.VertexCount; i++)
            {
                var v = pline.Get(i);
                result.AddVertex(new PlineVertex<double>(v.X + dx, v.Y + dy, v.Bulge));
            }
            result.SetUserDataValues(pline.UserDataValues);
            return result;
        }

        /// <summary>Port of upstream <c>run_same_boolean_test</c>.</summary>
        private static void RunSameBooleanTest(
            Polyline<double> self1,
            Polyline<double> self2,
            ModifiedPlineState self1State,
            ModifiedPlineState self2State,
            PlineProperties inputProperties)
        {
            // NOTE: upstream calls the plain `boolean(op)` entry point here, i.e. default options
            // (no collapsed area pruning).
            static PlineBooleanOptions<double> DefaultOptions() => new();

            foreach (var op in new[] { BooleanOp.Or, BooleanOp.And })
            {
                string context = StateContext(op, self1State, self2State);
                var result = Boolean(self1, self2, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Overlapping,
                    $"expected Overlapping but got {result.ResultInfo}; {context}");
                Assert.True(
                    result.PosPlines.Count == 1 && result.NegPlines.Count == 0,
                    $"expected exactly 1 pos pline and 0 neg plines but got {result.PosPlines.Count}/{result.NegPlines.Count}; {context}");

                var resultProperties = PlineProperties.FromPline(
                    result.PosPlines[0].Pline,
                    self2State.InvertedDirection);
                PlineProperties.AssertSetsMatch(
                    new[] { resultProperties },
                    new[] { inputProperties },
                    context);
            }

            foreach (var op in new[] { BooleanOp.Not, BooleanOp.Xor })
            {
                string context = StateContext(op, self1State, self2State);
                var result = Boolean(self1, self2, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Overlapping,
                    $"expected Overlapping but got {result.ResultInfo}; {context}");
                Assert.True(
                    result.PosPlines.Count == 0 && result.NegPlines.Count == 0,
                    $"expected empty result but got {result.PosPlines.Count}/{result.NegPlines.Count}; {context}");
            }

            // test same polyline disjoint by translating
            var extents = self1.Extents() ?? throw new InvalidOperationException("polyline has no extents");
            var disjoint1 = Translated(self1, 1.0 + extents.MaxX - extents.MinX, 0.0);
            var disjoint1Properties = PlineProperties.FromPline(disjoint1, false);

            // disjoint OR
            {
                var op = BooleanOp.Or;
                string context = "disjoint test failed, " + StateContext(op, self1State, self2State);
                var expected = new[] { disjoint1Properties, inputProperties };
                var result = Boolean(disjoint1, self2, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Disjoint,
                    $"expected Disjoint but got {result.ResultInfo}; {context}");
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.PosPlines), expected, context);
                Assert.True(result.NegPlines.Count == 0, $"expected no neg plines; {context}");
            }

            // disjoint AND
            {
                var op = BooleanOp.And;
                string context = "disjoint test failed, " + StateContext(op, self1State, self2State);
                var result = Boolean(disjoint1, self2, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Disjoint,
                    $"expected Disjoint but got {result.ResultInfo}; {context}");
                Assert.True(
                    result.PosPlines.Count == 0 && result.NegPlines.Count == 0,
                    $"expected empty result but got {result.PosPlines.Count}/{result.NegPlines.Count}; {context}");
            }

            // disjoint NOT
            {
                var op = BooleanOp.Not;
                string context = "disjoint test failed, " + StateContext(op, self1State, self2State);
                var expected = new[] { disjoint1Properties };
                var result = Boolean(disjoint1, self2, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Disjoint,
                    $"expected Disjoint but got {result.ResultInfo}; {context}");
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.PosPlines), expected, context);
                Assert.True(result.NegPlines.Count == 0, $"expected no neg plines; {context}");
            }

            // disjoint XOR
            {
                var op = BooleanOp.Xor;
                string context = "disjoint test failed, " + StateContext(op, self1State, self2State);
                var expected = new[] { disjoint1Properties, inputProperties };
                var result = Boolean(disjoint1, self2, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Disjoint,
                    $"expected Disjoint but got {result.ResultInfo}; {context}");
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.PosPlines), expected, context);
                Assert.True(result.NegPlines.Count == 0, $"expected no neg plines; {context}");
            }

            // test same polyline but offset one of them to be fully enclosed by the other
            double offset = self1.Area() < 0.0 ? -0.2 : 0.2;
            var self1InwardOffset = PlineOffset.ParallelOffset<Polyline<double>, double>(
                self1, offset, new PlineOffsetOptions<double>())[0];

            var offsetProperties = new[] { PlineProperties.FromPline(self1InwardOffset, false) };

            // enclosed OR
            {
                var op = BooleanOp.Or;
                string context = "enclosed test failed, " + StateContext(op, self1State, self2State);
                var expected = new[] { inputProperties };
                var result = Boolean(self2, self1InwardOffset, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Pline2InsidePline1,
                    $"expected Pline2InsidePline1 but got {result.ResultInfo}; {context}");
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.PosPlines), expected, context);
                Assert.True(result.NegPlines.Count == 0, $"expected no neg plines; {context}");
            }

            // enclosed AND
            {
                var op = BooleanOp.And;
                string context = "enclosed test failed, " + StateContext(op, self1State, self2State);
                var result = Boolean(self2, self1InwardOffset, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Pline2InsidePline1,
                    $"expected Pline2InsidePline1 but got {result.ResultInfo}; {context}");
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.PosPlines), offsetProperties, context);
                Assert.True(result.NegPlines.Count == 0, $"expected no neg plines; {context}");
            }

            // enclosed self2 NOT self1_offset
            {
                var op = BooleanOp.Not;
                string context = "enclosed test failed, " + StateContext(op, self1State, self2State);
                var posExpected = new[] { inputProperties };
                var result = Boolean(self2, self1InwardOffset, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Pline2InsidePline1,
                    $"expected Pline2InsidePline1 but got {result.ResultInfo}; {context}");
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.PosPlines), posExpected, context);
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.NegPlines), offsetProperties, context);
            }

            // enclosed self1_offset NOT self2
            {
                var op = BooleanOp.Not;
                string context = "enclosed test failed, " + StateContext(op, self1State, self2State);
                var result = Boolean(self1InwardOffset, self2, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Pline1InsidePline2,
                    $"expected Pline1InsidePline2 but got {result.ResultInfo}; {context}");
                Assert.True(
                    result.PosPlines.Count == 0 && result.NegPlines.Count == 0,
                    $"expected empty result but got {result.PosPlines.Count}/{result.NegPlines.Count}; {context}");
            }

            // enclosed XOR
            {
                var op = BooleanOp.Xor;
                string context = "enclosed test failed, " + StateContext(op, self1State, self2State);
                var posExpected = new[] { inputProperties };
                var result = Boolean(self2, self1InwardOffset, op, DefaultOptions());
                Assert.True(
                    result.ResultInfo == BooleanResultInfo.Pline2InsidePline1,
                    $"expected Pline2InsidePline1 but got {result.ResultInfo}; {context}");
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.PosPlines), posExpected, context);
                PlineProperties.AssertSetsMatchAbsArea(CreateBooleanPropertySet(result.NegPlines), offsetProperties, context);
            }
        }

        /// <summary>Port of upstream <c>run_same_boolean_tests</c>.</summary>
        private static void RunSameBooleanTests(Polyline<double> input)
        {
            var plineProperties = PlineProperties.FromPline(input, false);
            var otherModifiedSet = new ModifiedPlineSet(input, true, true);
            var testSet = new ModifiedPlineSet(input, true, true);

            testSet.Accept((modifiedPline, plineState) =>
                otherModifiedSet.Accept((modifiedPline2, plineState2) =>
                    RunSameBooleanTest(
                        modifiedPline,
                        modifiedPline2,
                        plineState,
                        plineState2,
                        plineProperties)));
        }

        // ----------------------------------------------------------------------------------
        // slice verification (upstream lines 316-370)
        // ----------------------------------------------------------------------------------

        /// <summary>Port of upstream <c>verify_slice_set</c>.</summary>
        private static void VerifySliceSet(
            BooleanResultPline<Polyline<double>, double> resultPline,
            Polyline<double> pline1,
            Polyline<double> pline2)
        {
            if (resultPline.Subslices.Count == 0)
            {
                return;
            }

            Polyline<double> SliceToPline(BooleanPlineSlice<double> s)
            {
                var source = s.SourceIsPline1 ? pline1 : pline2;
                return PlineSourceExtensions.CreateFromRemoveRepeat<Polyline<double>, double>(
                    s.View(source), PlineProperties.PosEqEps);
            }

            void StitchSliceOnto(BooleanPlineSlice<double> s, Polyline<double> target)
            {
                var source = s.SourceIsPline1 ? pline1 : pline2;
                target.ExtendRemoveRepeat(s.View(source), PlineProperties.PosEqEps);
            }

            var subslices = resultPline.Subslices;

            var stitched = SliceToPline(subslices[0]);

            for (int i = 1; i < subslices.Count; i++)
            {
                stitched.RemoveAt(stitched.VertexCount - 1);
                StitchSliceOnto(subslices[i], stitched);
            }

            var last = stitched.Last() ?? throw new InvalidOperationException("stitched polyline is empty");
            Assert.True(
                stitched.Get(0).Pos().FuzzyEq(last.Pos()),
                "start does not connect with end when stitching slices together");

            stitched.RemoveAt(stitched.VertexCount - 1);
            stitched.SetIsClosed(true);

            var expectedProperties = PlineProperties.FromPline(resultPline.Pline, false);
            var stitchedProperties = PlineProperties.FromPline(stitched, false);

            Assert.True(
                expectedProperties.FuzzyEqEps(stitchedProperties, PlineProperties.PropCmpEps),
                "slices stitched together do not match result polyline, expected: "
                    + $"{expectedProperties}, actual: {stitchedProperties}");
        }

        /// <summary>Port of upstream <c>verify_all_slices</c>.</summary>
        private static void VerifyAllSlices(
            Polyline<double> pline1,
            Polyline<double> pline2,
            BooleanResult<Polyline<double>, double> booleanResult)
        {
            foreach (var resultPline in booleanResult.PosPlines.Concat(booleanResult.NegPlines))
            {
                VerifySliceSet(resultPline, pline1, pline2);
            }
        }

        // ----------------------------------------------------------------------------------
        // general boolean tests (upstream lines 372-419)
        // ----------------------------------------------------------------------------------

        /// <summary>A single <c>(op, pos_expected, neg_expected)</c> tuple of a declared case.</summary>
        private sealed class BooleanCase
        {
            public BooleanCase(BooleanOp op, PlineProperties[] posExpected, PlineProperties[] negExpected)
            {
                Op = op;
                PosExpected = posExpected;
                NegExpected = negExpected;
            }

            public BooleanOp Op { get; }
            public PlineProperties[] PosExpected { get; }
            public PlineProperties[] NegExpected { get; }
        }

        private static BooleanCase Case(BooleanOp op, PlineProperties[] posExpected, PlineProperties[] negExpected)
            => new(op, posExpected, negExpected);

        private static readonly PlineProperties[] NoPlines = Array.Empty<PlineProperties>();

        private static PlineProperties[] Set(params PlineProperties[] properties) => properties;

        private static PlineProperties Props(
            int vertexCount,
            double area,
            double pathLength,
            double minX,
            double minY,
            double maxX,
            double maxY,
            params ulong[] userData)
            => new(vertexCount, area, pathLength, minX, minY, maxX, maxY, userData);

        /// <summary>Port of upstream <c>run_pline_boolean_tests</c>.</summary>
        private static void RunPlineBooleanTests(
            Polyline<double> pline1,
            Polyline<double> pline2,
            params BooleanCase[] cases)
        {
            var testSet1 = new ModifiedPlineSet(pline1, true, true);
            var testSet2 = new ModifiedPlineSet(pline2, true, true);

            testSet1.Accept((modifiedPline1, state1) =>
                testSet2.Accept((modifiedPline2, state2) =>
                {
                    foreach (var c in cases)
                    {
                        // NOTE: we prune collapsed areas for testing as there can be
                        // inconsistencies due to float thresholding when inverting direction or
                        // cycling vertex index positions.
                        var booleanOptions = new PlineBooleanOptions<double> { CollapsedAreaEps = 1e-5 };

                        var result = Boolean(modifiedPline1, modifiedPline2, c.Op, booleanOptions);
                        var posSetResult = CreateBooleanPropertySet(result.PosPlines);
                        var negSetResult = CreateBooleanPropertySet(result.NegPlines);

                        string context = string.Format(
                            CultureInfo.InvariantCulture,
                            "op: {0}, state1: {1}, state2: {2}",
                            c.Op,
                            state1,
                            state2);

                        PlineProperties.AssertSetsMatchAbsArea(posSetResult, c.PosExpected, "pos plines; " + context);
                        PlineProperties.AssertSetsMatchAbsArea(negSetResult, c.NegExpected, "neg plines; " + context);

                        VerifyAllSlices(modifiedPline1, modifiedPline2, result);
                    }
                }));
        }
    }
}
