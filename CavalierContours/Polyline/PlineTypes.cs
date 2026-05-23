using System;
using System.Collections.Generic;
using System.Numerics;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    public enum PlineOrientation : byte
    {
        Open,
        Clockwise,
        CounterClockwise
    }

    public readonly struct ClosestPointResult<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public readonly int SegStartIndex;
        public readonly Vector2<T> SegPoint;
        public readonly T Distance;

        public ClosestPointResult(int segStartIndex, Vector2<T> segPoint, T distance)
        {
            SegStartIndex = segStartIndex;
            SegPoint = segPoint;
            Distance = distance;
        }
    }

    public class PlineOffsetOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public StaticAABB2DIndex<T>? AabbIndex { get; set; }
        public bool HandleSelfIntersects { get; set; }
        public T PosEqualEps { get; set; }
        public T SliceJoinEps { get; set; }
        public T OffsetDistEps { get; set; }

        public PlineOffsetOptions()
        {
            AabbIndex = null;
            HandleSelfIntersects = false;
            PosEqualEps = T.CreateChecked(1e-5);
            SliceJoinEps = T.CreateChecked(1e-4);
            OffsetDistEps = T.CreateChecked(1e-4);
        }
    }

    public enum PlineContainsResult : byte
    {
        InvalidInput,
        Pline1InsidePline2,
        Pline2InsidePline1,
        Disjoint,
        Intersected
    }

    public class PlineContainsOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public StaticAABB2DIndex<T>? Pline1AabbIndex { get; set; }
        public T PosEqualEps { get; set; }

        public PlineContainsOptions()
        {
            Pline1AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
        }
    }

    public enum BooleanOp : byte
    {
        Or,
        And,
        Not,
        Xor
    }

    public class BooleanResultPline<P, T>
        where P : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public P Pline { get; set; }
        public List<BooleanPlineSlice<T>> Subslices { get; set; }

        public BooleanResultPline(P pline, List<BooleanPlineSlice<T>> subslices)
        {
            Pline = pline;
            Subslices = subslices;
        }
    }

    public enum BooleanResultInfo : byte
    {
        InvalidInput,
        Pline1InsidePline2,
        Pline2InsidePline1,
        Disjoint,
        Overlapping,
        Intersected
    }

    public class BooleanResult<P, T>
        where P : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public List<BooleanResultPline<P, T>> PosPlines { get; set; }
        public List<BooleanResultPline<P, T>> NegPlines { get; set; }
        public BooleanResultInfo ResultInfo { get; set; }

        public BooleanResult(List<BooleanResultPline<P, T>> posPlines, List<BooleanResultPline<P, T>> negPlines, BooleanResultInfo resultInfo)
        {
            PosPlines = posPlines;
            NegPlines = negPlines;
            ResultInfo = resultInfo;
        }

        public static BooleanResult<P, T> Empty(BooleanResultInfo resultInfo)
        {
            return new BooleanResult<P, T>(new List<BooleanResultPline<P, T>>(), new List<BooleanResultPline<P, T>>(), resultInfo);
        }

        public static BooleanResult<P, T> FromWholePlines(IEnumerable<P> posPlines, IEnumerable<P> negPlines, BooleanResultInfo resultInfo)
        {
            var pos = new List<BooleanResultPline<P, T>>();
            foreach (var p in posPlines)
            {
                pos.Add(new BooleanResultPline<P, T>(p, new List<BooleanPlineSlice<T>>()));
            }

            var neg = new List<BooleanResultPline<P, T>>();
            foreach (var p in negPlines)
            {
                neg.Add(new BooleanResultPline<P, T>(p, new List<BooleanPlineSlice<T>>()));
            }

            return new BooleanResult<P, T>(pos, neg, resultInfo);
        }
    }

    public class PlineBooleanOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public StaticAABB2DIndex<T>? Pline1AabbIndex { get; set; }
        public T PosEqualEps { get; set; }
        public T? CollapsedAreaEps { get; set; }

        public PlineBooleanOptions()
        {
            Pline1AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
            CollapsedAreaEps = null;
        }
    }

    public enum SelfIntersectsInclude : byte
    {
        All,
        Local,
        Global
    }

    public class PlineSelfIntersectOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public StaticAABB2DIndex<T>? AabbIndex { get; set; }
        public T PosEqualEps { get; set; }
        public SelfIntersectsInclude Include { get; set; }

        public PlineSelfIntersectOptions()
        {
            AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
            Include = SelfIntersectsInclude.All;
        }
    }

    public class FindIntersectsOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public StaticAABB2DIndex<T>? Pline1AabbIndex { get; set; }
        public T PosEqualEps { get; set; }

        public FindIntersectsOptions()
        {
            Pline1AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
        }
    }

    public readonly struct PlineBasicIntersect<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly int StartIndex1;
        public readonly int StartIndex2;
        public readonly Vector2<T> Point;

        public PlineBasicIntersect(int startIndex1, int startIndex2, Vector2<T> point)
        {
            StartIndex1 = startIndex1;
            StartIndex2 = startIndex2;
            Point = point;
        }
    }

    public readonly struct PlineOverlappingIntersect<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly int StartIndex1;
        public readonly int StartIndex2;
        public readonly Vector2<T> Point1;
        public readonly Vector2<T> Point2;

        public PlineOverlappingIntersect(int startIndex1, int startIndex2, Vector2<T> point1, Vector2<T> point2)
        {
            StartIndex1 = startIndex1;
            StartIndex2 = startIndex2;
            Point1 = point1;
            Point2 = point2;
        }
    }

    public enum PlineIntersectKind : byte
    {
        Basic,
        Overlapping
    }

    public readonly struct PlineIntersect<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly PlineIntersectKind Kind;
        public readonly PlineBasicIntersect<T> Basic;
        public readonly PlineOverlappingIntersect<T> Overlapping;

        private PlineIntersect(PlineBasicIntersect<T> basic)
        {
            Kind = PlineIntersectKind.Basic;
            Basic = basic;
            Overlapping = default;
        }

        private PlineIntersect(PlineOverlappingIntersect<T> overlapping)
        {
            Kind = PlineIntersectKind.Overlapping;
            Basic = default;
            Overlapping = overlapping;
        }

        public static PlineIntersect<T> NewBasic(int startIndex1, int startIndex2, Vector2<T> point)
        {
            return new PlineIntersect<T>(new PlineBasicIntersect<T>(startIndex1, startIndex2, point));
        }

        public static PlineIntersect<T> NewOverlapping(int startIndex1, int startIndex2, Vector2<T> point1, Vector2<T> point2)
        {
            return new PlineIntersect<T>(new PlineOverlappingIntersect<T>(startIndex1, startIndex2, point1, point2));
        }
    }

    public interface IPlineIntersectVisitor<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        bool VisitBasicIntr(PlineBasicIntersect<T> intr);
        bool VisitOverlappingIntr(PlineOverlappingIntersect<T> intr);
    }

    public readonly struct PlineIntersectVisitContext<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly int VertexIndex;
        public readonly PlineVertex<T> V1;
        public readonly PlineVertex<T> V2;

        public PlineIntersectVisitContext(int vertexIndex, PlineVertex<T> v1, PlineVertex<T> v2)
        {
            VertexIndex = vertexIndex;
            V1 = v1;
            V2 = v2;
        }
    }

    public interface ITwoPlinesIntersectVisitor<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        bool Visit(PlineSegIntr<T> intersect, in PlineIntersectVisitContext<T> pline1Context, in PlineIntersectVisitContext<T> pline2Context);
    }

    public interface IPlineVertexVisitor<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        bool VisitVertex(PlineVertex<T> vertex);
    }

    public interface IPlineSegVisitor<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        bool VisitSeg(PlineVertex<T> v1, PlineVertex<T> v2);
    }

    public class PlineIntersectsCollection<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public List<PlineBasicIntersect<T>> BasicIntersects { get; set; }
        public List<PlineOverlappingIntersect<T>> OverlappingIntersects { get; set; }

        public PlineIntersectsCollection(List<PlineBasicIntersect<T>> basicIntersects, List<PlineOverlappingIntersect<T>> overlappingIntersects)
        {
            BasicIntersects = basicIntersects;
            OverlappingIntersects = overlappingIntersects;
        }

        public static PlineIntersectsCollection<T> NewEmpty()
        {
            return new PlineIntersectsCollection<T>(new List<PlineBasicIntersect<T>>(), new List<PlineOverlappingIntersect<T>>());
        }
    }

    public readonly struct OverlappingSlice<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public readonly (int First, int Second) StartIndexes;
        public readonly (int First, int Second) EndIndexes;
        public readonly PlineViewData<T> ViewData;
        public readonly bool IsLoop;
        public readonly bool OpposingDirections;

        public OverlappingSlice(
            (int First, int Second) startIndexes,
            (int First, int Second) endIndexes,
            PlineViewData<T> viewData,
            bool isLoop,
            bool opposingDirections)
        {
            StartIndexes = startIndexes;
            EndIndexes = endIndexes;
            ViewData = viewData;
            IsLoop = isLoop;
            OpposingDirections = opposingDirections;
        }

        public static OverlappingSlice<T> New(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            PlineOverlappingIntersect<T> startIntr,
            PlineOverlappingIntersect<T>? endIntr,
            T posEqualEps)
        {
            var startV1 = pline1.Get(startIntr.StartIndex1);
            var startV2 = pline1.Get(pline1.NextWrappingIndex(startIntr.StartIndex1));
            var startU1 = pline2.Get(startIntr.StartIndex2);
            var startU2 = pline2.Get(pline2.NextWrappingIndex(startIntr.StartIndex2));

            bool opposingDirections;
            {
                var t1 = PlineSeg.SegTangentVector(startV1, startV2, startIntr.Point1);
                var t2 = PlineSeg.SegTangentVector(startU1, startU2, startIntr.Point1);
                opposingDirections = t1.Dot(t2) < T.Zero;
            }

            var startIndexes = (startIntr.StartIndex1, startIntr.StartIndex2);

            PlineVertex<T> CreateUpdatedStart()
            {
                var split1 = PlineSeg.SegSplitAtPoint(startU1, startU2, startIntr.Point1, posEqualEps);
                var split2 = PlineSeg.SegSplitAtPoint(split1.SplitVertex, startU2, startIntr.Point2, posEqualEps);
                return split2.UpdatedStart;
            }

            if (endIntr == null)
            {
                var updatedStart = CreateUpdatedStart();
                var updatedEndBulge = updatedStart.Bulge;
                var endPoint = startIntr.Point2;
                var endIndexOffset = 0;

                var viewData = new PlineViewData<T>(
                    startIndexes.Item2,
                    endIndexOffset,
                    updatedStart,
                    updatedEndBulge,
                    endPoint,
                    false
                );

                return new OverlappingSlice<T>(startIndexes, startIndexes, viewData, false, opposingDirections);
            }
            else
            {
                var endIntrVal = endIntr.Value;
                if (endIntrVal.Point2.FuzzyEqEps(startIntr.Point1, posEqualEps))
                {
                    var viewData = new PlineViewData<T>(
                        startIndexes.Item2,
                        pline2.VertexCount - 1,
                        startU1,
                        pline2.Get(pline2.VertexCount - 1).Bulge,
                        endIntrVal.Point2,
                        false
                    );

                    return new OverlappingSlice<T>(startIndexes, startIndexes, viewData, true, opposingDirections);
                }
                else
                {
                    var endPoint = endIntrVal.Point2;
                    var endIndexes = (endIntrVal.StartIndex1, endIntrVal.StartIndex2);
                    var endIndexOffset = pline2.FwdWrappingDist(startIndexes.Item2, endIntrVal.StartIndex2);

                    if (startIntr.StartIndex2 == endIntrVal.StartIndex2)
                    {
                        var updatedStart = CreateUpdatedStart();
                        var updatedEndBulge = updatedStart.Bulge;

                        var viewData = new PlineViewData<T>(
                            startIndexes.Item2,
                            endIndexOffset,
                            updatedStart,
                            updatedEndBulge,
                            endPoint,
                            false
                        );

                        return new OverlappingSlice<T>(startIndexes, endIndexes, viewData, false, opposingDirections);
                    }
                    else
                    {
                        var updatedStart = PlineSeg.SegSplitAtPoint(startU1, startU2, startIntr.Point1, posEqualEps).SplitVertex;

                        var endU1 = pline2.Get(endIntrVal.StartIndex2);
                        var endU2 = pline2.Get(pline2.NextWrappingIndex(endIntrVal.StartIndex2));

                        var split1 = PlineSeg.SegSplitAtPoint(endU1, endU2, endIntrVal.Point1, posEqualEps);
                        var split2 = PlineSeg.SegSplitAtPoint(split1.SplitVertex, endU2, endIntrVal.Point2, posEqualEps);
                        var updatedEnd = split2.UpdatedStart;

                        var viewData = new PlineViewData<T>(
                            startIndexes.Item2,
                            endIndexOffset,
                            updatedStart,
                            updatedEnd.Bulge,
                            endPoint,
                            false
                        );

                        return new OverlappingSlice<T>(startIndexes, endIndexes, viewData, false, opposingDirections);
                    }
                }
            }
        }
    }

    public readonly struct BooleanPlineSlice<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public readonly PlineViewData<T> ViewData;
        public readonly bool SourceIsPline1;
        public readonly bool Overlapping;

        public BooleanPlineSlice(PlineViewData<T> viewData, bool sourceIsPline1, bool overlapping)
        {
            ViewData = viewData;
            SourceIsPline1 = sourceIsPline1;
            Overlapping = overlapping;
        }

        public static BooleanPlineSlice<T> FromOpenPlineSlice(in PlineViewData<T> data, bool sourceIsPline1, bool inverted)
        {
            var viewData = new PlineViewData<T>(
                data.StartIndex,
                data.EndIndexOffset,
                data.UpdatedStart,
                data.UpdatedEndBulge,
                data.EndPoint,
                inverted
            );
            return new BooleanPlineSlice<T>(viewData, sourceIsPline1, false);
        }

        public static BooleanPlineSlice<T> FromOverlapping(IPlineSource<T> source, in OverlappingSlice<T> overlappingSlice, bool inverted)
        {
            var viewData = new PlineViewData<T>(
                overlappingSlice.StartIndexes.Second,
                overlappingSlice.ViewData.EndIndexOffset,
                overlappingSlice.ViewData.UpdatedStart,
                overlappingSlice.ViewData.UpdatedEndBulge,
                overlappingSlice.ViewData.EndPoint,
                inverted
            );
            return new BooleanPlineSlice<T>(viewData, false, true);
        }

        public PlineView<T> View(IPlineSource<T> source)
        {
            return ViewData.View(source);
        }
    }

    public enum ViewDataValidation : byte
    {
        SourceHasNoSegments,
        OffsetOutOfRange,
        UpdatedStartNotOnSegment,
        EndPointNotOnSegment,
        EndPointOnFinalOffsetVertex,
        UpdatedBulgeDoesNotMatch,
        IsValid
    }
}
