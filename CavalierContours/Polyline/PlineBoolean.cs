using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    internal readonly struct SlicePoint<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly Vector2<T> Pos;
        public readonly bool IsStartOfOverlappingSlice;

        public SlicePoint(Vector2<T> pos, bool isStartOfOverlappingSlice)
        {
            Pos = pos;
            IsStartOfOverlappingSlice = isStartOfOverlappingSlice;
        }
    }

    public class ProcessForBooleanResult<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public List<OverlappingSlice<T>> OverlappingSlices { get; set; } = new();
        public List<PlineBasicIntersect<T>> Intersects { get; set; } = new();
        public PlineOrientation Pline1Orientation { get; set; }
        public PlineOrientation Pline2Orientation { get; set; }

        public bool CompletelyOverlapping()
        {
            return OverlappingSlices.Count == 1 && OverlappingSlices[0].IsLoop;
        }

        public bool OpposingDirections()
        {
            return Pline1Orientation != Pline2Orientation;
        }

        public bool AnyIntersects()
        {
            return Intersects.Count > 0 || OverlappingSlices.Count > 0;
        }
    }

    public class PrunedSlices<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public List<BooleanPlineSlice<T>> SlicesRemaining { get; set; } = new();
        public int StartOfPline2Slices { get; set; }
        public int StartOfPline1OverlappingSlices { get; set; }
        public int StartOfPline2OverlappingSlices { get; set; }
    }

    public interface IStitchSelector
    {
        int? Select(int currentSliceIdx, ReadOnlySpan<int> availableIdx);
    }

    public class OrAndStitchSelector : IStitchSelector
    {
        private readonly int _startOfPline2Slices;
        private readonly int _startOfPline1OverlappingSlices;
        private readonly int _startOfPline2OverlappingSlices;

        public OrAndStitchSelector(
            int startOfPline2Slices,
            int startOfPline1OverlappingSlices,
            int startOfPline2OverlappingSlices)
        {
            _startOfPline2Slices = startOfPline2Slices;
            _startOfPline1OverlappingSlices = startOfPline1OverlappingSlices;
            _startOfPline2OverlappingSlices = startOfPline2OverlappingSlices;
        }

        public static OrAndStitchSelector FromPrunedSlices<T>(PrunedSlices<T> prunedSlices)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            return new OrAndStitchSelector(
                prunedSlices.StartOfPline2Slices,
                prunedSlices.StartOfPline1OverlappingSlices,
                prunedSlices.StartOfPline2OverlappingSlices
            );
        }

        public int? Select(int currentSliceIdx, ReadOnlySpan<int> availableIdx)
        {
            bool isPline1Idx = currentSliceIdx < _startOfPline2Slices
                || (currentSliceIdx >= _startOfPline1OverlappingSlices && currentSliceIdx < _startOfPline2OverlappingSlices);

            if (isPline1Idx)
            {
                foreach (int i in availableIdx)
                {
                    if (i >= _startOfPline2Slices && i < _startOfPline1OverlappingSlices)
                    {
                        return i;
                    }
                }
                foreach (int i in availableIdx)
                {
                    if (i < _startOfPline2Slices)
                    {
                        return i;
                    }
                }
            }
            else
            {
                foreach (int i in availableIdx)
                {
                    if (i < _startOfPline2Slices)
                    {
                        return i;
                    }
                }
                foreach (int i in availableIdx)
                {
                    if (i >= _startOfPline2Slices && i < _startOfPline1OverlappingSlices)
                    {
                        return i;
                    }
                }
            }

            return availableIdx.Length > 0 ? availableIdx[0] : null;
        }
    }

    public class NotXorStitchSelector : IStitchSelector
    {
        private readonly int _startOfPline2Slices;
        private readonly int _startOfPline1OverlappingSlices;
        private readonly int _startOfPline2OverlappingSlices;

        public NotXorStitchSelector(
            int startOfPline2Slices,
            int startOfPline1OverlappingSlices,
            int startOfPline2OverlappingSlices)
        {
            _startOfPline2Slices = startOfPline2Slices;
            _startOfPline1OverlappingSlices = startOfPline1OverlappingSlices;
            _startOfPline2OverlappingSlices = startOfPline2OverlappingSlices;
        }

        public static NotXorStitchSelector FromPrunedSlices<T>(PrunedSlices<T> prunedSlices)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            return new NotXorStitchSelector(
                prunedSlices.StartOfPline2Slices,
                prunedSlices.StartOfPline1OverlappingSlices,
                prunedSlices.StartOfPline2OverlappingSlices
            );
        }

        private int? IdxForPline1Slice(ReadOnlySpan<int> availableIdx)
        {
            foreach (int i in availableIdx)
            {
                if (i < _startOfPline2Slices) return i;
            }
            return null;
        }

        private int? IdxForPline2Slice(ReadOnlySpan<int> availableIdx)
        {
            foreach (int i in availableIdx)
            {
                if (i >= _startOfPline2Slices && i < _startOfPline1OverlappingSlices) return i;
            }
            return null;
        }

        public int? Select(int currentSliceIdx, ReadOnlySpan<int> availableIdx)
        {
            if (currentSliceIdx >= _startOfPline1OverlappingSlices)
            {
                if (currentSliceIdx < _startOfPline2OverlappingSlices)
                {
                    return IdxForPline2Slice(availableIdx) ?? IdxForPline1Slice(availableIdx);
                }
                return IdxForPline1Slice(availableIdx) ?? IdxForPline2Slice(availableIdx);
            }

            if (currentSliceIdx < _startOfPline2Slices)
            {
                return IdxForPline2Slice(availableIdx) ?? (availableIdx.Length > 0 ? availableIdx[0] : null);
            }

            return IdxForPline1Slice(availableIdx) ?? (availableIdx.Length > 0 ? availableIdx[0] : null);
        }
    }

    public static class PlineBoolean
    {
        private class FindIntersectsVisitor<T> : ITwoPlinesIntersectVisitor<T>
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            public readonly IPlineSource<T> Pline1;
            public readonly IPlineSource<T> Pline2;
            public readonly T PosEqualEps;
            public readonly int Open1LastIdx;
            public readonly int Open2LastIdx;
            public readonly PlineIntersectsCollection<T> Result;
            public readonly HashSet<int> PossibleDuplicates1;
            public readonly HashSet<int> PossibleDuplicates2;

            public FindIntersectsVisitor(
                IPlineSource<T> pline1,
                IPlineSource<T> pline2,
                T posEqualEps,
                PlineIntersectsCollection<T> result,
                HashSet<int> possibleDuplicates1,
                HashSet<int> possibleDuplicates2)
            {
                Pline1 = pline1;
                Pline2 = pline2;
                PosEqualEps = posEqualEps;
                Open1LastIdx = pline1.VertexCount - 2;
                Open2LastIdx = pline2.VertexCount - 2;
                Result = result;
                PossibleDuplicates1 = possibleDuplicates1;
                PossibleDuplicates2 = possibleDuplicates2;
            }

            public bool Visit(PlineSegIntr<T> intersect, in PlineIntersectVisitContext<T> pline1Context, in PlineIntersectVisitContext<T> pline2Context)
            {
                int i1 = pline1Context.VertexIndex;
                int i2 = pline2Context.VertexIndex;

                var ctx1 = pline1Context;
                var ctx2 = pline2Context;

                bool SkipIntrAtEnd(Vector2<T> intr)
                {
                    return (ctx1.V2.Pos().FuzzyEqEps(intr, PosEqualEps)
                            && (Pline1.IsClosed || i1 != Open1LastIdx))
                        || (ctx2.V2.Pos().FuzzyEqEps(intr, PosEqualEps)
                            && (Pline2.IsClosed || i2 != Open2LastIdx));
                }

                switch (intersect.Kind)
                {
                    case PlineSegIntrKind.NoIntersect:
                        break;
                    case PlineSegIntrKind.TangentIntersect:
                    case PlineSegIntrKind.OneIntersect:
                        {
                            var point = intersect.Point1;
                            if (!SkipIntrAtEnd(point))
                            {
                                Result.BasicIntersects.Add(new PlineBasicIntersect<T>(i1, i2, point));
                            }
                            break;
                        }
                    case PlineSegIntrKind.TwoIntersects:
                        {
                            var point1 = intersect.Point1;
                            var point2 = intersect.Point2;
                            if (!SkipIntrAtEnd(point1))
                            {
                                Result.BasicIntersects.Add(new PlineBasicIntersect<T>(i1, i2, point1));
                            }
                            if (!SkipIntrAtEnd(point2))
                            {
                                Result.BasicIntersects.Add(new PlineBasicIntersect<T>(i1, i2, point2));
                            }
                            break;
                        }
                    case PlineSegIntrKind.OverlappingLines:
                    case PlineSegIntrKind.OverlappingArcs:
                        {
                            var point1 = intersect.Point1;
                            var point2 = intersect.Point2;
                            Result.OverlappingIntersects.Add(new PlineOverlappingIntersect<T>(i1, i2, point1, point2));

                            if (pline1Context.V2.Pos().FuzzyEqEps(point1, PosEqualEps)
                                || pline1Context.V2.Pos().FuzzyEqEps(point2, PosEqualEps))
                            {
                                PossibleDuplicates1.Add(Pline1.NextWrappingIndex(i1));
                            }
                            if (pline2Context.V2.Pos().FuzzyEqEps(point1, PosEqualEps)
                                || pline2Context.V2.Pos().FuzzyEqEps(point2, PosEqualEps))
                            {
                                PossibleDuplicates2.Add(Pline2.NextWrappingIndex(i2));
                            }
                            break;
                        }
                }

                return true;
            }
        }

        public static void VisitIntersects<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            ITwoPlinesIntersectVisitor<T> visitor,
            FindIntersectsOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (pline1.VertexCount < 2 || pline2.VertexCount < 2)
            {
                return;
            }

            T posEqualEps = options.PosEqualEps;
            var pline1AabbIndex = options.Pline1AabbIndex ?? pline1.CreateApproxAabbIndex();

            var queryStack = new List<int>(8);

            foreach (var (i2, j2) in pline2.IterSegmentIndexes())
            {
                var pline2Context = new PlineIntersectVisitContext<T>(i2, pline2.Get(i2), pline2.Get(j2));

                var bb = PlineSeg.SegFastApproxBoundingBox(pline2Context.V1, pline2Context.V2);

                pline1AabbIndex.VisitQueryWithStack(
                    bb.MinX - posEqualEps,
                    bb.MinY - posEqualEps,
                    bb.MaxX + posEqualEps,
                    bb.MaxY + posEqualEps,
                    i1 =>
                    {
                        int j1 = pline1.NextWrappingIndex(i1);
                        var pline1Context = new PlineIntersectVisitContext<T>(i1, pline1.Get(i1), pline1.Get(j1));

                        var intr = PlineSegIntersection.Intersect(
                            pline1Context.V1,
                            pline1Context.V2,
                            pline2Context.V1,
                            pline2Context.V2,
                            posEqualEps
                        );

                        return visitor.Visit(intr, pline1Context, pline2Context);
                    },
                    queryStack
                );
            }
        }

        public static PlineIntersectsCollection<T> FindIntersects<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            FindIntersectsOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = PlineIntersectsCollection<T>.NewEmpty();
            if (pline1.VertexCount < 2 || pline2.VertexCount < 2)
            {
                return result;
            }

            T posEqualEps = options.PosEqualEps;
            var possibleDuplicates1 = new HashSet<int>();
            var possibleDuplicates2 = new HashSet<int>();

            var visitor = new FindIntersectsVisitor<T>(pline1, pline2, posEqualEps, result, possibleDuplicates1, possibleDuplicates2);
            VisitIntersects(pline1, pline2, visitor, options);

            if (possibleDuplicates1.Count == 0 && possibleDuplicates2.Count == 0)
            {
                return result;
            }

            var finalBasicIntrs = new List<PlineBasicIntersect<T>>(result.BasicIntersects.Count);
            foreach (var intr in result.BasicIntersects)
            {
                if (possibleDuplicates1.Contains(intr.StartIndex1))
                {
                    var startPt1 = pline1.Get(intr.StartIndex1).Pos();
                    if (intr.Point.FuzzyEqEps(startPt1, posEqualEps))
                    {
                        continue;
                    }
                }

                if (possibleDuplicates2.Contains(intr.StartIndex2))
                {
                    var startPt2 = pline2.Get(intr.StartIndex2).Pos();
                    if (intr.Point.FuzzyEqEps(startPt2, posEqualEps))
                    {
                        continue;
                    }
                }

                finalBasicIntrs.Add(intr);
            }

            result.BasicIntersects = finalBasicIntrs;
            return result;
        }

        public static List<OverlappingSlice<T>> SortAndJoinOverlappingIntersects<T>(
            List<PlineOverlappingIntersect<T>> intersects,
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new List<OverlappingSlice<T>>();
            if (intersects.Count == 0)
            {
                return result;
            }

            intersects.Sort((intrA, intrB) =>
            {
                int cmp = intrA.StartIndex2.CompareTo(intrB.StartIndex2);
                if (cmp != 0) return cmp;

                var start = pline2.Get(intrA.StartIndex2).Pos();
                T dist1 = BaseMath.DistSquared(start, intrA.Point1);
                T dist2 = BaseMath.DistSquared(start, intrB.Point1);
                return dist1.CompareTo(dist2);
            });

            var startIntr = intersects[0];
            PlineOverlappingIntersect<T>? endIntr = null;
            var currentEndPoint = startIntr.Point2;

            for (int i = 1; i < intersects.Count; i++)
            {
                var intr = intersects[i];
                if (!intr.Point1.FuzzyEqEps(currentEndPoint, posEqualEps))
                {
                    var slice = OverlappingSlice<T>.New(pline1, pline2, startIntr, endIntr, posEqualEps);
                    result.Add(slice);

                    startIntr = intr;
                    endIntr = null;
                }
                else
                {
                    endIntr = intr;
                }

                currentEndPoint = intr.Point2;
            }

            var finalSlice = OverlappingSlice<T>.New(pline1, pline2, startIntr, endIntr, posEqualEps);
            result.Add(finalSlice);

            if (result.Count > 1)
            {
                var lastSlice = result[result.Count - 1];
                var lastSliceEnd = lastSlice.ViewData.EndPoint;
                var firstSliceBegin = result[0].ViewData.UpdatedStart.Pos();

                if (lastSliceEnd.FuzzyEqEps(firstSliceBegin, posEqualEps))
                {
                    result.RemoveAt(result.Count - 1);
                    var firstSlice = result[0];

                    var updatedStartIndexes = lastSlice.StartIndexes;
                    var updatedViewData = new PlineViewData<T>(
                        lastSlice.StartIndexes.Second,
                        firstSlice.ViewData.EndIndexOffset + lastSlice.ViewData.EndIndexOffset,
                        lastSlice.ViewData.UpdatedStart,
                        firstSlice.ViewData.UpdatedEndBulge,
                        firstSlice.ViewData.EndPoint,
                        firstSlice.ViewData.InvertedDirection
                    );

                    if (lastSlice.ViewData.EndPoint.FuzzyEqEps(pline2.Get(0).Pos(), posEqualEps))
                    {
                        updatedViewData = new PlineViewData<T>(
                            updatedViewData.StartIndex,
                            updatedViewData.EndIndexOffset + 1,
                            updatedViewData.UpdatedStart,
                            updatedViewData.UpdatedEndBulge,
                            updatedViewData.EndPoint,
                            updatedViewData.InvertedDirection
                        );
                    }

                    result[0] = new OverlappingSlice<T>(
                        updatedStartIndexes,
                        firstSlice.EndIndexes,
                        updatedViewData,
                        firstSlice.IsLoop,
                        firstSlice.OpposingDirections
                    );
                }
            }

            return result;
        }

        public static ProcessForBooleanResult<T> ProcessForBoolean<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            StaticAABB2DIndex<T> pline1AabbIndex,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var findOptions = new FindIntersectsOptions<T>
            {
                Pline1AabbIndex = pline1AabbIndex,
                PosEqualEps = posEqualEps
            };

            var intrs = FindIntersects(pline1, pline2, findOptions);

            var overlappingSlices = SortAndJoinOverlappingIntersects(
                intrs.OverlappingIntersects,
                pline1,
                pline2,
                posEqualEps
            );

            var pline1Orientation = pline1.Orientation();
            var pline2Orientation = pline2.Orientation();

            return new ProcessForBooleanResult<T>
            {
                OverlappingSlices = overlappingSlices,
                Intersects = intrs.BasicIntersects,
                Pline1Orientation = pline1Orientation,
                Pline2Orientation = pline2Orientation
            };
        }

        private static void AdjustSpEpIndexes<T>(
            IPlineSource<T> pline,
            ref int spIdx,
            Vector2<T> sp,
            ref int epIdx,
            Vector2<T> ep,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int spIdxNext = pline.NextWrappingIndex(spIdx);
            if (sp.FuzzyEqEps(pline.Get(spIdxNext).Pos(), posEqualEps))
            {
                spIdx = spIdxNext;
            }
            int epIdxNext = pline.NextWrappingIndex(epIdx);
            if (ep.FuzzyEqEps(pline.Get(epIdxNext).Pos(), posEqualEps))
            {
                epIdx = epIdxNext;
            }
        }

        public static void SliceAtIntersects<T>(
            IPlineSource<T> pline,
            ProcessForBooleanResult<T> booleanInfo,
            bool useSecondIndex,
            Func<Vector2<T>, bool> pointOnSlicePred,
            List<BooleanPlineSlice<T>> outputSlices,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var intersectsLookup = new SortedDictionary<int, List<SlicePoint<T>>>();

            if (useSecondIndex)
            {
                foreach (var intr in booleanInfo.Intersects)
                {
                    if (!intersectsLookup.TryGetValue(intr.StartIndex2, out var list))
                    {
                        list = new List<SlicePoint<T>>();
                        intersectsLookup[intr.StartIndex2] = list;
                    }
                    list.Add(new SlicePoint<T>(intr.Point, false));
                }

                foreach (var overlappingSlice in booleanInfo.OverlappingSlices)
                {
                    var sp = overlappingSlice.ViewData.UpdatedStart.Pos();
                    var ep = overlappingSlice.ViewData.EndPoint;
                    int spIdx = overlappingSlice.StartIndexes.Second;
                    int epIdx = overlappingSlice.EndIndexes.Second;
                    AdjustSpEpIndexes(pline, ref spIdx, sp, ref epIdx, ep, posEqualEps);

                    if (!intersectsLookup.TryGetValue(spIdx, out var listSp))
                    {
                        listSp = new List<SlicePoint<T>>();
                        intersectsLookup[spIdx] = listSp;
                    }
                    listSp.Add(new SlicePoint<T>(sp, true));

                    if (!intersectsLookup.TryGetValue(epIdx, out var listEp))
                    {
                        listEp = new List<SlicePoint<T>>();
                        intersectsLookup[epIdx] = listEp;
                    }
                    listEp.Add(new SlicePoint<T>(ep, false));
                }
            }
            else
            {
                foreach (var intr in booleanInfo.Intersects)
                {
                    if (!intersectsLookup.TryGetValue(intr.StartIndex1, out var list))
                    {
                        list = new List<SlicePoint<T>>();
                        intersectsLookup[intr.StartIndex1] = list;
                    }
                    list.Add(new SlicePoint<T>(intr.Point, false));
                }

                foreach (var overlappingSlice in booleanInfo.OverlappingSlices)
                {
                    var sp = overlappingSlice.ViewData.UpdatedStart.Pos();
                    var ep = overlappingSlice.ViewData.EndPoint;
                    int spIdx = overlappingSlice.StartIndexes.First;
                    int epIdx = overlappingSlice.EndIndexes.First;
                    AdjustSpEpIndexes(pline, ref spIdx, sp, ref epIdx, ep, posEqualEps);

                    bool spIsSliceStart = !overlappingSlice.OpposingDirections;

                    if (!intersectsLookup.TryGetValue(spIdx, out var listSp))
                    {
                        listSp = new List<SlicePoint<T>>();
                        intersectsLookup[spIdx] = listSp;
                    }
                    listSp.Add(new SlicePoint<T>(sp, spIsSliceStart));

                    if (!intersectsLookup.TryGetValue(epIdx, out var listEp))
                    {
                        listEp = new List<SlicePoint<T>>();
                        intersectsLookup[epIdx] = listEp;
                    }
                    listEp.Add(new SlicePoint<T>(ep, !spIsSliceStart));
                }
            }

            foreach (var pair in intersectsLookup)
            {
                int i = pair.Key;
                var intrList = pair.Value;
                var startPos = pline.Get(i).Pos();
                intrList.Sort((intr1, intr2) =>
                {
                    T dist1 = BaseMath.DistSquared(intr1.Pos, startPos);
                    T dist2 = BaseMath.DistSquared(intr2.Pos, startPos);
                    return dist1.CompareTo(dist2);
                });
            }

            foreach (var pair in intersectsLookup)
            {
                int startIndex = pair.Key;
                var intrsList = pair.Value;
                int nextIndex = pline.NextWrappingIndex(startIndex);
                var startVertex = pline.Get(startIndex);
                var endVertex = pline.Get(nextIndex);

                if (intrsList.Count != 1)
                {
                    var firstSplit = PlineSeg.SegSplitAtPoint(startVertex, endVertex, intrsList[0].Pos, posEqualEps);
                    var prevVertex = firstSplit.SplitVertex;
                    for (int i = 1; i < intrsList.Count; i++)
                    {
                        var split = PlineSeg.SegSplitAtPoint(prevVertex, endVertex, intrsList[i].Pos, posEqualEps);
                        prevVertex = split.SplitVertex;

                        if (intrsList[i - 1].IsStartOfOverlappingSlice)
                        {
                            continue;
                        }

                        if (split.UpdatedStart.Pos().FuzzyEqEps(split.SplitVertex.Pos(), posEqualEps))
                        {
                            continue;
                        }

                        var midpoint = PlineSeg.SegMidpoint(split.UpdatedStart, split.SplitVertex);
                        if (!pointOnSlicePred(midpoint))
                        {
                            continue;
                        }

                        var opl = PlineViewData<T>.CreateOnSingleSegment(
                            pline,
                            startIndex,
                            split.UpdatedStart,
                            split.SplitVertex.Pos(),
                            posEqualEps
                        );

                        if (opl.HasValue)
                        {
                            outputSlices.Add(BooleanPlineSlice<T>.FromOpenPlineSlice(
                                opl.Value,
                                !useSecondIndex,
                                false
                            ));
                        }
                    }
                }

                var lastIntr = intrsList[intrsList.Count - 1];
                if (lastIntr.IsStartOfOverlappingSlice)
                {
                    continue;
                }

                var sliceStartVertex = PlineSeg.SegSplitAtPoint(startVertex, endVertex, lastIntr.Pos, posEqualEps).SplitVertex;

                int index = nextIndex;
                int loopCount = 0;
                int maxLoopCount = pline.VertexCount;
                while (true)
                {
                    if (loopCount > maxLoopCount)
                    {
                        throw new InvalidOperationException("loopCount exceeded maxLoopCount while creating slices from intersects");
                    }
                    loopCount++;

                    if (intersectsLookup.TryGetValue(index, out var nextIntrList))
                    {
                        var intersectPoint = nextIntrList[0].Pos;

                        var viewData = PlineViewData<T>.Create(
                            pline,
                            startIndex,
                            intersectPoint,
                            index,
                            sliceStartVertex,
                            loopCount,
                            posEqualEps
                        );

                        var slice = BooleanPlineSlice<T>.FromOpenPlineSlice(
                            viewData,
                            !useSecondIndex,
                            false
                        );

                        var midpoint = PlineSeg.SegMidpoint(
                            slice.ViewData.UpdatedStart,
                            pline.Get(pline.NextWrappingIndex(slice.ViewData.StartIndex))
                        );

                        if (pointOnSlicePred(midpoint))
                        {
                            outputSlices.Add(slice);
                        }

                        break;
                    }

                    index = pline.NextWrappingIndex(index);
                }
            }
        }

        public static PrunedSlices<T> PruneSlices<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            ProcessForBooleanResult<T> booleanInfo,
            BooleanOp operation,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            return PruneSlicesImpl(pline1, pline2, booleanInfo, operation, false, posEqualEps);
        }

        private static PrunedSlices<T> PruneSlicesImpl<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            ProcessForBooleanResult<T> booleanInfo,
            BooleanOp operation,
            bool xorSecondPass,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var slicesRemaining = new List<BooleanPlineSlice<T>>();

            Func<Vector2<T>, bool> pointInPline1 = pt => pline1.WindingNumber(pt) != 0;
            Func<Vector2<T>, bool> pointInPline2 = pt => pline2.WindingNumber(pt) != 0;

            if (xorSecondPass)
            {
                SliceAtIntersects(pline1, booleanInfo, false, pointInPline2, slicesRemaining, posEqualEps);
            }
            else
            {
                switch (operation)
                {
                    case BooleanOp.Or:
                        SliceAtIntersects(pline1, booleanInfo, false, pt => !pointInPline2(pt), slicesRemaining, posEqualEps);
                        break;
                    case BooleanOp.And:
                        SliceAtIntersects(pline1, booleanInfo, false, pointInPline2, slicesRemaining, posEqualEps);
                        break;
                    case BooleanOp.Not:
                    case BooleanOp.Xor:
                        SliceAtIntersects(pline1, booleanInfo, false, pt => !pointInPline2(pt), slicesRemaining, posEqualEps);
                        break;
                }
            }

            int startOfPline2Slices = slicesRemaining.Count;

            if (xorSecondPass)
            {
                SliceAtIntersects(pline2, booleanInfo, true, pt => !pointInPline1(pt), slicesRemaining, posEqualEps);
            }
            else
            {
                switch (operation)
                {
                    case BooleanOp.Or:
                    case BooleanOp.Xor:
                        SliceAtIntersects(pline2, booleanInfo, true, pt => !pointInPline1(pt), slicesRemaining, posEqualEps);
                        break;
                    case BooleanOp.And:
                    case BooleanOp.Not:
                        SliceAtIntersects(pline2, booleanInfo, true, pointInPline1, slicesRemaining, posEqualEps);
                        break;
                }
            }

            int startOfPline1OverlappingSlices = slicesRemaining.Count;

            foreach (var s in booleanInfo.OverlappingSlices)
            {
                slicesRemaining.Add(BooleanPlineSlice<T>.FromOverlapping(pline2, s, s.OpposingDirections));
            }

            int startOfPline2OverlappingSlices = slicesRemaining.Count;

            foreach (var s in booleanInfo.OverlappingSlices)
            {
                slicesRemaining.Add(BooleanPlineSlice<T>.FromOverlapping(pline2, s, false));
            }

            bool setOpposingDirection = operation switch
            {
                BooleanOp.Or or BooleanOp.And => false,
                BooleanOp.Not or BooleanOp.Xor => true,
                _ => false
            };

            if (setOpposingDirection != booleanInfo.OpposingDirections())
            {
                for (int i = 0; i < startOfPline2Slices; i++)
                {
                    var s = slicesRemaining[i];
                    var updatedViewData = new PlineViewData<T>(
                        s.ViewData.StartIndex,
                        s.ViewData.EndIndexOffset,
                        s.ViewData.UpdatedStart,
                        s.ViewData.UpdatedEndBulge,
                        s.ViewData.EndPoint,
                        true
                    );
                    slicesRemaining[i] = new BooleanPlineSlice<T>(updatedViewData, s.SourceIsPline1, s.Overlapping);
                }
            }

            return new PrunedSlices<T>
            {
                SlicesRemaining = slicesRemaining,
                StartOfPline2Slices = startOfPline2Slices,
                StartOfPline1OverlappingSlices = startOfPline1OverlappingSlices,
                StartOfPline2OverlappingSlices = startOfPline2OverlappingSlices
            };
        }

        private static StaticAABB2DIndex<T> CreateAabbIndexForSlices<T>(List<BooleanPlineSlice<T>> slices)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var builder = new StaticAABB2DIndexBuilder<T>(slices.Count);

            foreach (var slice in slices)
            {
                var pt = slice.ViewData.InvertedDirection
                    ? slice.ViewData.EndPoint
                    : slice.ViewData.UpdatedStart.Pos();
                builder.Add(pt.X, pt.Y, pt.X, pt.Y);
            }

            return builder.Build();
        }

        public static List<BooleanResultPline<O, T>> StitchSlicesIntoClosedPolylines<O, T>(
            List<BooleanPlineSlice<T>> slices,
            IPlineSource<T> sourcePline1,
            IPlineSource<T> sourcePline2,
            IStitchSelector stitchSelector,
            T posEqualEps,
            T? collapsedAreaEps)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new List<BooleanResultPline<O, T>>();
            if (slices.Count == 0)
            {
                return result;
            }

            var aabbIndex = CreateAabbIndexForSlices(slices);

            var visitedSliceIdx = new bool[slices.Count];

            Action<O, List<BooleanPlineSlice<T>>> closePline = (pline, subslices) =>
            {
                Debug.Assert(pline.Get(0).Pos().FuzzyEqEps(pline.Get(pline.VertexCount - 1).Pos(), posEqualEps));

                if (pline.VertexCount < 3)
                {
                    return;
                }

                pline.Remove(pline.VertexCount - 1);
                pline.SetIsClosed(true);

                if (collapsedAreaEps.HasValue && T.Abs(pline.Area()) < collapsedAreaEps.Value)
                {
                    return;
                }

                result.Add(new BooleanResultPline<O, T>(pline, subslices));
            };

            var queryResults = new List<int>();
            var queryStack = new List<int>(8);

            Func<BooleanPlineSlice<T>, O> sliceToPline = s =>
            {
                if (s.SourceIsPline1)
                {
                    return PlineSourceExtensions.CreateFromRemoveRepeat<O, T>(s.View(sourcePline1), posEqualEps);
                }
                else
                {
                    return PlineSourceExtensions.CreateFromRemoveRepeat<O, T>(s.View(sourcePline2), posEqualEps);
                }
            };

            Action<BooleanPlineSlice<T>, O> stitchSliceOnto = (s, target) =>
            {
                if (s.SourceIsPline1)
                {
                    target.ExtendRemoveRepeat(s.View(sourcePline1), posEqualEps);
                }
                else
                {
                    target.ExtendRemoveRepeat(s.View(sourcePline2), posEqualEps);
                }
            };

            for (int i = 0; i < slices.Count; i++)
            {
                if (visitedSliceIdx[i])
                {
                    continue;
                }
                visitedSliceIdx[i] = true;

                var s = slices[i];
                var currentPline = sliceToPline(s);
                var subslices = new List<BooleanPlineSlice<T>> { s };

                int beginningSliceIdx = i;
                int currentSliceIdx = i;
                int loopCount = 0;
                int maxLoopCount = slices.Count;

                while (true)
                {
                    if (loopCount > maxLoopCount)
                    {
                        throw new InvalidOperationException("loopCount exceeded maxLoopCount while creating closed polylines from slices");
                    }
                    loopCount++;

                    queryResults.Clear();
                    aabbIndex.VisitQueryWithStack(
                        currentPline.Get(currentPline.VertexCount - 1).Pos().X - posEqualEps,
                        currentPline.Get(currentPline.VertexCount - 1).Pos().Y - posEqualEps,
                        currentPline.Get(currentPline.VertexCount - 1).Pos().X + posEqualEps,
                        currentPline.Get(currentPline.VertexCount - 1).Pos().Y + posEqualEps,
                        idx =>
                        {
                            if (idx == beginningSliceIdx || !visitedSliceIdx[idx])
                            {
                                queryResults.Add(idx);
                            }
                            return true;
                        },
                        queryStack
                    );

                    if (queryResults.Count == 0)
                    {
                        break;
                    }

                    var queryResultsSpan = CollectionsMarshal.AsSpan(queryResults);

                    var selected = stitchSelector.Select(currentSliceIdx, queryResultsSpan);
                    if (selected == null)
                    {
                        break;
                    }
                    else if (selected.Value == beginningSliceIdx)
                    {
                        closePline(currentPline, subslices);
                        break;
                    }
                    else
                    {
                        int connectedSliceIdx = selected.Value;
                        var nextSlice = slices[connectedSliceIdx];
                        currentPline.Remove(currentPline.VertexCount - 1);
                        stitchSliceOnto(nextSlice, currentPline);
                        visitedSliceIdx[connectedSliceIdx] = true;
                        subslices.Add(nextSlice);

                        currentSliceIdx = connectedSliceIdx;
                    }
                }
            }

            var compositeUserdata = new List<ulong>();
            compositeUserdata.AddRange(sourcePline1.UserDataValues);
            compositeUserdata.AddRange(sourcePline2.UserDataValues);

            foreach (var resultItem in result)
            {
                resultItem.Pline.SetUserDataValues(compositeUserdata);
            }

            return result;
        }

        public static BooleanResult<O, T> PolylineBoolean<O, T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            BooleanOp operation,
            PlineBooleanOptions<T> options)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (pline1.VertexCount < 2
                || !pline1.IsClosed
                || pline2.VertexCount < 2
                || !pline2.IsClosed)
            {
                return BooleanResult<O, T>.Empty(BooleanResultInfo.InvalidInput);
            }

            var pline1AabbIndex = options.Pline1AabbIndex ?? pline1.CreateApproxAabbIndex();

            var booleanInfo = ProcessForBoolean(pline1, pline2, pline1AabbIndex, options.PosEqualEps);

            Func<bool> isPline1InPline2 = () => pline2.WindingNumber(pline1.Get(0).Pos()) != 0;
            Func<bool> isPline2InPline1 = () => pline1.WindingNumber(pline2.Get(0).Pos()) != 0;

            T posEqualEps = options.PosEqualEps;
            T? collapsedAreaEps = options.CollapsedAreaEps;

            switch (operation)
            {
                case BooleanOp.Or:
                    {
                        if (booleanInfo.CompletelyOverlapping())
                        {
                            return BooleanResult<O, T>.FromWholePlines(
                                new[] { PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                Array.Empty<O>(),
                                BooleanResultInfo.Overlapping
                            );
                        }
                        else if (!booleanInfo.AnyIntersects())
                        {
                            if (isPline1InPline2())
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                    Array.Empty<O>(),
                                    BooleanResultInfo.Pline1InsidePline2
                                );
                            }
                            else if (isPline2InPline1())
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1) },
                                    Array.Empty<O>(),
                                    BooleanResultInfo.Pline2InsidePline1
                                );
                            }
                            else
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1), PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                    Array.Empty<O>(),
                                    BooleanResultInfo.Disjoint
                                );
                            }
                        }
                        else
                        {
                            var prunedSlices = PruneSlices(pline1, pline2, booleanInfo, BooleanOp.Or, posEqualEps);

                            var stitchSelector = OrAndStitchSelector.FromPrunedSlices(prunedSlices);

                            var remaining = StitchSlicesIntoClosedPolylines<O, T>(
                                prunedSlices.SlicesRemaining,
                                pline1,
                                pline2,
                                stitchSelector,
                                posEqualEps,
                                collapsedAreaEps
                            );

                            var posPlines = new List<BooleanResultPline<O, T>>();
                            var negPlines = new List<BooleanResultPline<O, T>>();

                            foreach (var resultPline in remaining)
                            {
                                var orientation = resultPline.Pline.Orientation();
                                if (orientation != booleanInfo.Pline2Orientation)
                                {
                                    negPlines.Add(resultPline);
                                }
                                else
                                {
                                    posPlines.Add(resultPline);
                                }
                            }

                            return new BooleanResult<O, T>(posPlines, negPlines, BooleanResultInfo.Intersected);
                        }
                    }

                case BooleanOp.And:
                    {
                        if (booleanInfo.CompletelyOverlapping())
                        {
                            return BooleanResult<O, T>.FromWholePlines(
                                new[] { PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                Array.Empty<O>(),
                                BooleanResultInfo.Overlapping
                            );
                        }
                        else if (!booleanInfo.AnyIntersects())
                        {
                            if (isPline1InPline2())
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1) },
                                    Array.Empty<O>(),
                                    BooleanResultInfo.Pline1InsidePline2
                                );
                            }
                            else if (isPline2InPline1())
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                    Array.Empty<O>(),
                                    BooleanResultInfo.Pline2InsidePline1
                                );
                            }
                            else
                            {
                                return BooleanResult<O, T>.Empty(BooleanResultInfo.Disjoint);
                            }
                        }
                        else
                        {
                            var prunedSlices = PruneSlices(pline1, pline2, booleanInfo, BooleanOp.And, posEqualEps);

                            var stitchSelector = OrAndStitchSelector.FromPrunedSlices(prunedSlices);
                            var posPlines = StitchSlicesIntoClosedPolylines<O, T>(
                                prunedSlices.SlicesRemaining,
                                pline1,
                                pline2,
                                stitchSelector,
                                posEqualEps,
                                collapsedAreaEps
                            );

                            return new BooleanResult<O, T>(posPlines, new List<BooleanResultPline<O, T>>(), BooleanResultInfo.Intersected);
                        }
                    }

                case BooleanOp.Not:
                    {
                        if (booleanInfo.CompletelyOverlapping())
                        {
                            return BooleanResult<O, T>.Empty(BooleanResultInfo.Overlapping);
                        }
                        else if (!booleanInfo.AnyIntersects())
                        {
                            if (isPline1InPline2())
                            {
                                return BooleanResult<O, T>.Empty(BooleanResultInfo.Pline1InsidePline2);
                            }
                            else if (isPline2InPline1())
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1) },
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                    BooleanResultInfo.Pline2InsidePline1
                                );
                            }
                            else
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1) },
                                    Array.Empty<O>(),
                                    BooleanResultInfo.Disjoint
                                );
                            }
                        }
                        else
                        {
                            var prunedSlices = PruneSlices(pline1, pline2, booleanInfo, BooleanOp.Not, posEqualEps);

                            var stitchSelector = NotXorStitchSelector.FromPrunedSlices(prunedSlices);

                            var posPlines = StitchSlicesIntoClosedPolylines<O, T>(
                                prunedSlices.SlicesRemaining,
                                pline1,
                                pline2,
                                stitchSelector,
                                posEqualEps,
                                collapsedAreaEps
                            );

                            return new BooleanResult<O, T>(posPlines, new List<BooleanResultPline<O, T>>(), BooleanResultInfo.Intersected);
                        }
                    }

                case BooleanOp.Xor:
                    {
                        if (booleanInfo.CompletelyOverlapping())
                        {
                            return BooleanResult<O, T>.Empty(BooleanResultInfo.Overlapping);
                        }
                        else if (!booleanInfo.AnyIntersects())
                        {
                            if (isPline1InPline2())
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1) },
                                    BooleanResultInfo.Pline1InsidePline2
                                );
                            }
                            else if (isPline2InPline1())
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1) },
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                    BooleanResultInfo.Pline2InsidePline1
                                );
                            }
                            else
                            {
                                return BooleanResult<O, T>.FromWholePlines(
                                    new[] { PlineSourceExtensions.CreateFrom<O, T>(pline1), PlineSourceExtensions.CreateFrom<O, T>(pline2) },
                                    Array.Empty<O>(),
                                    BooleanResultInfo.Disjoint
                                );
                            }
                        }
                        else
                        {
                            var prunedSlices1 = PruneSlices(pline1, pline2, booleanInfo, BooleanOp.Not, posEqualEps);

                            var stitchSelector1 = NotXorStitchSelector.FromPrunedSlices(prunedSlices1);
                            var remaining1 = StitchSlicesIntoClosedPolylines<O, T>(
                                prunedSlices1.SlicesRemaining,
                                pline1,
                                pline2,
                                stitchSelector1,
                                posEqualEps,
                                collapsedAreaEps
                            );

                            var prunedSlices2 = PruneSlicesImpl(
                                pline1,
                                pline2,
                                booleanInfo,
                                BooleanOp.Xor,
                                true,
                                posEqualEps
                            );

                            var stitchSelector2 = NotXorStitchSelector.FromPrunedSlices(prunedSlices2);
                            var remaining2 = StitchSlicesIntoClosedPolylines<O, T>(
                                prunedSlices2.SlicesRemaining,
                                pline1,
                                pline2,
                                stitchSelector2,
                                posEqualEps,
                                collapsedAreaEps
                            );

                            remaining1.AddRange(remaining2);
                            return new BooleanResult<O, T>(remaining1, new List<BooleanResultPline<O, T>>(), BooleanResultInfo.Intersected);
                        }
                    }

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }
    }
}