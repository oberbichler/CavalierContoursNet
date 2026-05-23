using System;
using System.Collections.Generic;
using System.Numerics;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Spatial;

namespace CavalierContours.Shape
{
    public class OffsetLoop<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public int ParentLoopIdx { get; set; }
        public IndexedPolyline<T> IndexedPline { get; set; }

        public OffsetLoop() : this(0, new IndexedPolyline<T>(new Polyline<T>()))
        {
        }

        public OffsetLoop(int parentLoopIdx, IndexedPolyline<T> indexedPline)
        {
            ParentLoopIdx = parentLoopIdx;
            IndexedPline = indexedPline;
        }
    }

    public class IndexedPolyline<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public Polyline<T> Polyline { get; set; }
        public StaticAABB2DIndex<T> SpatialIndex { get; set; }

        public IndexedPolyline(Polyline<T> polyline)
        {
            Polyline = polyline;
            SpatialIndex = polyline.CreateApproxAabbIndex();
        }

        public List<Polyline<T>> ParallelOffsetForShape(T offset, ShapeOffsetOptions<T> options)
        {
            var opts = new PlineOffsetOptions<T>
            {
                AabbIndex = SpatialIndex,
                HandleSelfIntersects = false,
                PosEqualEps = options.PosEqualEps,
                SliceJoinEps = options.SliceJoinEps,
                OffsetDistEps = options.OffsetDistEps
            };

            return PlineOffset.ParallelOffset<Polyline<T>, T>(Polyline, offset, opts);
        }
    }

    public class ShapeOffsetOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public T PosEqualEps { get; set; }
        public T OffsetDistEps { get; set; }
        public T SliceJoinEps { get; set; }

        public ShapeOffsetOptions()
        {
            PosEqualEps = T.CreateChecked(1e-5);
            OffsetDistEps = T.CreateChecked(1e-4);
            SliceJoinEps = T.CreateChecked(1e-4);
        }

        public ShapeOffsetOptions(T posEqualEps, T offsetDistEps, T sliceJoinEps)
        {
            PosEqualEps = posEqualEps;
            OffsetDistEps = offsetDistEps;
            SliceJoinEps = sliceJoinEps;
        }
    }

    public class SlicePointSet<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public int LoopIdx1 { get; set; }
        public int LoopIdx2 { get; set; }
        public List<PlineBasicIntersect<T>> SlicePoints { get; set; }

        public SlicePointSet(int loopIdx1, int loopIdx2, List<PlineBasicIntersect<T>> slicePoints)
        {
            LoopIdx1 = loopIdx1;
            LoopIdx2 = loopIdx2;
            SlicePoints = slicePoints;
        }
    }

    public readonly struct DissectedSlice<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public readonly int SourceIdx;
        public readonly PlineViewData<T> VData;

        public DissectedSlice(int sourceIdx, PlineViewData<T> vData)
        {
            SourceIdx = sourceIdx;
            VData = vData;
        }
    }

    public class Shape<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        public List<IndexedPolyline<T>> CcwPlines { get; }
        public List<IndexedPolyline<T>> CwPlines { get; }
        public StaticAABB2DIndex<T> PlinesIndex { get; }

        public Shape(List<IndexedPolyline<T>> ccwPlines, List<IndexedPolyline<T>> cwPlines, StaticAABB2DIndex<T> plinesIndex)
        {
            CcwPlines = ccwPlines;
            CwPlines = cwPlines;
            PlinesIndex = plinesIndex;
        }

        public static Shape<T> FromPlines(IEnumerable<Polyline<T>> plines)
        {
            var ccwPlines = new List<IndexedPolyline<T>>();
            var cwPlines = new List<IndexedPolyline<T>>();

            foreach (var pl in plines)
            {
                if (pl.VertexCount > 1)
                {
                    if (pl.Orientation() == PlineOrientation.CounterClockwise)
                    {
                        ccwPlines.Add(new IndexedPolyline<T>(pl));
                    }
                    else
                    {
                        cwPlines.Add(new IndexedPolyline<T>(pl));
                    }
                }
            }

            var builder = new StaticAABB2DIndexBuilder<T>(ccwPlines.Count + cwPlines.Count);

            void AddAllBounds(List<IndexedPolyline<T>> list)
            {
                foreach (var pline in list)
                {
                    var bounds = pline.SpatialIndex.Bounds;
                    if (bounds == null)
                    {
                        throw new InvalidOperationException("expect non-empty polyline");
                    }
                    builder.Add(bounds.Value.MinX, bounds.Value.MinY, bounds.Value.MaxX, bounds.Value.MaxY);
                }
            }

            AddAllBounds(ccwPlines);
            AddAllBounds(cwPlines);

            var plinesIndex = builder.Build();

            return new Shape<T>(ccwPlines, cwPlines, plinesIndex);
        }

        public static Shape<T> Empty()
        {
            return new Shape<T>(
                new List<IndexedPolyline<T>>(),
                new List<IndexedPolyline<T>>(),
                new StaticAABB2DIndexBuilder<T>(0).Build()
            );
        }

        public Shape<T> ParallelOffset(T offset, ShapeOffsetOptions<T> options)
        {
            var (ccwOffsetLoops, cwOffsetLoops, offsetLoopsIndex) = CreateOffsetLoopsWithIndex(offset, options);

            if (ccwOffsetLoops.Count == 0 && cwOffsetLoops.Count == 0)
            {
                return Empty();
            }

            var slicePointSets = FindIntersectsBetweenOffsetLoops(
                ccwOffsetLoops,
                cwOffsetLoops,
                offsetLoopsIndex,
                options.PosEqualEps
            );

            var slicesData = CreateValidSlicesFromIntersects(
                ccwOffsetLoops,
                cwOffsetLoops,
                slicePointSets,
                offset,
                options
            );

            return StitchSlicesTogether(
                slicesData,
                ccwOffsetLoops,
                cwOffsetLoops,
                options.PosEqualEps,
                options.SliceJoinEps
            );
        }

        public (List<OffsetLoop<T>> CcwOffsetLoops, List<OffsetLoop<T>> CwOffsetLoops, StaticAABB2DIndex<T> OffsetLoopsIndex) CreateOffsetLoopsWithIndex(
            T offset,
            ShapeOffsetOptions<T> options)
        {
            var ccwOffsetLoops = new List<OffsetLoop<T>>();
            var cwOffsetLoops = new List<OffsetLoop<T>>();
            int parentIdx = 0;

            foreach (var pline in CcwPlines)
            {
                foreach (var offsetPline in pline.ParallelOffsetForShape(offset, options))
                {
                    T area = offsetPline.Area();
                    if (offset > T.Zero && area < T.Zero)
                    {
                        continue;
                    }

                    var offsetLoop = new OffsetLoop<T>(parentIdx, new IndexedPolyline<T>(offsetPline));

                    if (area < T.Zero)
                    {
                        cwOffsetLoops.Add(offsetLoop);
                    }
                    else
                    {
                        ccwOffsetLoops.Add(offsetLoop);
                    }
                }
                parentIdx++;
            }

            foreach (var pline in CwPlines)
            {
                foreach (var offsetPline in pline.ParallelOffsetForShape(offset, options))
                {
                    T area = offsetPline.Area();
                    if (offset < T.Zero && area > T.Zero)
                    {
                        continue;
                    }

                    var offsetLoop = new OffsetLoop<T>(parentIdx, new IndexedPolyline<T>(offsetPline));

                    if (area < T.Zero)
                    {
                        cwOffsetLoops.Add(offsetLoop);
                    }
                    else
                    {
                        ccwOffsetLoops.Add(offsetLoop);
                    }
                }
                parentIdx++;
            }

            var builder = new StaticAABB2DIndexBuilder<T>(ccwOffsetLoops.Count + cwOffsetLoops.Count);

            void AddAllBounds(List<OffsetLoop<T>> list)
            {
                foreach (var l in list)
                {
                    var bounds = l.IndexedPline.SpatialIndex.Bounds;
                    if (bounds == null)
                    {
                        throw new InvalidOperationException("expect non-empty polyline");
                    }
                    builder.Add(bounds.Value.MinX, bounds.Value.MinY, bounds.Value.MaxX, bounds.Value.MaxY);
                }
            }

            AddAllBounds(ccwOffsetLoops);
            AddAllBounds(cwOffsetLoops);

            var offsetLoopsIndex = builder.Build();

            return (ccwOffsetLoops, cwOffsetLoops, offsetLoopsIndex);
        }

        private struct CollectVisitor : IQueryVisitor
        {
            public List<int> Results;
            public bool Visit(int indexPos)
            {
                Results.Add(indexPos);
                return true;
            }
        }

        public List<SlicePointSet<T>> FindIntersectsBetweenOffsetLoops(
            List<OffsetLoop<T>> ccwOffsetLoops,
            List<OffsetLoop<T>> cwOffsetLoops,
            StaticAABB2DIndex<T> offsetLoopsIndex,
            T posEqualEps)
        {
            int offsetLoopCount = ccwOffsetLoops.Count + cwOffsetLoops.Count;
            var slicePointSets = new List<SlicePointSet<T>>();
            var visitedLoopPairs = new HashSet<ulong>();
            var queryStack = new List<int>();
            var queryResults = new List<int>();

            for (int i = 0; i < offsetLoopCount; i++)
            {
                var loop1 = GetLoop(i, ccwOffsetLoops, cwOffsetLoops);
                var spatialIdx1 = loop1.IndexedPline.SpatialIndex;
                var bounds = spatialIdx1.Bounds;
                if (bounds == null)
                {
                    throw new InvalidOperationException("expect non-empty polyline");
                }

                queryResults.Clear();
                var collectVisitor = new CollectVisitor { Results = queryResults };
                offsetLoopsIndex.VisitQueryWithStack(
                    bounds.Value.MinX,
                    bounds.Value.MinY,
                    bounds.Value.MaxX,
                    bounds.Value.MaxY,
                    ref collectVisitor,
                    queryStack
                );

                for (int r = 0; r < queryResults.Count; r++)
                {
                    int j = queryResults[r];
                    if (i == j)
                    {
                        continue;
                    }

                    ulong reverseKey = ((ulong)(uint)j << 32) | (uint)i;
                    if (visitedLoopPairs.Contains(reverseKey))
                    {
                        continue;
                    }

                    ulong key = ((ulong)(uint)i << 32) | (uint)j;
                    visitedLoopPairs.Add(key);

                    var loop2 = GetLoop(j, ccwOffsetLoops, cwOffsetLoops);

                    var intrsOpts = new FindIntersectsOptions<T>
                    {
                        Pline1AabbIndex = spatialIdx1,
                        PosEqualEps = posEqualEps
                    };

                    var intersects = PlineIntersects.FindIntersects(
                        loop1.IndexedPline.Polyline,
                        loop2.IndexedPline.Polyline,
                        intrsOpts
                    );

                    if (intersects.BasicIntersects.Count == 0 && intersects.OverlappingIntersects.Count == 0)
                    {
                        continue;
                    }

                    var slicePoints = new List<PlineBasicIntersect<T>>();

                    foreach (var intr in intersects.BasicIntersects)
                    {
                        slicePoints.Add(intr);
                    }

                    foreach (var overlapIntr in intersects.OverlappingIntersects)
                    {
                        int startIndex1 = overlapIntr.StartIndex1;
                        int startIndex2 = overlapIntr.StartIndex2;
                        slicePoints.Add(new PlineBasicIntersect<T>(startIndex1, startIndex2, overlapIntr.Point1));
                        slicePoints.Add(new PlineBasicIntersect<T>(startIndex1, startIndex2, overlapIntr.Point2));
                    }

                    slicePointSets.Add(new SlicePointSet<T>(i, j, slicePoints));
                }
            }

            return slicePointSets;
        }

        private readonly struct DissectionPoint
        {
            public readonly int SegIdx;
            public readonly Vector2<T> Pos;

            public DissectionPoint(int segIdx, Vector2<T> pos)
            {
                SegIdx = segIdx;
                Pos = pos;
            }
        }

        public List<DissectedSlice<T>> CreateValidSlicesFromIntersects(
            List<OffsetLoop<T>> ccwOffsetLoops,
            List<OffsetLoop<T>> cwOffsetLoops,
            List<SlicePointSet<T>> slicePointSets,
            T offset,
            ShapeOffsetOptions<T> options)
        {
            int offsetLoopCount = ccwOffsetLoops.Count + cwOffsetLoops.Count;
            T posEqualEps = options.PosEqualEps;
            T offsetDistEps = options.OffsetDistEps;

            var slicePointsLookup = new Dictionary<int, List<int>>();
            for (int setIdx = 0; setIdx < slicePointSets.Count; setIdx++)
            {
                var set = slicePointSets[setIdx];
                if (!slicePointsLookup.TryGetValue(set.LoopIdx1, out var list1))
                {
                    list1 = new List<int>();
                    slicePointsLookup[set.LoopIdx1] = list1;
                }
                list1.Add(setIdx);

                if (!slicePointsLookup.TryGetValue(set.LoopIdx2, out var list2))
                {
                    list2 = new List<int>();
                    slicePointsLookup[set.LoopIdx2] = list2;
                }
                list2.Add(setIdx);
            }

            PlineViewData<T>? CreateSlice(in DissectionPoint pt1, in DissectionPoint pt2, Polyline<T> offsetLoop)
            {
                return PlineViewData<T>.FromSlicePoints(
                    offsetLoop,
                    pt1.Pos,
                    pt1.SegIdx,
                    pt2.Pos,
                    pt2.SegIdx,
                    posEqualEps
                );
            }

            bool IsSliceValid(in PlineViewData<T> vData, Polyline<T> offsetLoop, int parentIdx, List<int> qStack)
            {
                var sliceView = vData.View(offsetLoop);
                int vertexCount = sliceView.VertexCount;

                Vector2<T> midpoint1;
                Vector2<T>? midpoint2 = null;

                if (vertexCount > 3)
                {
                    midpoint1 = PlineSeg.SegMidpoint(sliceView.Get(1), sliceView.Get(2));
                }
                else if (vertexCount == 3)
                {
                    midpoint1 = PlineSeg.SegMidpoint(sliceView.Get(0), sliceView.Get(1));
                    midpoint2 = PlineSeg.SegMidpoint(sliceView.Get(1), sliceView.Get(2));
                }
                else
                {
                    midpoint1 = PlineSeg.SegMidpoint(sliceView.Get(0), sliceView.Get(1));
                }

                int totalPlines = CcwPlines.Count + CwPlines.Count;
                for (int inputLoopIdx = 0; inputLoopIdx < totalPlines; inputLoopIdx++)
                {
                    if (inputLoopIdx == parentIdx)
                    {
                        continue;
                    }

                    IndexedPolyline<T> parentLoop;
                    if (inputLoopIdx < CcwPlines.Count)
                    {
                        parentLoop = CcwPlines[inputLoopIdx];
                    }
                    else
                    {
                        parentLoop = CwPlines[inputLoopIdx - CcwPlines.Count];
                    }

                    if (!PlineOffset.PointValidForOffset(
                        parentLoop.Polyline,
                        offset,
                        parentLoop.SpatialIndex,
                        midpoint1,
                        qStack,
                        posEqualEps,
                        offsetDistEps))
                    {
                        return false;
                    }

                    if (midpoint2.HasValue)
                    {
                        if (!PlineOffset.PointValidForOffset(
                            parentLoop.Polyline,
                            offset,
                            parentLoop.SpatialIndex,
                            midpoint2.Value,
                            qStack,
                            posEqualEps,
                            offsetDistEps))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            var sortedIntrs = new List<DissectionPoint>();
            var slicesData = new List<DissectedSlice<T>>();
            var queryStack = new List<int>();

            for (int loopIdx = 0; loopIdx < offsetLoopCount; loopIdx++)
            {
                sortedIntrs.Clear();
                var currLoop = GetLoop(loopIdx, ccwOffsetLoops, cwOffsetLoops);

                if (slicePointsLookup.TryGetValue(loopIdx, out var slicePointSetIdxs))
                {
                    foreach (int setIdx in slicePointSetIdxs)
                    {
                        var set = slicePointSets[setIdx];
                        bool loopIsFirstIndex = set.LoopIdx1 == loopIdx;

                        foreach (var intrPt in set.SlicePoints)
                        {
                            int segIdx = loopIsFirstIndex ? intrPt.StartIndex1 : intrPt.StartIndex2;
                            sortedIntrs.Add(new DissectionPoint(segIdx, intrPt.Point));
                        }
                    }

                    sortedIntrs.Sort((a, b) =>
                    {
                        int cmp = a.SegIdx.CompareTo(b.SegIdx);
                        if (cmp != 0)
                        {
                            return cmp;
                        }

                        var segStart = currLoop.IndexedPline.Polyline.Get(a.SegIdx).Pos();
                        T dist1 = BaseMath.DistSquared(a.Pos, segStart);
                        T dist2 = BaseMath.DistSquared(b.Pos, segStart);
                        return dist1.CompareTo(dist2);
                    });

                    if (sortedIntrs.Count == 1)
                    {
                        var vData = PlineViewData<T>.FromEntirePline(currLoop.IndexedPline.Polyline);
                        if (IsSliceValid(vData, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                        {
                            slicesData.Add(new DissectedSlice<T>(loopIdx, vData));
                        }
                    }
                    else
                    {
                        for (int w = 0; w < sortedIntrs.Count - 1; w++)
                        {
                            var pt1 = sortedIntrs[w];
                            var pt2 = sortedIntrs[w + 1];
                            var vData = CreateSlice(pt1, pt2, currLoop.IndexedPline.Polyline);
                            if (vData != null && IsSliceValid(vData.Value, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                            {
                                slicesData.Add(new DissectedSlice<T>(loopIdx, vData.Value));
                            }
                        }

                        var lastPt = sortedIntrs[^1];
                        var firstPt = sortedIntrs[0];
                        var lastToStartVData = CreateSlice(lastPt, firstPt, currLoop.IndexedPline.Polyline);
                        if (lastToStartVData != null && IsSliceValid(lastToStartVData.Value, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                        {
                            slicesData.Add(new DissectedSlice<T>(loopIdx, lastToStartVData.Value));
                        }
                    }
                }
                else
                {
                    var vData = PlineViewData<T>.FromEntirePline(currLoop.IndexedPline.Polyline);
                    if (IsSliceValid(vData, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                    {
                        slicesData.Add(new DissectedSlice<T>(loopIdx, vData));
                    }
                }
            }

            return slicesData;
        }

        private struct StitchVisitor : IQueryVisitor
        {
            public List<int> QueryResults;
            public bool[] VisitedSlicesIdxs;

            public bool Visit(int indexPos)
            {
                if (!VisitedSlicesIdxs[indexPos])
                {
                    QueryResults.Add(indexPos);
                }
                return true;
            }
        }

        public Shape<T> StitchSlicesTogether(
            List<DissectedSlice<T>> slicesData,
            List<OffsetLoop<T>> ccwOffsetLoops,
            List<OffsetLoop<T>> cwOffsetLoops,
            T posEqualEps,
            T sliceJoinEps)
        {
            if (slicesData.Count == 0)
            {
                return Empty();
            }

            var ccwPlinesResult = new List<IndexedPolyline<T>>();
            var cwPlinesResult = new List<IndexedPolyline<T>>();

            var builder = new StaticAABB2DIndexBuilder<T>(slicesData.Count);
            for (int i = 0; i < slicesData.Count; i++)
            {
                var slice = slicesData[i];
                var startPoint = slice.VData.UpdatedStart.Pos();
                builder.Add(
                    startPoint.X - sliceJoinEps,
                    startPoint.Y - sliceJoinEps,
                    startPoint.X + sliceJoinEps,
                    startPoint.Y + sliceJoinEps
                );
            }
            var sliceStartsAabbIndex = builder.Build();

            var visitedSlicesIdxs = new bool[slicesData.Count];
            var queryResults = new List<int>();
            var queryStack = new List<int>();

            for (int sliceIdx = 0; sliceIdx < slicesData.Count; sliceIdx++)
            {
                if (visitedSlicesIdxs[sliceIdx])
                {
                    continue;
                }
                visitedSlicesIdxs[sliceIdx] = true;

                int currentIndex = sliceIdx;
                int loopCount = 0;
                int maxLoopCount = slicesData.Count;
                var currentPline = new Polyline<T>();

                while (true)
                {
                    if (loopCount > maxLoopCount)
                    {
                        throw new InvalidOperationException("loopCount exceeded maxLoopCount while stitching slices together");
                    }
                    loopCount++;

                    var currSlice = slicesData[currentIndex];
                    var sourceLoop = GetLoop(currSlice.SourceIdx, ccwOffsetLoops, cwOffsetLoops);
                    var sliceView = currSlice.VData.View(sourceLoop.IndexedPline.Polyline);
                    var sliceUserdataValues = sliceView.UserDataValues;
                    currentPline.ExtendRemoveRepeat(sliceView, posEqualEps);
                    currentPline.AddUserDataValues(sliceUserdataValues);

                    queryResults.Clear();
                    var sliceEndPoint = currSlice.VData.EndPoint;
                    var stitchVisitor = new StitchVisitor
                    {
                        QueryResults = queryResults,
                        VisitedSlicesIdxs = visitedSlicesIdxs
                    };

                    sliceStartsAabbIndex.VisitQueryWithStack(
                        sliceEndPoint.X - sliceJoinEps,
                        sliceEndPoint.Y - sliceJoinEps,
                        sliceEndPoint.X + sliceJoinEps,
                        sliceEndPoint.Y + sliceJoinEps,
                        ref stitchVisitor,
                        queryStack
                    );

                    if (queryResults.Count == 0)
                    {
                        if (currentPline.VertexCount > 2)
                        {
                            currentPline.RemoveAt(currentPline.VertexCount - 1);
                            currentPline.SetIsClosed(true);
                        }
                        bool isCcw = currentPline.Orientation() == PlineOrientation.CounterClockwise;
                        if (isCcw)
                        {
                            ccwPlinesResult.Add(new IndexedPolyline<T>(currentPline));
                        }
                        else
                        {
                            cwPlinesResult.Add(new IndexedPolyline<T>(currentPline));
                        }
                        break;
                    }

                    int nextIndex = -1;
                    for (int r = 0; r < queryResults.Count; r++)
                    {
                        int idx = queryResults[r];
                        if (slicesData[idx].SourceIdx == currSlice.SourceIdx)
                        {
                            nextIndex = idx;
                            break;
                        }
                    }

                    if (nextIndex == -1)
                    {
                        nextIndex = queryResults[0];
                    }

                    currentIndex = nextIndex;
                    visitedSlicesIdxs[currentIndex] = true;
                }
            }

            var plinesIndexBuilder = new StaticAABB2DIndexBuilder<T>(ccwPlinesResult.Count + cwPlinesResult.Count);

            void AddAllBounds(List<IndexedPolyline<T>> plines)
            {
                foreach (var pline in plines)
                {
                    var bounds = pline.SpatialIndex.Bounds;
                    if (bounds == null)
                    {
                        throw new InvalidOperationException("expect non-empty polyline");
                    }
                    plinesIndexBuilder.Add(bounds.Value.MinX, bounds.Value.MinY, bounds.Value.MaxX, bounds.Value.MaxY);
                }
            }

            AddAllBounds(ccwPlinesResult);
            AddAllBounds(cwPlinesResult);

            var plinesIndex = plinesIndexBuilder.Build();

            return new Shape<T>(ccwPlinesResult, cwPlinesResult, plinesIndex);
        }

        private static OffsetLoop<T> GetLoop(int i, List<OffsetLoop<T>> s1, List<OffsetLoop<T>> s2)
        {
            if (i < s1.Count)
            {
                return s1[i];
            }
            else
            {
                return s2[i - s1.Count];
            }
        }
    }
}