using System;
using System.Collections.Generic;
using System.Linq;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Shape;
using CavalierContours.Spatial;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// The public surface must reject <see langword="null"/> reference arguments with
    /// <see cref="ArgumentNullException"/> carrying the offending parameter name, rather than
    /// failing later with a <see cref="NullReferenceException"/> that a caller cannot tell apart
    /// from an internal defect. Rust has no null, so there is no upstream behaviour mirrored here;
    /// this is purely the C# contract.
    /// </summary>
    public class ArgumentValidationTests
    {
        private static Polyline<double> Square() =>
            PlineBuilder.Closed((0.0, 0.0, 0.0), (2.0, 0.0, 0.0), (2.0, 2.0, 0.0), (0.0, 2.0, 0.0));

        private static Polyline<double> OffsetSquare() =>
            PlineBuilder.Closed((1.0, 1.0, 0.0), (3.0, 1.0, 0.0), (3.0, 3.0, 0.0), (1.0, 3.0, 0.0));

        /// <summary>
        /// Asserts that <paramref name="action"/> throws an <see cref="ArgumentNullException"/>
        /// naming <paramref name="expectedParamName"/>.
        /// </summary>
        private static void AssertThrowsFor(string expectedParamName, Action action)
        {
            var ex = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(expectedParamName, ex.ParamName);
        }

        // ----------------------------------------------------------------------------------
        // PlineOffset
        // ----------------------------------------------------------------------------------

        [Fact]
        public void ParallelOffsetRejectsNullPolyline()
        {
            AssertThrowsFor("polyline", () =>
                PlineOffset.ParallelOffset<Polyline<double>, double>(
                    null!, 0.2, new PlineOffsetOptions<double>()));
        }

        [Fact]
        public void ParallelOffsetRejectsNullOptions()
        {
            AssertThrowsFor("options", () =>
                PlineOffset.ParallelOffset<Polyline<double>, double>(Square(), 0.2, null!));
        }

        [Fact]
        public void CreateRawOffsetPolylineRejectsNullPolyline()
        {
            AssertThrowsFor("polyline", () =>
                PlineOffset.CreateRawOffsetPolyline<Polyline<double>, double>(null!, 0.2, 1e-5));
        }

        [Fact]
        public void SlicesFromRawOffsetRejectsNullArguments()
        {
            var pline = Square();
            var index = pline.CreateApproxAabbIndex();
            var opts = new PlineOffsetOptions<double>();

            AssertThrowsFor("originalPolyline", () =>
                PlineOffset.SlicesFromRawOffset<double>(null!, pline, index, 0.2, opts));
            AssertThrowsFor("rawOffsetPolyline", () =>
                PlineOffset.SlicesFromRawOffset<double>(pline, null!, index, 0.2, opts));
            AssertThrowsFor("origPolylineIndex", () =>
                PlineOffset.SlicesFromRawOffset<double>(pline, pline, null!, 0.2, opts));
            AssertThrowsFor("options", () =>
                PlineOffset.SlicesFromRawOffset<double>(pline, pline, index, 0.2, null!));
        }

        [Fact]
        public void SlicesFromDualRawOffsetsRejectsNullArguments()
        {
            var pline = Square();
            var index = pline.CreateApproxAabbIndex();
            var opts = new PlineOffsetOptions<double>();

            AssertThrowsFor("originalPolyline", () =>
                PlineOffset.SlicesFromDualRawOffsets<double>(null!, pline, pline, index, 0.2, opts));
            AssertThrowsFor("rawOffsetPolyline", () =>
                PlineOffset.SlicesFromDualRawOffsets<double>(pline, null!, pline, index, 0.2, opts));
            AssertThrowsFor("dualRawOffsetPolyline", () =>
                PlineOffset.SlicesFromDualRawOffsets<double>(pline, pline, null!, index, 0.2, opts));
            AssertThrowsFor("origPolylineIndex", () =>
                PlineOffset.SlicesFromDualRawOffsets<double>(pline, pline, pline, null!, 0.2, opts));
            AssertThrowsFor("options", () =>
                PlineOffset.SlicesFromDualRawOffsets<double>(pline, pline, pline, index, 0.2, null!));
        }

        [Fact]
        public void StitchSlicesTogetherRejectsNullArguments()
        {
            var pline = Square();
            var slices = new List<PlineViewData<double>>();
            var opts = new PlineOffsetOptions<double>();

            AssertThrowsFor("rawOffsetPline", () =>
                PlineOffset.StitchSlicesTogether<Polyline<double>, double>(null!, slices, true, 3, opts));
            AssertThrowsFor("slices", () =>
                PlineOffset.StitchSlicesTogether<Polyline<double>, double>(pline, null!, true, 3, opts));
            AssertThrowsFor("options", () =>
                PlineOffset.StitchSlicesTogether<Polyline<double>, double>(pline, slices, true, 3, null!));
        }

        [Fact]
        public void PointValidForOffsetRejectsNullArguments()
        {
            var pline = Square();
            var index = pline.CreateApproxAabbIndex();
            var stack = new List<int>();
            var point = new Vector2<double>(1.0, 1.0);

            AssertThrowsFor("polyline", () =>
                PlineOffset.PointValidForOffset<double>(null!, 0.2, index, point, stack, 1e-5, 1e-4));
            AssertThrowsFor("aabbIndex", () =>
                PlineOffset.PointValidForOffset<double>(pline, 0.2, null!, point, stack, 1e-5, 1e-4));
            AssertThrowsFor("queryStack", () =>
                PlineOffset.PointValidForOffset<double>(pline, 0.2, index, point, null!, 1e-5, 1e-4));
        }

        // ----------------------------------------------------------------------------------
        // PlineBoolean
        // ----------------------------------------------------------------------------------

        [Fact]
        public void PolylineBooleanRejectsNullPline1()
        {
            AssertThrowsFor("pline1", () =>
                PlineBoolean.PolylineBoolean<Polyline<double>, double>(
                    null!, OffsetSquare(), BooleanOp.Or, new PlineBooleanOptions<double>()));
        }

        [Fact]
        public void PolylineBooleanRejectsNullPline2()
        {
            AssertThrowsFor("pline2", () =>
                PlineBoolean.PolylineBoolean<Polyline<double>, double>(
                    Square(), null!, BooleanOp.Or, new PlineBooleanOptions<double>()));
        }

        [Fact]
        public void PolylineBooleanRejectsNullOptions()
        {
            AssertThrowsFor("options", () =>
                PlineBoolean.PolylineBoolean<Polyline<double>, double>(
                    Square(), OffsetSquare(), BooleanOp.Or, null!));
        }

        [Fact]
        public void PlineBooleanFindIntersectsRejectsNullArguments()
        {
            var opts = new FindIntersectsOptions<double>();

            AssertThrowsFor("pline1", () =>
                PlineBoolean.FindIntersects<double>(null!, OffsetSquare(), opts));
            AssertThrowsFor("pline2", () =>
                PlineBoolean.FindIntersects<double>(Square(), null!, opts));
            AssertThrowsFor("options", () =>
                PlineBoolean.FindIntersects<double>(Square(), OffsetSquare(), null!));
        }

        [Fact]
        public void PlineBooleanSortAndJoinOverlappingIntersectsRejectsNullArguments()
        {
            var intersects = new List<PlineOverlappingIntersect<double>>();

            AssertThrowsFor("intersects", () =>
                PlineBoolean.SortAndJoinOverlappingIntersects<double>(
                    null!, Square(), OffsetSquare(), 1e-5));
            AssertThrowsFor("pline1", () =>
                PlineBoolean.SortAndJoinOverlappingIntersects<double>(
                    intersects, null!, OffsetSquare(), 1e-5));
            AssertThrowsFor("pline2", () =>
                PlineBoolean.SortAndJoinOverlappingIntersects<double>(
                    intersects, Square(), null!, 1e-5));
        }

        // ----------------------------------------------------------------------------------
        // PlineContains
        // ----------------------------------------------------------------------------------

        [Fact]
        public void PolylineContainsRejectsNullPline1()
        {
            AssertThrowsFor("pline1", () =>
                PlineContains.PolylineContains<double>(
                    null!, OffsetSquare(), new PlineContainsOptions<double>()));
        }

        [Fact]
        public void PolylineContainsRejectsNullPline2()
        {
            AssertThrowsFor("pline2", () =>
                PlineContains.PolylineContains<double>(
                    Square(), null!, new PlineContainsOptions<double>()));
        }

        [Fact]
        public void PolylineContainsRejectsNullOptions()
        {
            AssertThrowsFor("options", () =>
                PlineContains.PolylineContains<double>(Square(), OffsetSquare(), null!));
        }

        // ----------------------------------------------------------------------------------
        // PlineIntersects
        // ----------------------------------------------------------------------------------

        [Fact]
        public void ScanForIntersectRejectsNullArguments()
        {
            var opts = new FindIntersectsOptions<double>();

            AssertThrowsFor("pline1", () =>
                PlineIntersects.ScanForIntersect<double>(null!, OffsetSquare(), opts));
            AssertThrowsFor("pline2", () =>
                PlineIntersects.ScanForIntersect<double>(Square(), null!, opts));
            AssertThrowsFor("options", () =>
                PlineIntersects.ScanForIntersect<double>(Square(), OffsetSquare(), null!));
        }

        [Fact]
        public void AllSelfIntersectsAsBasicRejectsNullArguments()
        {
            var pline = Square();

            AssertThrowsFor("polyline", () =>
                PlineIntersects.AllSelfIntersectsAsBasic<double>(
                    null!, pline.CreateApproxAabbIndex(), true, 1e-5));
            AssertThrowsFor("aabbIndex", () =>
                PlineIntersects.AllSelfIntersectsAsBasic<double>(pline, null!, true, 1e-5));
        }

        // ----------------------------------------------------------------------------------
        // PlineView
        // ----------------------------------------------------------------------------------

        [Fact]
        public void FromEntirePlineRejectsNullSource()
        {
            AssertThrowsFor("source", () => PlineViewData<double>.FromEntirePline(null!));
        }

        [Fact]
        public void ValidateForSourceRejectsNullSource()
        {
            var data = PlineViewData<double>.FromEntirePline(Square());
            AssertThrowsFor("source", () => data.ValidateForSource(null!));
        }

        [Fact]
        public void GetVertexRejectsNullSource()
        {
            var data = PlineViewData<double>.FromEntirePline(Square());
            AssertThrowsFor("source", () => data.GetVertex(null!, 0));
        }

        [Fact]
        public void FromSlicePointsRejectsNullSource()
        {
            AssertThrowsFor("source", () => PlineViewData<double>.FromSlicePoints(
                null!,
                new Vector2<double>(0.0, 0.0),
                0,
                new Vector2<double>(2.0, 0.0),
                0,
                1e-5));
        }

        // ----------------------------------------------------------------------------------
        // Shape
        // ----------------------------------------------------------------------------------

        [Fact]
        public void ShapeFromPlinesRejectsNullPlines()
        {
            AssertThrowsFor("plines", () => Shape<double>.FromPlines(null!));
        }

        [Fact]
        public void ShapeParallelOffsetRejectsNullOptions()
        {
            var shape = Shape<double>.FromPlines(new[] { Square() });
            AssertThrowsFor("options", () => shape.ParallelOffset(0.2, null!));
        }

        [Fact]
        public void ShapeCreateOffsetLoopsWithIndexRejectsNullOptions()
        {
            var shape = Shape<double>.FromPlines(new[] { Square() });
            AssertThrowsFor("options", () => shape.CreateOffsetLoopsWithIndex(0.2, null!));
        }

        // ----------------------------------------------------------------------------------
        // Polyline
        // ----------------------------------------------------------------------------------

        [Fact]
        public void PolylineConstructorNamesTheVertexesParameter()
        {
            AssertThrowsFor("vertexes", () => new Polyline<double>(null!, false));
        }

        [Fact]
        public void ExtendVertexesNamesTheVertexesParameter()
        {
            var pline = Square();
            AssertThrowsFor("vertexes", () => pline.ExtendVertexes(null!));
        }

        [Fact]
        public void AddUserDataValuesNamesTheValuesParameter()
        {
            var pline = Square();
            AssertThrowsFor("values", () => pline.AddUserDataValues(null!));
        }

        // ----------------------------------------------------------------------------------
        // StaticAABB2DIndex
        // ----------------------------------------------------------------------------------

        [Fact]
        public void VisitQueryWithStackRejectsNullStack()
        {
            var index = Square().CreateApproxAabbIndex();
            AssertThrowsFor("stack", () =>
                index.VisitQueryWithStack(0.0, 0.0, 2.0, 2.0, _ => true, null!));
        }

        [Fact]
        public void VisitQueryRejectsNullVisitorDelegate()
        {
            var index = Square().CreateApproxAabbIndex();
            AssertThrowsFor("visitor", () =>
                index.VisitQuery(0.0, 0.0, 2.0, 2.0, null!));
        }

        // ----------------------------------------------------------------------------------
        // IPlineSource extension methods, called on a null source
        // ----------------------------------------------------------------------------------

        [Fact]
        public void SourceExtensionsRejectNullSource()
        {
            IPlineSource<double> nil = null!;

            AssertThrowsFor("pline", () => nil.IsEmpty());
            AssertThrowsFor("pline", () => nil.Last());
            AssertThrowsFor("pline", () => nil.SegmentCount());
            AssertThrowsFor("pline", () => nil.NextWrappingIndex(0));
            AssertThrowsFor("pline", () => nil.PrevWrappingIndex(0));
            AssertThrowsFor("pline", () => nil.FwdWrappingDist(0, 1));
            AssertThrowsFor("pline", () => nil.FwdWrappingIndex(0, 1));
            AssertThrowsFor("pline", () => nil.Extents());
            AssertThrowsFor("pline", () => nil.PathLength());
            AssertThrowsFor("pline", () => nil.Area());
            AssertThrowsFor("pline", () => nil.Orientation());
            AssertThrowsFor("pline", () => nil.RemoveRepeatPos(1e-5));
            AssertThrowsFor("pline", () => nil.CreateApproxAabbIndex());
            AssertThrowsFor("pline", () => nil.CreateAabbIndex());
            AssertThrowsFor("pline", () => nil.ClosestPoint(new Vector2<double>(0.0, 0.0), 1e-5));
            AssertThrowsFor("pline", () => nil.WindingNumber(new Vector2<double>(0.0, 0.0)));
            AssertThrowsFor("pline", () => nil.ArcsToApproxLines(1e-3));
            AssertThrowsFor("pline", () => nil.FindPointAtPathLength(1.0));
            AssertThrowsFor("pline", () => PlineSourceExtensions.CreateFrom<Polyline<double>, double>(nil));
            AssertThrowsFor("pline", () =>
                PlineSourceExtensions.CreateFromRemoveRepeat<Polyline<double>, double>(nil, 1e-5));
            AssertThrowsFor("self", () => nil.RemoveRedundant(1e-5));
            AssertThrowsFor("self", () => nil.RotateStart(0, new Vector2<double>(0.0, 0.0), 1e-5));
            AssertThrowsFor("self", () => nil.FuzzyEq(Square()));
            AssertThrowsFor("self", () => nil.FuzzyEqEps(Square(), 1e-5));
            AssertThrowsFor("other", () => Square().FuzzyEq(null!));
            AssertThrowsFor("other", () => Square().FuzzyEqEps(null!, 1e-5));
        }

        /// <summary>
        /// The iterating extension methods must reject a null source at the call itself, not on
        /// the first <c>MoveNext</c>. A compiler generated iterator defers its whole body, so these
        /// need an eagerly evaluated wrapper around the iterator to make the guard observable.
        /// </summary>
        [Fact]
        public void IteratingSourceExtensionsRejectNullSourceEagerly()
        {
            IPlineSource<double> nil = null!;

            AssertThrowsFor("pline", () => nil.IterSegments());
            AssertThrowsFor("pline", () => nil.IterVertexes());
            AssertThrowsFor("pline", () => nil.IterSegmentIndexes());
        }

        [Fact]
        public void MutatingSourceExtensionsRejectNullSource()
        {
            IPlineSourceMut<double> nil = null!;

            AssertThrowsFor("pline", () => nil.Add(1.0, 2.0, 0.0));
            AssertThrowsFor("self", () => nil.InvertDirection());
            AssertThrowsFor("self", () =>
                nil.AddOrReplaceVertex(new PlineVertex<double>(1.0, 2.0, 0.0), 1e-5));
            AssertThrowsFor("self", () => nil.ExtendRemoveRepeat(Square(), 1e-5));
            AssertThrowsFor("other", () => Square().ExtendRemoveRepeat(null!, 1e-5));
        }

        // ----------------------------------------------------------------------------------
        // Valid input must keep working unchanged
        // ----------------------------------------------------------------------------------

        [Fact]
        public void ValidInputIsUnaffectedByTheGuards()
        {
            var pline = Square();

            var offset = PlineOffset.ParallelOffset<Polyline<double>, double>(
                pline, 0.2, new PlineOffsetOptions<double>());
            Assert.Single(offset);
            Assert.Equal(1.6 * 1.6, offset[0].Area(), 9);

            Assert.Equal(4, pline.SegmentCount());
            Assert.False(pline.IsEmpty());
            Assert.Equal(4, pline.IterSegments().Count());
            Assert.Equal(4, pline.IterVertexes().Count());
            Assert.Equal(4, pline.IterSegmentIndexes().Count());

            var contains = PlineContains.PolylineContains(
                pline, OffsetSquare(), new PlineContainsOptions<double>());
            Assert.Equal(PlineContainsResult.Intersected, contains);
        }

        /// <summary>
        /// Two public methods ignore their source polyline entirely and therefore accept null
        /// today. Guarding them would reject input that currently succeeds, so they are left as
        /// they are; this test pins that down so the tolerance is not removed by accident.
        /// </summary>
        [Fact]
        public void MethodsThatIgnoreTheirSourceStillAcceptNull()
        {
            var single = PlineViewData<double>.CreateOnSingleSegment(
                null!,
                startIndex: 0,
                updatedStart: new PlineVertex<double>(0.0, 0.0, 0.0),
                endIntersect: new Vector2<double>(2.0, 0.0),
                posEqualEps: 1e-5);
            Assert.NotNull(single);
            Assert.Equal(0, single!.Value.StartIndex);

            var viewData = PlineViewData<double>.FromEntirePline(Square());
            var overlapping = new OverlappingSlice<double>(
                (0, 0), (1, 1), viewData, isLoop: false, opposingDirections: false);
            var slice = BooleanPlineSlice<double>.FromOverlapping(null!, in overlapping, inverted: false);
            Assert.True(slice.Overlapping);
            Assert.False(slice.SourceIsPline1);
        }
    }
}
