using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    public readonly struct PlineView<T> : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public readonly IPlineSource<T> Source;
        public readonly PlineViewData<T> Data;

        public PlineView(IPlineSource<T> source, PlineViewData<T> data)
        {
            Source = source;
            Data = data;
        }

        public int VertexCount => Data.VertexCount;
        public bool IsClosed => false;

        public int UserDataCount => Source.UserDataCount;
        public IEnumerable<ulong> UserDataValues => Source.UserDataValues;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> Get(int index)
        {
            var v = Data.GetVertex(Source, index);
            if (v == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of view range");
            }
            return v.Value;
        }
    }

    public readonly struct PlineViewData<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public readonly int StartIndex;
        public readonly int EndIndexOffset;
        public readonly PlineVertex<T> UpdatedStart;
        public readonly T UpdatedEndBulge;
        public readonly Vector2<T> EndPoint;
        public readonly bool InvertedDirection;

        public PlineViewData(int startIndex, int endIndexOffset, PlineVertex<T> updatedStart, T updatedEndBulge, Vector2<T> endPoint, bool invertedDirection)
        {
            StartIndex = startIndex;
            EndIndexOffset = endIndexOffset;
            UpdatedStart = updatedStart;
            UpdatedEndBulge = updatedEndBulge;
            EndPoint = endPoint;
            InvertedDirection = invertedDirection;
        }

        public int VertexCount => EndIndexOffset + 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineView<T> View(IPlineSource<T> source)
        {
            Debug.Assert(ValidateForSource(source) == ViewDataValidation.IsValid);
            return new PlineView<T>(source, this);
        }

        public PlineVertex<T>? GetVertex(IPlineSource<T> source, int index)
        {
            if (index >= VertexCount) return null;

            if (InvertedDirection)
            {
                if (index == 0)
                {
                    return PlineVertex<T>.FromVector2(EndPoint, -UpdatedEndBulge);
                }

                if (index < EndIndexOffset)
                {
                    int bulgeI = source.FwdWrappingIndex(StartIndex, EndIndexOffset - index);
                    int i = source.NextWrappingIndex(bulgeI);
                    return source.Get(i).WithBulge(-source.Get(bulgeI).Bulge);
                }

                if (index == EndIndexOffset)
                {
                    int i = source.FwdWrappingIndex(StartIndex, EndIndexOffset - index + 1);
                    return source.Get(i).WithBulge(-UpdatedStart.Bulge);
                }

                if (index == EndIndexOffset + 1)
                {
                    return UpdatedStart.WithBulge(T.Zero);
                }
            }
            else
            {
                if (index == 0)
                {
                    return UpdatedStart;
                }

                if (index < EndIndexOffset)
                {
                    int i = source.FwdWrappingIndex(StartIndex, index);
                    return source.Get(i);
                }

                if (index == EndIndexOffset)
                {
                    int i = source.FwdWrappingIndex(StartIndex, EndIndexOffset);
                    return source.Get(i).WithBulge(UpdatedEndBulge);
                }

                if (index == EndIndexOffset + 1)
                {
                    return PlineVertex<T>.FromVector2(EndPoint, T.Zero);
                }
            }

            return null;
        }

        public static PlineViewData<T>? CreateOnSingleSegment(
            IPlineSource<T> source,
            int startIndex,
            PlineVertex<T> updatedStart,
            Vector2<T> endIntersect,
            T posEqualEps)
        {
            if (updatedStart.Pos().FuzzyEqEps(endIntersect, posEqualEps))
            {
                return null;
            }
            return new PlineViewData<T>(startIndex, 0, updatedStart, updatedStart.Bulge, endIntersect, false);
        }

        public static PlineViewData<T> Create(
            IPlineSource<T> source,
            int startIndex,
            Vector2<T> endIntersect,
            int intersectIndex,
            PlineVertex<T> updatedStart,
            int traverseCount,
            T posEqualEps)
        {
            Debug.Assert(traverseCount != 0, "traverseCount must be greater than 0");

            PlineVertex<T> currentVertex = source.Get(intersectIndex);
            int endIndexOffset;
            T updatedEndBulge;

            if (endIntersect.FuzzyEqEps(currentVertex.Pos(), posEqualEps))
            {
                endIndexOffset = traverseCount - 1;
                updatedEndBulge = endIndexOffset != 0
                    ? source.Get(source.PrevWrappingIndex(intersectIndex)).Bulge
                    : updatedStart.Bulge;
            }
            else
            {
                int nextIndex = source.NextWrappingIndex(intersectIndex);
                var split = PlineSeg.SegSplitAtPoint(currentVertex, source.Get(nextIndex), endIntersect, posEqualEps);
                endIndexOffset = traverseCount;
                updatedEndBulge = split.UpdatedStart.Bulge;
            }

            return new PlineViewData<T>(startIndex, endIndexOffset, updatedStart, updatedEndBulge, endIntersect, false);
        }

        public static PlineViewData<T> FromEntirePline(IPlineSource<T> source)
        {
            int vc = source.VertexCount;
            Debug.Assert(vc >= 2, "source must have at least 2 vertexes");

            if (source.IsClosed)
            {
                return new PlineViewData<T>(0, vc - 1, source.Get(0), source.Get(vc - 1).Bulge, source.Get(0).Pos(), false);
            }
            else
            {
                return new PlineViewData<T>(0, vc - 2, source.Get(0), source.Get(vc - 2).Bulge, source.Get(vc - 1).Pos(), false);
            }
        }

        public static PlineViewData<T>? FromNewStart(
            IPlineSource<T> source,
            Vector2<T> startPoint,
            int startIndex,
            T posEqualEps)
        {
            if (!source.IsClosed)
            {
                return FromSlicePoints(source, startPoint, startIndex, source.Last()!.Value.Pos(), source.VertexCount - 1, posEqualEps);
            }

            int vc = source.VertexCount;
            Debug.Assert(vc >= 2, "source must have at least 2 vertexes");

            int nextIdx = source.NextWrappingIndex(startIndex);
            if (source.Get(nextIdx).Pos().FuzzyEqEps(startPoint, posEqualEps))
            {
                startIndex = nextIdx;
            }

            PlineVertex<T> startV1 = source.Get(startIndex);
            PlineVertex<T> startV2 = source.Get(source.NextWrappingIndex(startIndex));
            var split = PlineSeg.SegSplitAtPoint(startV1, startV2, startPoint, posEqualEps);

            int endIndexOffset = startV1.Pos().FuzzyEqEps(startPoint, posEqualEps) ? vc - 1 : vc;
            T updatedEndBulge = startV1.Pos().FuzzyEqEps(startPoint, posEqualEps)
                ? source.Get(source.PrevWrappingIndex(startIndex)).Bulge
                : split.UpdatedStart.Bulge;

            return new PlineViewData<T>(startIndex, endIndexOffset, split.SplitVertex, updatedEndBulge, startPoint, false);
        }

        public static PlineViewData<T>? FromSlicePoints(
            IPlineSource<T> source,
            Vector2<T> startPoint,
            int startIndex,
            Vector2<T> endPoint,
            int endIndex,
            T posEqualEps)
        {
            Debug.Assert(startIndex <= endIndex || source.IsClosed, "startIndex must be <= endIndex if open");

            int nextIdx = source.NextWrappingIndex(startIndex);
            bool startPointAtSegEnd = false;
            if (source.IsClosed || startIndex < endIndex)
            {
                if (source.Get(nextIdx).Pos().FuzzyEqEps(startPoint, posEqualEps))
                {
                    startIndex = nextIdx;
                    startPointAtSegEnd = true;
                }
            }

            int traverseCount;
            int indexDist = source.FwdWrappingDist(startIndex, endIndex);
            if (indexDist == 0 && source.IsClosed && !startPoint.FuzzyEqEps(endPoint, posEqualEps))
            {
                Vector2<T> segStart = source.Get(startIndex).Pos();
                T dist1 = BaseMath.DistSquared(segStart, startPoint);
                T dist2 = BaseMath.DistSquared(segStart, endPoint);
                traverseCount = dist1 < dist2 ? 0 : source.VertexCount;
            }
            else
            {
                traverseCount = indexDist;
            }

            PlineVertex<T> startV1 = source.Get(startIndex);
            PlineVertex<T> startV2 = source.Get(source.NextWrappingIndex(startIndex));
            PlineVertex<T> updatedStart;

            if (startPointAtSegEnd)
            {
                if (traverseCount == 0)
                {
                    var split = PlineSeg.SegSplitAtPoint(startV1, startV2, endPoint, posEqualEps);
                    updatedStart = split.UpdatedStart;
                }
                else
                {
                    updatedStart = startV1;
                }
            }
            else
            {
                var startSplit = PlineSeg.SegSplitAtPoint(startV1, startV2, startPoint, posEqualEps);
                var updatedForStart = startSplit.SplitVertex;
                if (traverseCount == 0)
                {
                    var split = PlineSeg.SegSplitAtPoint(updatedForStart, startV2, endPoint, posEqualEps);
                    updatedStart = split.UpdatedStart;
                }
                else
                {
                    updatedStart = updatedForStart;
                }
            }

            if (traverseCount == 0)
            {
                return CreateOnSingleSegment(source, startIndex, updatedStart, endPoint, posEqualEps);
            }
            else
            {
                return Create(source, startIndex, endPoint, endIndex, updatedStart, traverseCount, posEqualEps);
            }
        }

        public ViewDataValidation ValidateForSource(IPlineSource<T> source)
        {
            if (source.VertexCount < 2) return ViewDataValidation.SourceHasNoSegments;
            if (EndIndexOffset > source.VertexCount) return ViewDataValidation.OffsetOutOfRange;

            T validationEps = T.CreateChecked(1e-5);
            T onSegEps = T.CreateChecked(1e-3);

            bool PointIsOnSegment(int segIdx, Vector2<T> pt)
            {
                var sv1 = source.Get(segIdx);
                var sv2 = source.Get(source.NextWrappingIndex(segIdx));
                if (pt.FuzzyEqEps(sv1.Pos(), onSegEps) || pt.FuzzyEqEps(sv2.Pos(), onSegEps)) return true;
                var closest = PlineSeg.SegClosestPoint(sv1, sv2, pt, validationEps);
                return closest.FuzzyEqEps(pt, onSegEps);
            }

            if (!PointIsOnSegment(StartIndex, UpdatedStart.Pos()))
            {
                return ViewDataValidation.UpdatedStartNotOnSegment;
            }

            int endIdx = source.FwdWrappingIndex(StartIndex, EndIndexOffset);
            if (!PointIsOnSegment(endIdx, EndPoint))
            {
                return ViewDataValidation.EndPointNotOnSegment;
            }

            if (EndPoint.FuzzyEqEps(source.Get(endIdx).Pos(), validationEps))
            {
                return ViewDataValidation.EndPointOnFinalOffsetVertex;
            }

            if (EndIndexOffset == 0)
            {
                if (!UpdatedEndBulge.FuzzyEq(UpdatedStart.Bulge, validationEps))
                {
                    return ViewDataValidation.UpdatedBulgeDoesNotMatch;
                }
            }

            return ViewDataValidation.IsValid;
        }
    }
}
