using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    public static class PlineIntersects
    {
        public static bool VisitLocalSelfIntersects<T>(
            IPlineSource<T> polyline,
            IPlineIntersectVisitor<T> visitor,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = polyline.VertexCount;
            if (vc < 2)
            {
                return true;
            }

            if (vc == 2)
            {
                if (polyline.IsClosed)
                {
                    if (polyline.Get(0).Bulge.FuzzyEq(-polyline.Get(1).Bulge))
                    {
                        return visitor.VisitOverlappingIntr(new PlineOverlappingIntersect<T>(
                            0,
                            1,
                            polyline.Get(0).Pos(),
                            polyline.Get(1).Pos()
                        ));
                    }
                }
                return true;
            }

            bool VisitIndexes(int i, int j, int k)
            {
                var v1 = polyline.Get(i);
                var v2 = polyline.Get(j);
                var v3 = polyline.Get(k);

                if (v1.Pos().FuzzyEqEps(v2.Pos(), posEqualEps))
                {
                    if (!visitor.VisitOverlappingIntr(new PlineOverlappingIntersect<T>(
                        i,
                        j,
                        v1.Pos(),
                        v2.Pos()
                    )))
                    {
                        return false;
                    }
                }
                else
                {
                    var intr = PlineSegIntersection.Intersect(v1, v2, v2, v3, posEqualEps);
                    switch (intr.Kind)
                    {
                        case PlineSegIntrKind.NoIntersect:
                            break;
                        case PlineSegIntrKind.TangentIntersect:
                        case PlineSegIntrKind.OneIntersect:
                            if (!intr.Point1.FuzzyEqEps(v2.Pos(), posEqualEps))
                            {
                                if (!visitor.VisitBasicIntr(new PlineBasicIntersect<T>(i, j, intr.Point1)))
                                {
                                    return false;
                                }
                            }
                            break;
                        case PlineSegIntrKind.TwoIntersects:
                            if (!intr.Point1.FuzzyEqEps(v2.Pos(), posEqualEps))
                            {
                                if (!visitor.VisitBasicIntr(new PlineBasicIntersect<T>(i, j, intr.Point1)))
                                {
                                    return false;
                                }
                            }

                            if (!intr.Point2.FuzzyEqEps(v2.Pos(), posEqualEps))
                            {
                                if (!visitor.VisitBasicIntr(new PlineBasicIntersect<T>(i, j, intr.Point2)))
                                {
                                    return false;
                                }
                            }
                            break;
                        case PlineSegIntrKind.OverlappingLines:
                        case PlineSegIntrKind.OverlappingArcs:
                            if (!visitor.VisitOverlappingIntr(new PlineOverlappingIntersect<T>(i, j, intr.Point1, intr.Point2)))
                            {
                                return false;
                            }
                            break;
                    }
                }

                return true;
            }

            for (int i = 2; i < vc; i++)
            {
                if (!VisitIndexes(i - 2, i - 1, i))
                {
                    return false;
                }
            }

            if (polyline.IsClosed)
            {
                if (!VisitIndexes(vc - 2, vc - 1, 0))
                {
                    return false;
                }
                if (!VisitIndexes(vc - 1, 0, 1))
                {
                    return false;
                }
            }

            return true;
        }

        private struct GlobalSelfIntersectsVisitor<T> : IQueryVisitor
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            private readonly IPlineSource<T> _polyline;
            private readonly IPlineIntersectVisitor<T> _visitor;
            private readonly T _posEqualEps;
            private readonly int _i;
            private readonly int _j;
            private readonly PlineVertex<T> _v1;
            private readonly PlineVertex<T> _v2;
            private readonly HashSet<(int, int)> _visitedPairs;
            public bool Cf;

            public GlobalSelfIntersectsVisitor(
                IPlineSource<T> polyline,
                IPlineIntersectVisitor<T> visitor,
                T posEqualEps,
                int i,
                int j,
                PlineVertex<T> v1,
                PlineVertex<T> v2,
                HashSet<(int, int)> visitedPairs)
            {
                _polyline = polyline;
                _visitor = visitor;
                _posEqualEps = posEqualEps;
                _i = i;
                _j = j;
                _v1 = v1;
                _v2 = v2;
                _visitedPairs = visitedPairs;
                Cf = true;
            }

            public bool Visit(int hitI)
            {
                int hitJ = _polyline.NextWrappingIndex(hitI);
                if (_i == hitI || _i == hitJ || _j == hitI || _j == hitJ)
                {
                    return true;
                }

                if (_visitedPairs.Contains((hitI, _i)))
                {
                    return true;
                }

                _visitedPairs.Add((_i, hitI));

                var u1 = _polyline.Get(hitI);
                var u2 = _polyline.Get(hitJ);

                var v2Local = _v2;
                var posEqualEpsLocal = _posEqualEps;
                bool SkipIntrAtEnd(Vector2<T> intr)
                {
                    return v2Local.Pos().FuzzyEqEps(intr, posEqualEpsLocal)
                        && u2.Pos().FuzzyEqEps(intr, posEqualEpsLocal);
                }

                var intrResult = PlineSegIntersection.Intersect(_v1, _v2, u1, u2, _posEqualEps);
                switch (intrResult.Kind)
                {
                    case PlineSegIntrKind.NoIntersect:
                        break;
                    case PlineSegIntrKind.TangentIntersect:
                    case PlineSegIntrKind.OneIntersect:
                        if (!SkipIntrAtEnd(intrResult.Point1))
                        {
                            if (!_visitor.VisitBasicIntr(new PlineBasicIntersect<T>(_i, hitI, intrResult.Point1)))
                            {
                                Cf = false;
                                return false;
                            }
                        }
                        break;
                    case PlineSegIntrKind.TwoIntersects:
                        if (!SkipIntrAtEnd(intrResult.Point1))
                        {
                            if (!_visitor.VisitBasicIntr(new PlineBasicIntersect<T>(_i, hitI, intrResult.Point1)))
                            {
                                Cf = false;
                                return false;
                            }
                        }
                        if (!SkipIntrAtEnd(intrResult.Point2))
                        {
                            if (!_visitor.VisitBasicIntr(new PlineBasicIntersect<T>(_i, hitI, intrResult.Point2)))
                            {
                                Cf = false;
                                return false;
                            }
                        }
                        break;
                    case PlineSegIntrKind.OverlappingLines:
                    case PlineSegIntrKind.OverlappingArcs:
                        if (!SkipIntrAtEnd(intrResult.Point1))
                        {
                            if (!_visitor.VisitOverlappingIntr(new PlineOverlappingIntersect<T>(_i, hitI, intrResult.Point1, intrResult.Point2)))
                            {
                                Cf = false;
                                return false;
                            }
                        }
                        break;
                }

                return true;
            }
        }

        public static bool VisitGlobalSelfIntersects<T>(
            IPlineSource<T> polyline,
            StaticAABB2DIndex<T> aabbIndex,
            IPlineIntersectVisitor<T> visitor,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = polyline.VertexCount;
            if (vc < 3)
            {
                return true;
            }

            var visitedPairs = new HashSet<(int, int)>(vc);
            var queryStack = new List<int>(8);

            bool cf = true;
            var itemIndices = aabbIndex.ItemIndices;
            var itemBoxes = aabbIndex.ItemBoxes;

            for (int index = 0; index < itemIndices.Length; index++)
            {
                int i = itemIndices[index];
                var aabb = itemBoxes[index];

                int j = polyline.NextWrappingIndex(i);
                var v1 = polyline.Get(i);
                var v2 = polyline.Get(j);

                var queryVisitor = new GlobalSelfIntersectsVisitor<T>(
                    polyline,
                    visitor,
                    posEqualEps,
                    i,
                    j,
                    v1,
                    v2,
                    visitedPairs
                );

                aabbIndex.VisitQueryWithStack(
                    aabb.MinX - posEqualEps,
                    aabb.MinY - posEqualEps,
                    aabb.MaxX + posEqualEps,
                    aabb.MaxY + posEqualEps,
                    ref queryVisitor,
                    queryStack
                );

                if (!queryVisitor.Cf)
                {
                    cf = false;
                    break;
                }
            }

            return cf;
        }

        private class BasicSelfIntersectsVisitor<T> : IPlineIntersectVisitor<T>
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            public readonly List<PlineBasicIntersect<T>> Intrs = new();
            private readonly bool _includeOverlapping;

            public BasicSelfIntersectsVisitor(bool includeOverlapping)
            {
                _includeOverlapping = includeOverlapping;
            }

            public bool VisitBasicIntr(PlineBasicIntersect<T> intr)
            {
                Intrs.Add(intr);
                return true;
            }

            public bool VisitOverlappingIntr(PlineOverlappingIntersect<T> intr)
            {
                if (_includeOverlapping)
                {
                    Intrs.Add(new PlineBasicIntersect<T>(intr.StartIndex1, intr.StartIndex2, intr.Point1));
                    Intrs.Add(new PlineBasicIntersect<T>(intr.StartIndex1, intr.StartIndex2, intr.Point2));
                }
                return true;
            }
        }

        public static List<PlineBasicIntersect<T>> AllSelfIntersectsAsBasic<T>(
            IPlineSource<T> polyline,
            StaticAABB2DIndex<T> aabbIndex,
            bool includeOverlapping,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var visitor = new BasicSelfIntersectsVisitor<T>(includeOverlapping);
            VisitLocalSelfIntersects(polyline, visitor, posEqualEps);
            VisitGlobalSelfIntersects(polyline, aabbIndex, visitor, posEqualEps);
            return visitor.Intrs;
        }

        private struct TwoPlinesQueryVisitor<T> : IQueryVisitor
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            private readonly IPlineSource<T> _pline1;
            private readonly PlineIntersectVisitContext<T> _pline2Context;
            private readonly ITwoPlinesIntersectVisitor<T> _visitor;
            private readonly T _posEqualEps;
            public bool Cf;

            public TwoPlinesQueryVisitor(
                IPlineSource<T> pline1,
                in PlineIntersectVisitContext<T> pline2Context,
                ITwoPlinesIntersectVisitor<T> visitor,
                T posEqualEps)
            {
                _pline1 = pline1;
                _pline2Context = pline2Context;
                _visitor = visitor;
                _posEqualEps = posEqualEps;
                Cf = true;
            }

            public bool Visit(int i1)
            {
                int j1 = _pline1.NextWrappingIndex(i1);

                var pline1Context = new PlineIntersectVisitContext<T>(
                    i1,
                    _pline1.Get(i1),
                    _pline1.Get(j1)
                );

                var intr = PlineSegIntersection.Intersect(
                    pline1Context.V1,
                    pline1Context.V2,
                    _pline2Context.V1,
                    _pline2Context.V2,
                    _posEqualEps
                );

                if (!_visitor.Visit(intr, pline1Context, _pline2Context))
                {
                    Cf = false;
                    return false;
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
            StaticAABB2DIndex<T> pline1AabbIndex = options.Pline1AabbIndex ?? pline1.CreateApproxAabbIndex();

            var queryStack = new List<int>(8);

            foreach (var (i2, j2) in pline2.IterSegmentIndexes())
            {
                var pline2Context = new PlineIntersectVisitContext<T>(
                    i2,
                    pline2.Get(i2),
                    pline2.Get(j2)
                );

                var queryVisitor = new TwoPlinesQueryVisitor<T>(
                    pline1,
                    pline2Context,
                    visitor,
                    posEqualEps
                );

                var bb = PlineSeg.SegFastApproxBoundingBox(pline2Context.V1, pline2Context.V2);

                pline1AabbIndex.VisitQueryWithStack(
                    bb.MinX - posEqualEps,
                    bb.MinY - posEqualEps,
                    bb.MaxX + posEqualEps,
                    bb.MaxY + posEqualEps,
                    ref queryVisitor,
                    queryStack
                );

                if (!queryVisitor.Cf)
                {
                    break;
                }
            }
        }

        private class FindIntersectsVisitor<T> : ITwoPlinesIntersectVisitor<T>
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            public readonly PlineIntersectsCollection<T> Result = PlineIntersectsCollection<T>.NewEmpty();
            public readonly HashSet<int> PossibleDuplicates1 = new();
            public readonly HashSet<int> PossibleDuplicates2 = new();

            private readonly IPlineSource<T> _pline1;
            private readonly IPlineSource<T> _pline2;
            private readonly T _posEqualEps;
            private readonly int _open1LastIdx;
            private readonly int _open2LastIdx;

            public FindIntersectsVisitor(
                IPlineSource<T> pline1,
                IPlineSource<T> pline2,
                T posEqualEps)
            {
                _pline1 = pline1;
                _pline2 = pline2;
                _posEqualEps = posEqualEps;
                _open1LastIdx = pline1.VertexCount - 2;
                _open2LastIdx = pline2.VertexCount - 2;
            }

            public bool Visit(
                PlineSegIntr<T> intersect,
                in PlineIntersectVisitContext<T> pline1Context,
                in PlineIntersectVisitContext<T> pline2Context)
            {
                int i1 = pline1Context.VertexIndex;
                int i2 = pline2Context.VertexIndex;

                var p1V2 = pline1Context.V2;
                var p2V2 = pline2Context.V2;
                bool SkipIntrAtEnd(Vector2<T> intr)
                {
                    return (p1V2.Pos().FuzzyEqEps(intr, _posEqualEps)
                        && (_pline1.IsClosed || i1 != _open1LastIdx))
                        || (p2V2.Pos().FuzzyEqEps(intr, _posEqualEps)
                            && (_pline2.IsClosed || i2 != _open2LastIdx));
                }

                switch (intersect.Kind)
                {
                    case PlineSegIntrKind.NoIntersect:
                        break;
                    case PlineSegIntrKind.TangentIntersect:
                    case PlineSegIntrKind.OneIntersect:
                        if (!SkipIntrAtEnd(intersect.Point1))
                        {
                            Result.BasicIntersects.Add(new PlineBasicIntersect<T>(i1, i2, intersect.Point1));
                        }
                        break;
                    case PlineSegIntrKind.TwoIntersects:
                        if (!SkipIntrAtEnd(intersect.Point1))
                        {
                            Result.BasicIntersects.Add(new PlineBasicIntersect<T>(i1, i2, intersect.Point1));
                        }
                        if (!SkipIntrAtEnd(intersect.Point2))
                        {
                            Result.BasicIntersects.Add(new PlineBasicIntersect<T>(i1, i2, intersect.Point2));
                        }
                        break;
                    case PlineSegIntrKind.OverlappingLines:
                    case PlineSegIntrKind.OverlappingArcs:
                        Result.OverlappingIntersects.Add(new PlineOverlappingIntersect<T>(i1, i2, intersect.Point1, intersect.Point2));

                        if (pline1Context.V2.Pos().FuzzyEqEps(intersect.Point1, _posEqualEps)
                            || pline1Context.V2.Pos().FuzzyEqEps(intersect.Point2, _posEqualEps))
                        {
                            PossibleDuplicates1.Add(_pline1.NextWrappingIndex(i1));
                        }

                        if (pline2Context.V2.Pos().FuzzyEqEps(intersect.Point1, _posEqualEps)
                            || pline2Context.V2.Pos().FuzzyEqEps(intersect.Point2, _posEqualEps))
                        {
                            PossibleDuplicates2.Add(_pline2.NextWrappingIndex(i2));
                        }
                        break;
                }

                return true;
            }
        }

        public static PlineIntersectsCollection<T> FindIntersects<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            FindIntersectsOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (pline1.VertexCount < 2 || pline2.VertexCount < 2)
            {
                return PlineIntersectsCollection<T>.NewEmpty();
            }

            T posEqualEps = options.PosEqualEps;

            var visitor = new FindIntersectsVisitor<T>(pline1, pline2, posEqualEps);
            VisitIntersects(pline1, pline2, visitor, options);

            var result = visitor.Result;

            if (visitor.PossibleDuplicates1.Count == 0 && visitor.PossibleDuplicates2.Count == 0)
            {
                return result;
            }

            var finalBasicIntrs = new List<PlineBasicIntersect<T>>(result.BasicIntersects.Count);

            foreach (var intr in result.BasicIntersects)
            {
                if (visitor.PossibleDuplicates1.Contains(intr.StartIndex1))
                {
                    var startPt1 = pline1.Get(intr.StartIndex1).Pos();
                    if (intr.Point.FuzzyEqEps(startPt1, posEqualEps))
                    {
                        continue;
                    }
                }

                if (visitor.PossibleDuplicates2.Contains(intr.StartIndex2))
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

        private class ScanForIntersectVisitor<T> : ITwoPlinesIntersectVisitor<T>
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            public bool FoundIntersect = false;

            public bool Visit(
                PlineSegIntr<T> intersect,
                in PlineIntersectVisitContext<T> pline1Context,
                in PlineIntersectVisitContext<T> pline2Context)
            {
                switch (intersect.Kind)
                {
                    case PlineSegIntrKind.NoIntersect:
                        return true;
                    default:
                        FoundIntersect = true;
                        return false;
                }
            }
        }

        public static bool ScanForIntersect<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            FindIntersectsOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var visitor = new ScanForIntersectVisitor<T>();
            VisitIntersects(pline1, pline2, visitor, options);
            return visitor.FoundIntersect;
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

            for (int idx = 1; idx < intersects.Count; idx++)
            {
                var intr = intersects[idx];
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

            {
                var slice = OverlappingSlice<T>.New(pline1, pline2, startIntr, endIntr, posEqualEps);
                result.Add(slice);
            }

            if (result.Count > 1)
            {
                var lastSliceEnd = result[^1].ViewData.EndPoint;
                var firstSliceBegin = result[0].ViewData.UpdatedStart.Pos();
                if (lastSliceEnd.FuzzyEqEps(firstSliceBegin, posEqualEps))
                {
                    var lastSlice = result[^1];
                    result.RemoveAt(result.Count - 1);

                    var firstSlice = result[0];

                    int updatedEndIndexOffset = firstSlice.ViewData.EndIndexOffset + lastSlice.ViewData.EndIndexOffset;
                    if (lastSlice.ViewData.EndPoint.FuzzyEqEps(pline2.Get(0).Pos(), posEqualEps))
                    {
                        updatedEndIndexOffset += 1;
                    }

                    var updatedViewData = new PlineViewData<T>(
                        firstSlice.ViewData.StartIndex,
                        updatedEndIndexOffset,
                        lastSlice.ViewData.UpdatedStart,
                        firstSlice.ViewData.UpdatedEndBulge,
                        firstSlice.ViewData.EndPoint,
                        false
                    );

                    result[0] = new OverlappingSlice<T>(
                        lastSlice.StartIndexes,
                        firstSlice.EndIndexes,
                        updatedViewData,
                        firstSlice.IsLoop,
                        firstSlice.OpposingDirections
                    );
                }
            }

            return result;
        }
    }
}
