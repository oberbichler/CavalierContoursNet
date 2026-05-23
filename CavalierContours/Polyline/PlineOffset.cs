using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    public readonly struct RawPlineOffsetSeg<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly PlineVertex<T> V1;
        public readonly PlineVertex<T> V2;
        public readonly Vector2<T> OrigV2Pos;
        public readonly bool CollapsedArc;

        public RawPlineOffsetSeg(PlineVertex<T> v1, PlineVertex<T> v2, Vector2<T> origV2Pos, bool collapsedArc)
        {
            V1 = v1;
            V2 = v2;
            OrigV2Pos = origV2Pos;
            CollapsedArc = collapsedArc;
        }
    }

    public static class PlineOffset
    {
        private readonly struct JoinParams<T>
            where T : struct, IFloatingPointIeee754<T>
        {
            public readonly bool ConnectionArcsCcw;
            public readonly T PosEqualEps;

            public JoinParams(bool connectionArcsCcw, T posEqualEps)
            {
                ConnectionArcsCcw = connectionArcsCcw;
                PosEqualEps = posEqualEps;
            }
        }

        private struct PointValidForOffsetVisitor<T> : IQueryVisitor
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            private readonly IPlineSource<T> _polyline;
            private readonly T _minDist;
            private readonly Vector2<T> _point;
            private readonly T _posEqualEps;
            public bool PointValid;

            public PointValidForOffsetVisitor(IPlineSource<T> polyline, T minDist, Vector2<T> point, T posEqualEps)
            {
                _polyline = polyline;
                _minDist = minDist;
                _point = point;
                _posEqualEps = posEqualEps;
                PointValid = true;
            }

            public bool Visit(int i)
            {
                int j = _polyline.NextWrappingIndex(i);
                var closestPoint = PlineSeg.SegClosestPoint(_polyline.Get(i), _polyline.Get(j), _point, _posEqualEps);
                T dist = BaseMath.DistSquared(closestPoint, _point);
                PointValid = dist > _minDist;
                return PointValid;
            }
        }

        private struct IntersectsOriginalPlineVisitor<T> : IQueryVisitor
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            private readonly IPlineSource<T> _originalPolyline;
            private readonly PlineVertex<T> _v1;
            private readonly PlineVertex<T> _v2;
            private readonly T _posEqualEps;
            public bool HasIntersect;

            public IntersectsOriginalPlineVisitor(
                IPlineSource<T> originalPolyline,
                PlineVertex<T> v1,
                PlineVertex<T> v2,
                T posEqualEps)
            {
                _originalPolyline = originalPolyline;
                _v1 = v1;
                _v2 = v2;
                _posEqualEps = posEqualEps;
                HasIntersect = false;
            }

            public bool Visit(int i)
            {
                int j = _originalPolyline.NextWrappingIndex(i);
                var segIntr = PlineSegIntersection.Intersect(
                    _v1,
                    _v2,
                    _originalPolyline.Get(i),
                    _originalPolyline.Get(j),
                    _posEqualEps
                );
                HasIntersect = segIntr.Kind != PlineSegIntrKind.NoIntersect;
                return !HasIntersect;
            }
        }

        private struct StitchVisitor : IQueryVisitor
        {
            private readonly bool[] _visitedIndexes;
            private readonly List<int> _queryResults;

            public StitchVisitor(bool[] visitedIndexes, List<int> queryResults)
            {
                _visitedIndexes = visitedIndexes;
                _queryResults = queryResults;
            }

            public bool Visit(int i)
            {
                if (!_visitedIndexes[i])
                {
                    _queryResults.Add(i);
                }
                return true;
            }
        }

        public static List<RawPlineOffsetSeg<T>> CreateUntrimmedRawOffsetSegs<T>(IPlineSource<T> polyline, T offset)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            RawPlineOffsetSeg<T> ProcessLineSeg(PlineVertex<T> v1, PlineVertex<T> v2)
            {
                var lineV = v2.Pos() - v1.Pos();
                var offsetV = lineV.SafeUnitPerp().Scale(offset);
                return new RawPlineOffsetSeg<T>(
                    PlineVertex<T>.FromVector2(v1.Pos() + offsetV, T.Zero),
                    PlineVertex<T>.FromVector2(v2.Pos() + offsetV, T.Zero),
                    v2.Pos(),
                    false
                );
            }

            RawPlineOffsetSeg<T> ProcessArcSeg(PlineVertex<T> v1, PlineVertex<T> v2)
            {
                (T arcRadius, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                T offs = v1.BulgeIsNeg() ? offset : -offset;
                T radiusAfterOffset = arcRadius + offs;
                var v1ToCenter = (v1.Pos() - arcCenter).SafeNormalize();
                var v2ToCenter = (v2.Pos() - arcCenter).SafeNormalize();

                T newV1Bulge;
                bool collapsedArc;
                if (radiusAfterOffset.FuzzyLt(T.Zero))
                {
                    newV1Bulge = T.Zero;
                    collapsedArc = true;
                }
                else
                {
                    newV1Bulge = v1.Bulge;
                    collapsedArc = false;
                }

                return new RawPlineOffsetSeg<T>(
                    PlineVertex<T>.FromVector2(v1ToCenter.Scale(offs) + v1.Pos(), newV1Bulge),
                    PlineVertex<T>.FromVector2(v2ToCenter.Scale(offs) + v2.Pos(), v2.Bulge),
                    v2.Pos(),
                    collapsedArc
                );
            }

            int segmentCount = polyline.SegmentCount();
            var result = new List<RawPlineOffsetSeg<T>>(segmentCount);

            foreach (var (v1, v2) in polyline.IterSegments())
            {
                if (v1.BulgeIsZero())
                {
                    result.Add(ProcessLineSeg(v1, v2));
                }
                else
                {
                    result.Add(ProcessArcSeg(v1, v2));
                }
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFalseIntersect<T>(T t)
            where T : struct, IFloatingPointIeee754<T>
        {
            return t < T.Zero || t > T.One;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T BulgeForConnection<T>(
            Vector2<T> arcCenter,
            Vector2<T> startPoint,
            Vector2<T> endPoint,
            bool isCcw)
            where T : struct, IFloatingPointIeee754<T>
        {
            T a1 = BaseMath.Angle(arcCenter, startPoint);
            T a2 = BaseMath.Angle(arcCenter, endPoint);
            return BaseMath.BulgeFromAngle(BaseMath.DeltaAngleSigned(a1, a2, !isCcw));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ConnectUsingArc<T>(
            RawPlineOffsetSeg<T> s1,
            RawPlineOffsetSeg<T> s2,
            bool connectionArcsCcw,
            IPlineSourceMut<T> result,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var arcCenter = s1.OrigV2Pos;
            var sp = s1.V2.Pos();
            var ep = s2.V1.Pos();
            T bulge = BulgeForConnection(arcCenter, sp, ep, connectionArcsCcw);
            AddOrReplace(result, sp.X, sp.Y, bulge, posEqualEps);
            AddOrReplace(result, ep.X, ep.Y, s2.V1.Bulge, posEqualEps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddOrReplace<T>(IPlineSourceMut<T> self, T x, T y, T bulge, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            self.AddOrReplaceVertex(new PlineVertex<T>(x, y, bulge), posEqualEps);
        }

        private static void LineLineJoin<T>(
            RawPlineOffsetSeg<T> s1,
            RawPlineOffsetSeg<T> s2,
            in JoinParams<T> joinParams,
            IPlineSourceMut<T> result)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            bool connectionArcsCcw = joinParams.ConnectionArcsCcw;
            T posEqualEps = joinParams.PosEqualEps;
            var v1 = s1.V1;
            var v2 = s1.V2;
            var u1 = s2.V1;
            var u2 = s2.V2;

            Debug.Assert(v1.BulgeIsZero() && u1.BulgeIsZero(), "both segments should be lines");

            if (s1.CollapsedArc || s2.CollapsedArc)
            {
                ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
            }
            else
            {
                var intr = LineLineIntersection.Intersect(v1.Pos(), v2.Pos(), u1.Pos(), u2.Pos(), posEqualEps);
                switch (intr.Kind)
                {
                    case LineLineIntrKind.NoIntersect:
                        {
                            var sp = s1.V2.Pos();
                            var ep = s2.V1.Pos();
                            T bulge = connectionArcsCcw ? T.One : -T.One;
                            AddOrReplace(result, sp.X, sp.Y, bulge, posEqualEps);
                            AddOrReplace(result, ep.X, ep.Y, s2.V1.Bulge, posEqualEps);
                            break;
                        }
                    case LineLineIntrKind.TrueIntersect:
                        {
                            var intrPoint = BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), intr.Seg1T);
                            AddOrReplace(result, intrPoint.X, intrPoint.Y, T.Zero, posEqualEps);
                            break;
                        }
                    case LineLineIntrKind.Overlapping:
                        {
                            AddOrReplace(result, v2.X, v2.Y, T.Zero, posEqualEps);
                            break;
                        }
                    case LineLineIntrKind.FalseIntersect:
                        {
                            if (intr.Seg1T > T.One && IsFalseIntersect(intr.Seg2T))
                            {
                                ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
                            }
                            else
                            {
                                AddOrReplace(result, v2.X, v2.Y, T.Zero, posEqualEps);
                                AddOrReplace(result, u1.X, u1.Y, u1.Bulge, posEqualEps);
                            }
                            break;
                        }
                }
            }
        }

        private static void LineArcJoin<T>(
            RawPlineOffsetSeg<T> s1,
            RawPlineOffsetSeg<T> s2,
            in JoinParams<T> joinParams,
            IPlineSourceMut<T> result)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            bool connectionArcsCcw = joinParams.ConnectionArcsCcw;
            T posEqualEps = joinParams.PosEqualEps;
            var v1 = s1.V1;
            var v2 = s1.V2;
            var u1 = s2.V1;
            var u2 = s2.V2;

            Debug.Assert(v1.BulgeIsZero() && !u1.BulgeIsZero(), "first segment should be line, second segment should be arc");

            (T arcRadius, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(u1, u2);

            void ProcessIntersect(T t, Vector2<T> intersect)
            {
                bool trueLineIntr = !IsFalseIntersect(t);
                bool trueArcIntr = BaseMath.PointWithinArcSweep(
                    arcCenter,
                    u1.Pos(),
                    u2.Pos(),
                    u1.BulgeIsNeg(),
                    intersect,
                    posEqualEps
                );

                if (trueLineIntr && trueArcIntr)
                {
                    T a = BaseMath.Angle(arcCenter, intersect);
                    T arcEndAngle = BaseMath.Angle(arcCenter, u2.Pos());
                    T theta = BaseMath.DeltaAngle(a, arcEndAngle);
                    if ((theta > T.Zero) == u1.BulgeIsPos())
                    {
                        AddOrReplace(result, intersect.X, intersect.Y, BaseMath.BulgeFromAngle(theta), posEqualEps);
                    }
                    else
                    {
                        AddOrReplace(result, intersect.X, intersect.Y, u1.Bulge, posEqualEps);
                    }
                    return;
                }

                if (t > T.One && !trueArcIntr)
                {
                    ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
                    return;
                }

                if (s1.CollapsedArc)
                {
                    ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
                    return;
                }

                AddOrReplace(result, v2.X, v2.Y, T.Zero, posEqualEps);
                result.AddOrReplaceVertex(u1, posEqualEps);
            }

            var lineCircleIntr = LineCircleIntersection.Intersect(v1.Pos(), v2.Pos(), arcRadius, arcCenter, posEqualEps);
            switch (lineCircleIntr.Kind)
            {
                case LineCircleIntrKind.NoIntersect:
                    {
                        ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
                        break;
                    }
                case LineCircleIntrKind.TangentIntersect:
                    {
                        ProcessIntersect(lineCircleIntr.T0, BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), lineCircleIntr.T0));
                        break;
                    }
                case LineCircleIntrKind.TwoIntersects:
                    {
                        var intr1 = BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), lineCircleIntr.T0);
                        T dist1 = BaseMath.DistSquared(intr1, s1.OrigV2Pos);
                        var intr2 = BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), lineCircleIntr.T1);
                        T dist2 = BaseMath.DistSquared(intr2, s1.OrigV2Pos);

                        if (dist1 < dist2)
                        {
                            ProcessIntersect(lineCircleIntr.T0, intr1);
                        }
                        else
                        {
                            ProcessIntersect(lineCircleIntr.T1, intr2);
                        }
                        break;
                    }
            }
        }

        private static void ArcLineJoin<T>(
            RawPlineOffsetSeg<T> s1,
            RawPlineOffsetSeg<T> s2,
            in JoinParams<T> joinParams,
            IPlineSourceMut<T> result)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            bool connectionArcsCcw = joinParams.ConnectionArcsCcw;
            T posEqualEps = joinParams.PosEqualEps;
            var v1 = s1.V1;
            var v2 = s1.V2;
            var u1 = s2.V1;
            var u2 = s2.V2;

            Debug.Assert(!v1.BulgeIsZero() && u1.BulgeIsZero(), "first segment should be arc, second segment should be line");

            (T arcRadius, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(v1, v2);

            void ProcessIntersect(T t, Vector2<T> intersect)
            {
                bool trueLineIntr = !IsFalseIntersect(t);
                bool trueArcIntr = BaseMath.PointWithinArcSweep(
                    arcCenter,
                    v1.Pos(),
                    v2.Pos(),
                    v1.BulgeIsNeg(),
                    intersect,
                    posEqualEps
                );

                if (trueLineIntr && trueArcIntr)
                {
                    var prevVertex = result.Last()!.Value;
                    if (!prevVertex.BulgeIsZero()
                        && !prevVertex.Pos().FuzzyEqEps(v2.Pos(), posEqualEps))
                    {
                        T a = BaseMath.Angle(arcCenter, intersect);
                        (_, Vector2<T> prevArcCenter) = PlineSeg.SegArcRadiusAndCenter(prevVertex, v2);
                        T prevArcStartAngle = BaseMath.Angle(prevArcCenter, prevVertex.Pos());
                        T updatedPrevTheta = BaseMath.DeltaAngle(prevArcStartAngle, a);

                        if ((updatedPrevTheta > T.Zero) == prevVertex.BulgeIsPos())
                        {
                            result.SetVertex(result.VertexCount - 1, prevVertex.WithBulge(BaseMath.BulgeFromAngle(updatedPrevTheta)));
                        }
                    }

                    AddOrReplace(result, intersect.X, intersect.Y, T.Zero, posEqualEps);
                    return;
                }

                ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
            }

            var lineCircleIntr = LineCircleIntersection.Intersect(u1.Pos(), u2.Pos(), arcRadius, arcCenter, posEqualEps);
            switch (lineCircleIntr.Kind)
            {
                case LineCircleIntrKind.NoIntersect:
                    {
                        ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
                        break;
                    }
                case LineCircleIntrKind.TangentIntersect:
                    {
                        ProcessIntersect(lineCircleIntr.T0, BaseMath.PointFromParametric(u1.Pos(), u2.Pos(), lineCircleIntr.T0));
                        break;
                    }
                case LineCircleIntrKind.TwoIntersects:
                    {
                        var origPoint = s2.CollapsedArc ? u1.Pos() : s1.OrigV2Pos;
                        var intr1 = BaseMath.PointFromParametric(u1.Pos(), u2.Pos(), lineCircleIntr.T0);
                        T dist1 = BaseMath.DistSquared(intr1, origPoint);
                        var intr2 = BaseMath.PointFromParametric(u1.Pos(), u2.Pos(), lineCircleIntr.T1);
                        T dist2 = BaseMath.DistSquared(intr2, origPoint);

                        if (dist1 < dist2)
                        {
                            ProcessIntersect(lineCircleIntr.T0, intr1);
                        }
                        else
                        {
                            ProcessIntersect(lineCircleIntr.T1, intr2);
                        }
                        break;
                    }
            }
        }

        private static void ArcArcJoin<T>(
            RawPlineOffsetSeg<T> s1,
            RawPlineOffsetSeg<T> s2,
            in JoinParams<T> joinParams,
            IPlineSourceMut<T> result)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            bool connectionArcsCcw = joinParams.ConnectionArcsCcw;
            T posEqualEps = joinParams.PosEqualEps;
            var v1 = s1.V1;
            var v2 = s1.V2;
            var u1 = s2.V1;
            var u2 = s2.V2;

            Debug.Assert(!v1.BulgeIsZero() && !u1.BulgeIsZero(), "both segments should be arcs");

            (T arc1Radius, Vector2<T> arc1Center) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
            (T arc2Radius, Vector2<T> arc2Center) = PlineSeg.SegArcRadiusAndCenter(u1, u2);

            bool BothArcsSweepPoint(Vector2<T> point)
            {
                return BaseMath.PointWithinArcSweep(
                    arc1Center,
                    v1.Pos(),
                    v2.Pos(),
                    v1.BulgeIsNeg(),
                    point,
                    posEqualEps
                ) && BaseMath.PointWithinArcSweep(
                    arc2Center,
                    u1.Pos(),
                    u2.Pos(),
                    u1.BulgeIsNeg(),
                    point,
                    posEqualEps
                );
            }

            void ProcessIntersect(Vector2<T> intersect, bool trueIntersect)
            {
                if (!trueIntersect)
                {
                    ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
                }
                else
                {
                    var prevVertex = result.Last()!.Value;

                    if (!prevVertex.BulgeIsZero()
                        && !prevVertex.Pos().FuzzyEqEps(v2.Pos(), posEqualEps))
                    {
                        T a1 = BaseMath.Angle(arc1Center, intersect);
                        (_, Vector2<T> prevArcCenter) = PlineSeg.SegArcRadiusAndCenter(prevVertex, v2);
                        T prevArcStartAngle = BaseMath.Angle(prevArcCenter, prevVertex.Pos());
                        T updatedPrevTheta = BaseMath.DeltaAngle(prevArcStartAngle, a1);

                        if ((updatedPrevTheta > T.Zero) == prevVertex.BulgeIsPos())
                        {
                            result.SetVertex(result.VertexCount - 1, prevVertex.WithBulge(BaseMath.BulgeFromAngle(updatedPrevTheta)));
                        }
                    }

                    T a2 = BaseMath.Angle(arc2Center, intersect);
                    T endAngle = BaseMath.Angle(arc2Center, u2.Pos());
                    T theta = BaseMath.DeltaAngle(a2, endAngle);

                    if ((theta > T.Zero) == u1.BulgeIsPos())
                    {
                        AddOrReplace(result, intersect.X, intersect.Y, BaseMath.BulgeFromAngle(theta), posEqualEps);
                    }
                    else
                    {
                        AddOrReplace(result, intersect.X, intersect.Y, u1.Bulge, posEqualEps);
                    }
                }
            }

            var ccIntr = CircleCircleIntersection.Intersect(arc1Radius, arc1Center, arc2Radius, arc2Center, posEqualEps);
            switch (ccIntr.Kind)
            {
                case CircleCircleIntrKind.NoIntersect:
                    {
                        ConnectUsingArc(s1, s2, connectionArcsCcw, result, posEqualEps);
                        break;
                    }
                case CircleCircleIntrKind.TangentIntersect:
                    {
                        ProcessIntersect(ccIntr.Point1, BothArcsSweepPoint(ccIntr.Point1));
                        break;
                    }
                case CircleCircleIntrKind.TwoIntersects:
                    {
                        T dist1 = BaseMath.DistSquared(ccIntr.Point1, s1.OrigV2Pos);
                        T dist2 = BaseMath.DistSquared(ccIntr.Point2, s1.OrigV2Pos);
                        if (dist1.FuzzyEq(dist2, posEqualEps))
                        {
                            if (BothArcsSweepPoint(ccIntr.Point1))
                            {
                                ProcessIntersect(ccIntr.Point1, true);
                            }
                            else
                            {
                                ProcessIntersect(ccIntr.Point2, BothArcsSweepPoint(ccIntr.Point2));
                            }
                        }
                        else if (dist1 < dist2)
                        {
                            ProcessIntersect(ccIntr.Point1, BothArcsSweepPoint(ccIntr.Point1));
                        }
                        else
                        {
                            ProcessIntersect(ccIntr.Point2, BothArcsSweepPoint(ccIntr.Point2));
                        }
                        break;
                    }
                case CircleCircleIntrKind.Overlapping:
                    {
                        result.AddOrReplaceVertex(u1, posEqualEps);
                        break;
                    }
            }
        }

        public static O CreateRawOffsetPolyline<O, T>(IPlineSource<T> polyline, T offset, T posEqualEps)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = polyline.VertexCount;
            if (vc < 2)
            {
                return new O();
            }

            var rawOffsetSegs = CreateUntrimmedRawOffsetSegs(polyline, offset);
            if (rawOffsetSegs.Count == 0)
            {
                return new O();
            }

            // detect single collapsed arc segment
            if (rawOffsetSegs.Count == 1 && rawOffsetSegs[0].CollapsedArc)
            {
                return new O();
            }

            bool connectionArcsCcw = offset < T.Zero;
            var joinParams = new JoinParams<T>(connectionArcsCcw, posEqualEps);

            void JoinSegPair(in RawPlineOffsetSeg<T> s1, in RawPlineOffsetSeg<T> s2, IPlineSourceMut<T> res)
            {
                bool s1IsLine = s1.V1.BulgeIsZero();
                bool s2IsLine = s2.V1.BulgeIsZero();
                if (s1IsLine && s2IsLine)
                {
                    LineLineJoin(s1, s2, joinParams, res);
                }
                else if (s1IsLine && !s2IsLine)
                {
                    LineArcJoin(s1, s2, joinParams, res);
                }
                else if (!s1IsLine && s2IsLine)
                {
                    ArcLineJoin(s1, s2, joinParams, res);
                }
                else
                {
                    ArcArcJoin(s1, s2, joinParams, res);
                }
            }

            var result = new O();
            result.SetIsClosed(polyline.IsClosed);

            // add the very first vertex
            result.AddVertex(rawOffsetSegs[0].V1);

            if (rawOffsetSegs.Count >= 2)
            {
                JoinSegPair(rawOffsetSegs[0], rawOffsetSegs[1], result);
            }

            bool firstVertexReplaced = result.VertexCount == 1;

            for (int i = 1; i < rawOffsetSegs.Count - 1; i++)
            {
                JoinSegPair(rawOffsetSegs[i], rawOffsetSegs[i + 1], result);
            }

            if (polyline.IsClosed && result.VertexCount > 1)
            {
                // join closing segments at vertex indexes (n, 0) and (0, 1)
                var s1 = rawOffsetSegs[^1];
                var s2 = rawOffsetSegs[0];

                // temp polyline to capture results of joining (to avoid mutating result)
                var closingPartResult = new O();
                closingPartResult.AddVertex(result.Last()!.Value);
                JoinSegPair(s1, s2, closingPartResult);

                // update last vertexes
                result.SetVertex(result.VertexCount - 1, closingPartResult.Get(0));
                for (int idx = 1; idx < closingPartResult.VertexCount; idx++)
                {
                    result.AddVertex(closingPartResult.Get(idx));
                }

                // update first vertex (only if it has not already been updated/replaced)
                if (!firstVertexReplaced)
                {
                    var updatedFirstPos = closingPartResult.Last()!.Value.Pos();
                    if (result.Get(0).BulgeIsZero())
                    {
                        // just update position
                        T b = result.Get(0).Bulge;
                        result.SetVertex(0, new PlineVertex<T>(updatedFirstPos.X, updatedFirstPos.Y, b));
                    }
                    else if (result.VertexCount > 1)
                    {
                        // update position and bulge
                        (_, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(result.Get(0), result.Get(1));
                        T a1 = BaseMath.Angle(arcCenter, updatedFirstPos);
                        T a2 = BaseMath.Angle(arcCenter, result.Get(1).Pos());
                        T updatedTheta = BaseMath.DeltaAngle(a1, a2);
                        if ((updatedTheta < T.Zero && result.Get(0).BulgeIsPos())
                            || (updatedTheta > T.Zero && result.Get(0).BulgeIsNeg()))
                        {
                            // first vertex not valid, just update its position (it will be removed later)
                            T b = result.Get(0).Bulge;
                            result.SetVertex(0, new PlineVertex<T>(updatedFirstPos.X, updatedFirstPos.Y, b));
                        }
                        else
                        {
                            // update position and bulge
                            result.SetVertex(0, new PlineVertex<T>(updatedFirstPos.X, updatedFirstPos.Y, BaseMath.BulgeFromAngle(updatedTheta)));
                        }
                    }
                }

                // must do final singularity prune between last, first, and second vertex because after
                // joining segments (n, 0) and (0, 1) they may have been introduced
                if (result.VertexCount > 1)
                {
                    if (result.Get(0).Pos().FuzzyEqEps(result.Last()!.Value.Pos(), posEqualEps))
                    {
                        result.Remove(result.VertexCount - 1);
                    }

                    if (result.VertexCount > 1
                        && result.Get(0).Pos().FuzzyEqEps(result.Get(1).Pos(), posEqualEps))
                    {
                        result.Remove(0);
                    }
                }
            }
            else
            {
                // not closed polyline or less than 2 vertexes
                var lastRawOffsetVertex = rawOffsetSegs[^1].V2;
                result.AddOrReplaceVertex(lastRawOffsetVertex, posEqualEps);
            }

            // if due to joining of segments we are left with only 1 vertex then return empty polyline
            if (result.VertexCount == 1)
            {
                result.Clear();
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointValidForOffset<T>(
            IPlineSource<T> polyline,
            T offset,
            StaticAABB2DIndex<T> aabbIndex,
            Vector2<T> point,
            List<int> queryStack,
            T posEqualEps,
            T offsetTol)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            T absOffset = T.Abs(offset) - offsetTol;
            T minDist = absOffset * absOffset;
            var visitor = new PointValidForOffsetVisitor<T>(polyline, minDist, point, posEqualEps);

            aabbIndex.VisitQueryWithStack(
                point.X - absOffset,
                point.Y - absOffset,
                point.X + absOffset,
                point.Y + absOffset,
                ref visitor,
                queryStack
            );

            return visitor.PointValid;
        }

        private static bool SliceIsValid<T>(
            PlineViewData<T> slice,
            IPlineSource<T> originalPolyline,
            IPlineSource<T> rawOffsetPolyline,
            StaticAABB2DIndex<T> origPolylineIndex,
            T offset,
            List<int> queryStack,
            T posEqualEps,
            T offsetDistEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            bool PointValidDist(Vector2<T> pt)
            {
                return PointValidForOffset(
                    originalPolyline,
                    offset,
                    origPolylineIndex,
                    pt,
                    queryStack,
                    posEqualEps,
                    offsetDistEps
                );
            }

            if (slice.EndIndexOffset == 0)
            {
                var v1 = slice.UpdatedStart;
                if (!PointValidDist(v1.Pos()))
                {
                    return false;
                }
                var v2 = PlineVertex<T>.FromVector2(slice.EndPoint, T.Zero);
                if (!PointValidDist(v2.Pos()))
                {
                    return false;
                }
                var midpoint = PlineSeg.SegMidpoint(v1, v2);
                if (!PointValidDist(midpoint))
                {
                    return false;
                }

                return !IntersectsOriginalPline(originalPolyline, origPolylineIndex, v1, v2, queryStack, posEqualEps);
            }

            var startSegMidpoint = PlineSeg.SegMidpoint(
                slice.UpdatedStart,
                rawOffsetPolyline.Get(rawOffsetPolyline.NextWrappingIndex(slice.StartIndex))
            );

            if (!PointValidDist(startSegMidpoint))
            {
                return false;
            }

            int endIndex = rawOffsetPolyline.FwdWrappingIndex(slice.StartIndex, slice.EndIndexOffset);
            var endSegMidpoint = PlineSeg.SegMidpoint(
                rawOffsetPolyline.Get(endIndex).WithBulge(slice.UpdatedEndBulge),
                PlineVertex<T>.FromVector2(slice.EndPoint, T.Zero)
            );

            if (!PointValidDist(endSegMidpoint))
            {
                return false;
            }

            foreach (var (v1, v2) in slice.View(rawOffsetPolyline).IterSegments())
            {
                if (!PointValidDist(v1.Pos()))
                {
                    return false;
                }

                if (IntersectsOriginalPline(originalPolyline, origPolylineIndex, v1, v2, queryStack, posEqualEps))
                {
                    return false;
                }
            }

            return PointValidDist(slice.EndPoint);
        }

        private static bool IntersectsOriginalPline<T>(
            IPlineSource<T> originalPolyline,
            StaticAABB2DIndex<T> origPolylineIndex,
            PlineVertex<T> v1,
            PlineVertex<T> v2,
            List<int> queryStack,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var approxBb = PlineSeg.SegFastApproxBoundingBox(v1, v2);
            var visitor = new IntersectsOriginalPlineVisitor<T>(originalPolyline, v1, v2, posEqualEps);
            T fuzz = Fuzzy<T>.Epsilon;

            origPolylineIndex.VisitQueryWithStack(
                approxBb.MinX - fuzz,
                approxBb.MinY - fuzz,
                approxBb.MaxX + fuzz,
                approxBb.MaxY + fuzz,
                ref visitor,
                queryStack
            );

            return visitor.HasIntersect;
        }

        public static List<PlineViewData<T>> SlicesFromRawOffset<T>(
            IPlineSource<T> originalPolyline,
            IPlineSource<T> rawOffsetPolyline,
            StaticAABB2DIndex<T> origPolylineIndex,
            T offset,
            PlineOffsetOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new List<PlineViewData<T>>();
            if (rawOffsetPolyline.VertexCount < 2)
            {
                return result;
            }

            T posEqualEps = options.PosEqualEps;
            T offsetDistEps = options.OffsetDistEps;

            var rawOffsetIndex = rawOffsetPolyline.CreateApproxAabbIndex();
            var selfIntrs = PlineIntersects.AllSelfIntersectsAsBasic(
                rawOffsetPolyline,
                rawOffsetIndex,
                false,
                posEqualEps
            );

            var queryStack = new List<int>();
            if (selfIntrs.Count == 0)
            {
                if (!PointValidForOffset(
                    originalPolyline,
                    offset,
                    origPolylineIndex,
                    rawOffsetPolyline.Get(0).Pos(),
                    queryStack,
                    posEqualEps,
                    offsetDistEps))
                {
                    return result;
                }

                var slice = PlineViewData<T>.FromEntirePline(rawOffsetPolyline);
                result.Add(slice);
                return result;
            }

            var intersectsLookup = new SortedDictionary<int, List<Vector2<T>>>();

            foreach (var si in selfIntrs)
            {
                if (!intersectsLookup.TryGetValue(si.StartIndex1, out var list1))
                {
                    list1 = new List<Vector2<T>>();
                    intersectsLookup[si.StartIndex1] = list1;
                }
                list1.Add(si.Point);

                if (!intersectsLookup.TryGetValue(si.StartIndex2, out var list2))
                {
                    list2 = new List<Vector2<T>>();
                    intersectsLookup[si.StartIndex2] = list2;
                }
                list2.Add(si.Point);
            }

            // sort intersects by distance from segment start vertex
            foreach (var kvp in intersectsLookup)
            {
                int i = kvp.Key;
                var intrList = kvp.Value;
                var startPos = rawOffsetPolyline.Get(i).Pos();
                intrList.Sort((si1, si2) =>
                {
                    T dist1 = BaseMath.DistSquared(si1, startPos);
                    T dist2 = BaseMath.DistSquared(si2, startPos);
                    return dist1.CompareTo(dist2);
                });
            }

            foreach (var kvp in intersectsLookup)
            {
                int startIndex = kvp.Key;
                var intrList = kvp.Value;

                for (int idx = 0; idx < intrList.Count - 1; idx++)
                {
                    var intr1 = intrList[idx];
                    var intr2 = intrList[idx + 1];

                    var slice = PlineViewData<T>.FromSlicePoints(
                        rawOffsetPolyline,
                        intr1,
                        startIndex,
                        intr2,
                        startIndex,
                        posEqualEps
                    );

                    if (slice.HasValue && SliceIsValid(slice.Value, originalPolyline, rawOffsetPolyline, origPolylineIndex, offset, queryStack, posEqualEps, offsetDistEps))
                    {
                        result.Add(slice.Value);
                    }
                }

                int nextIndex = rawOffsetPolyline.NextWrappingIndex(startIndex);

                int foundIndex = -1;
                List<Vector2<T>>? nextIntrList = null;

                foreach (var innerKvp in intersectsLookup)
                {
                    if (innerKvp.Key >= nextIndex)
                    {
                        foundIndex = innerKvp.Key;
                        nextIntrList = innerKvp.Value;
                        break;
                    }
                }

                if (nextIntrList == null)
                {
                    // wrap around
                    foreach (var innerKvp in intersectsLookup)
                    {
                        foundIndex = innerKvp.Key;
                        nextIntrList = innerKvp.Value;
                        break;
                    }
                }

                Debug.Assert(nextIntrList != null);

                var wrapSlice = PlineViewData<T>.FromSlicePoints(
                    rawOffsetPolyline,
                    intrList[^1],
                    startIndex,
                    nextIntrList[0],
                    foundIndex,
                    posEqualEps
                );

                if (wrapSlice.HasValue && SliceIsValid(wrapSlice.Value, originalPolyline, rawOffsetPolyline, origPolylineIndex, offset, queryStack, posEqualEps, offsetDistEps))
                {
                    result.Add(wrapSlice.Value);
                }
            }

            return result;
        }

        private static void VisitCircleIntersects<T>(
            IPlineSource<T> pline,
            Vector2<T> circleCenter,
            T circleRadius,
            StaticAABB2DIndex<T> aabbIndex,
            Action<int, Vector2<T>> visitor,
            PlineOffsetOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            T posEqualEps = options.PosEqualEps;

            bool IsValidLineIntr(T t)
            {
                return !IsFalseIntersect(t) && T.Abs(t) > posEqualEps;
            }

            bool IsValidArcIntr(
                Vector2<T> arcCenter,
                Vector2<T> arcStart,
                Vector2<T> arcEnd,
                T bulge,
                Vector2<T> intr)
            {
                return !arcStart.FuzzyEqEps(intr, posEqualEps)
                    && BaseMath.PointWithinArcSweep(
                        arcCenter,
                        arcStart,
                        arcEnd,
                        bulge < T.Zero,
                        intr,
                        posEqualEps
                    );
            }

            var queryResults = aabbIndex.Query(
                circleCenter.X - circleRadius,
                circleCenter.Y - circleRadius,
                circleCenter.X + circleRadius,
                circleCenter.Y + circleRadius
            );

            foreach (int startIndex in queryResults)
            {
                var v1 = pline.Get(startIndex);
                var v2 = pline.Get(pline.NextWrappingIndex(startIndex));
                if (v1.BulgeIsZero())
                {
                    var lineCircleIntr = LineCircleIntersection.Intersect(
                        v1.Pos(),
                        v2.Pos(),
                        circleRadius,
                        circleCenter,
                        posEqualEps
                    );

                    switch (lineCircleIntr.Kind)
                    {
                        case LineCircleIntrKind.TangentIntersect:
                            if (IsValidLineIntr(lineCircleIntr.T0))
                            {
                                visitor(startIndex, BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), lineCircleIntr.T0));
                            }
                            break;
                        case LineCircleIntrKind.TwoIntersects:
                            if (IsValidLineIntr(lineCircleIntr.T0))
                            {
                                visitor(startIndex, BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), lineCircleIntr.T0));
                            }
                            if (IsValidLineIntr(lineCircleIntr.T1))
                            {
                                visitor(startIndex, BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), lineCircleIntr.T1));
                            }
                            break;
                    }
                }
                else
                {
                    (T arcRadius, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                    var circleCircleIntr = CircleCircleIntersection.Intersect(
                        arcRadius,
                        arcCenter,
                        circleRadius,
                        circleCenter,
                        posEqualEps
                    );

                    switch (circleCircleIntr.Kind)
                    {
                        case CircleCircleIntrKind.TangentIntersect:
                            if (IsValidArcIntr(arcCenter, v1.Pos(), v2.Pos(), v1.Bulge, circleCircleIntr.Point1))
                            {
                                visitor(startIndex, circleCircleIntr.Point1);
                            }
                            break;
                        case CircleCircleIntrKind.TwoIntersects:
                            if (IsValidArcIntr(arcCenter, v1.Pos(), v2.Pos(), v1.Bulge, circleCircleIntr.Point1))
                            {
                                visitor(startIndex, circleCircleIntr.Point1);
                            }
                            if (IsValidArcIntr(arcCenter, v1.Pos(), v2.Pos(), v1.Bulge, circleCircleIntr.Point2))
                            {
                                visitor(startIndex, circleCircleIntr.Point2);
                            }
                            break;
                    }
                }
            }
        }

        public static List<PlineViewData<T>> SlicesFromDualRawOffsets<T>(
            IPlineSource<T> originalPolyline,
            IPlineSource<T> rawOffsetPolyline,
            IPlineSource<T> dualRawOffsetPolyline,
            StaticAABB2DIndex<T> origPolylineIndex,
            T offset,
            PlineOffsetOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new List<PlineViewData<T>>();
            if (rawOffsetPolyline.VertexCount < 2)
            {
                return result;
            }

            T posEqualEps = options.PosEqualEps;
            T offsetDistEps = options.OffsetDistEps;

            var rawOffsetIndex = rawOffsetPolyline.CreateApproxAabbIndex();

            var selfIntrs = PlineIntersects.AllSelfIntersectsAsBasic(
                rawOffsetPolyline,
                rawOffsetIndex,
                false,
                posEqualEps
            );

            var findIntrsOpts = new FindIntersectsOptions<T>
            {
                Pline1AabbIndex = rawOffsetIndex,
                PosEqualEps = options.PosEqualEps
            };

            var dualIntrs = PlineIntersects.FindIntersects(
                rawOffsetPolyline,
                dualRawOffsetPolyline,
                findIntrsOpts
            );

            var intersectsLookup = new SortedDictionary<int, List<Vector2<T>>>();

            void AddIntr(int startIndex, Vector2<T> intr)
            {
                if (!intersectsLookup.TryGetValue(startIndex, out var list))
                {
                    list = new List<Vector2<T>>();
                    intersectsLookup[startIndex] = list;
                }
                list.Add(intr);
            }

            if (!originalPolyline.IsClosed)
            {
                T circleRadius = T.Abs(offset);
                VisitCircleIntersects(
                    rawOffsetPolyline,
                    originalPolyline.Get(0).Pos(),
                    circleRadius,
                    rawOffsetIndex,
                    AddIntr,
                    options
                );
                VisitCircleIntersects(
                    rawOffsetPolyline,
                    originalPolyline.Last()!.Value.Pos(),
                    circleRadius,
                    rawOffsetIndex,
                    AddIntr,
                    options
                );
            }

            foreach (var si in selfIntrs)
            {
                AddIntr(si.StartIndex1, si.Point);
                AddIntr(si.StartIndex2, si.Point);
            }

            foreach (var intr in dualIntrs.BasicIntersects)
            {
                AddIntr(intr.StartIndex1, intr.Point);
            }

            var queryStack = new List<int>(8);

            if (intersectsLookup.Count == 0)
            {
                if (!PointValidForOffset(
                    originalPolyline,
                    offset,
                    origPolylineIndex,
                    rawOffsetPolyline.Get(0).Pos(),
                    queryStack,
                    posEqualEps,
                    offsetDistEps))
                {
                    return result;
                }

                var slice = PlineViewData<T>.FromEntirePline(rawOffsetPolyline);
                result.Add(slice);
                return result;
            }

            foreach (var kvp in intersectsLookup)
            {
                int i = kvp.Key;
                var intrList = kvp.Value;
                var startPos = rawOffsetPolyline.Get(i).Pos();
                intrList.Sort((si1, si2) =>
                {
                    T dist1 = BaseMath.DistSquared(si1, startPos);
                    T dist2 = BaseMath.DistSquared(si2, startPos);
                    return dist1.CompareTo(dist2);
                });
            }

            if (!originalPolyline.IsClosed)
            {
                // build first slice that ends at the first intersect
                var firstEntry = intersectsLookup.GetEnumerator();
                firstEntry.MoveNext();
                int intrIdx = firstEntry.Current.Key;
                var intrList = firstEntry.Current.Value;
                var intr = intrList[0];

                var slice = PlineViewData<T>.FromSlicePoints(
                    rawOffsetPolyline,
                    rawOffsetPolyline.Get(0).Pos(),
                    0,
                    intr,
                    intrIdx,
                    posEqualEps
                );

                if (slice.HasValue && SliceIsValid(slice.Value, originalPolyline, rawOffsetPolyline, origPolylineIndex, offset, queryStack, posEqualEps, offsetDistEps))
                {
                    result.Add(slice.Value);
                }
            }

            foreach (var kvp in intersectsLookup)
            {
                int startIndex = kvp.Key;
                var intrList = kvp.Value;

                for (int idx = 0; idx < intrList.Count - 1; idx++)
                {
                    var intr1 = intrList[idx];
                    var intr2 = intrList[idx + 1];

                    var slice = PlineViewData<T>.FromSlicePoints(
                        rawOffsetPolyline,
                        intr1,
                        startIndex,
                        intr2,
                        startIndex,
                        posEqualEps
                    );

                    if (slice.HasValue && SliceIsValid(slice.Value, originalPolyline, rawOffsetPolyline, origPolylineIndex, offset, queryStack, posEqualEps, offsetDistEps))
                    {
                        result.Add(slice.Value);
                    }
                }

                int nextIndex = rawOffsetPolyline.NextWrappingIndex(startIndex);

                int foundIndex = -1;
                List<Vector2<T>>? nextIntrList = null;

                foreach (var innerKvp in intersectsLookup)
                {
                    if (innerKvp.Key >= nextIndex)
                    {
                        foundIndex = innerKvp.Key;
                        nextIntrList = innerKvp.Value;
                        break;
                    }
                }

                if (nextIntrList == null)
                {
                    if (originalPolyline.IsClosed)
                    {
                        // wrap around
                        foreach (var innerKvp in intersectsLookup)
                        {
                            foundIndex = innerKvp.Key;
                            nextIntrList = innerKvp.Value;
                            break;
                        }
                    }
                    else
                    {
                        // open polyline and didn't find next intersect, we're done
                        var slice = PlineViewData<T>.FromSlicePoints(
                            rawOffsetPolyline,
                            intrList[^1],
                            startIndex,
                            rawOffsetPolyline.Last()!.Value.Pos(),
                            rawOffsetPolyline.VertexCount - 1,
                            posEqualEps
                        );

                        if (slice.HasValue && SliceIsValid(slice.Value, originalPolyline, rawOffsetPolyline, origPolylineIndex, offset, queryStack, posEqualEps, offsetDistEps))
                        {
                            result.Add(slice.Value);
                        }
                        return result;
                    }
                }

                Debug.Assert(nextIntrList != null);

                var wrapSlice = PlineViewData<T>.FromSlicePoints(
                    rawOffsetPolyline,
                    intrList[^1],
                    startIndex,
                    nextIntrList[0],
                    foundIndex,
                    posEqualEps
                );

                if (wrapSlice.HasValue && SliceIsValid(wrapSlice.Value, originalPolyline, rawOffsetPolyline, origPolylineIndex, offset, queryStack, posEqualEps, offsetDistEps))
                {
                    result.Add(wrapSlice.Value);
                }
            }

            return result;
        }

        public static List<O> StitchSlicesTogether<O, T>(
            IPlineSource<T> rawOffsetPline,
            IReadOnlyList<PlineViewData<T>> slices,
            bool isClosed,
            int origMaxIndex,
            PlineOffsetOptions<T> options)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new List<O>();
            if (slices.Count == 0)
            {
                return result;
            }

            T joinEps = options.SliceJoinEps;
            T posEqualEps = options.PosEqualEps;

            if (slices.Count == 1)
            {
                var pline = PlineSourceExtensions.CreateFromRemoveRepeat<O, T>(slices[0].View(rawOffsetPline), joinEps);

                if (isClosed
                    && pline.Get(0).Pos().FuzzyEqEps(pline.Last()!.Value.Pos(), joinEps))
                {
                    pline.SetIsClosed(true);
                    pline.Remove(pline.VertexCount - 1);
                }

                result.Add(pline);
                return result;
            }

            var aabbIndex = new Func<StaticAABB2DIndex<T>>(() =>
            {
                var builder = new StaticAABB2DIndexBuilder<T>(slices.Count);
                for (int idx = 0; idx < slices.Count; idx++)
                {
                    var slice = slices[idx];
                    var startPoint = slice.UpdatedStart.Pos();
                    builder.Add(
                        startPoint.X - joinEps,
                        startPoint.Y - joinEps,
                        startPoint.X + joinEps,
                        startPoint.Y + joinEps
                    );
                }
                return builder.Build();
            })();

            var visitedIndexes = new bool[slices.Count];
            var queryResults = new List<int>();
            var queryStack = new List<int>(8);

            for (int i = 0; i < slices.Count; i++)
            {
                if (visitedIndexes[i])
                {
                    continue;
                }

                visitedIndexes[i] = true;

                var currentPline = new O();
                currentPline.SetIsClosed(false);

                int currentIndex = i;
                var initialStartPoint = slices[i].UpdatedStart.Pos();
                int loopCount = 0;
                int maxLoopCount = slices.Count;

                while (true)
                {
                    if (loopCount > maxLoopCount)
                    {
                        throw new InvalidOperationException("loop_count exceeded max_loop_count while stitching slices together");
                    }
                    loopCount++;

                    var currentSlice = slices[currentIndex];

                    currentPline.ExtendRemoveRepeat(currentSlice.View(rawOffsetPline), joinEps);

                    int currentLoopStartIndex = currentSlice.StartIndex;
                    var currentEndPoint = currentSlice.EndPoint;

                    queryResults.Clear();
                    var stitchVisitor = new StitchVisitor(visitedIndexes, queryResults);

                    aabbIndex.VisitQueryWithStack(
                        currentEndPoint.X - joinEps,
                        currentEndPoint.Y - joinEps,
                        currentEndPoint.X + joinEps,
                        currentEndPoint.Y + joinEps,
                        ref stitchVisitor,
                        queryStack
                    );

                    int GetIndexDist(int idx)
                    {
                        var slice = slices[idx];
                        if (currentLoopStartIndex <= slice.StartIndex)
                        {
                            return slice.StartIndex - currentLoopStartIndex;
                        }
                        else
                        {
                            return origMaxIndex - currentLoopStartIndex + slice.StartIndex;
                        }
                    }

                    bool EndConnectsToStart(int idx)
                    {
                        var endPoint = slices[idx].EndPoint;
                        return endPoint.FuzzyEqEps(initialStartPoint, posEqualEps);
                    }

                    queryResults.Sort((a, b) =>
                    {
                        int cmp = GetIndexDist(a).CompareTo(GetIndexDist(b));
                        if (cmp != 0) return cmp;
                        return EndConnectsToStart(a).CompareTo(EndConnectsToStart(b));
                    });

                    if (queryResults.Count == 0)
                    {
                        if (currentPline.VertexCount > 1)
                        {
                            var currentPlineSp = currentPline.Get(0).Pos();
                            var currentPlineEp = currentPline.Last()!.Value.Pos();
                            if (isClosed && currentPlineSp.FuzzyEqEps(currentPlineEp, joinEps))
                            {
                                currentPline.Remove(currentPline.VertexCount - 1);
                                currentPline.SetIsClosed(true);
                            }

                            result.Add(currentPline);
                        }
                        break;
                    }

                    visitedIndexes[queryResults[0]] = true;
                    currentPline.Remove(currentPline.VertexCount - 1);
                    currentIndex = queryResults[0];
                }
            }

            return result;
        }

        private static List<O> ParallelOffsetForSource<O, T>(
            IPlineSource<T> polyline,
            T offset,
            PlineOffsetOptions<T> options,
            bool allowExternalIndex)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            StaticAABB2DIndex<T>? constructedIndex = null;
            StaticAABB2DIndex<T> index;

            if (allowExternalIndex)
            {
                if (options.AabbIndex != null)
                {
                    index = options.AabbIndex;
                }
                else
                {
                    constructedIndex = polyline.CreateApproxAabbIndex();
                    index = constructedIndex;
                }
            }
            else
            {
                constructedIndex = polyline.CreateApproxAabbIndex();
                index = constructedIndex;
            }

            var rawOffset = CreateRawOffsetPolyline<O, T>(polyline, offset, options.PosEqualEps);
            if (rawOffset.IsEmpty())
            {
                return new List<O>();
            }
            else if (polyline.IsClosed && !options.HandleSelfIntersects)
            {
                var slices = SlicesFromRawOffset(polyline, rawOffset, index, offset, options);
                return StitchSlicesTogether<O, T>(
                    rawOffset,
                    slices,
                    true,
                    rawOffset.VertexCount - 1,
                    options
                );
            }
            else
            {
                var dualRawOffset = CreateRawOffsetPolyline<O, T>(polyline, -offset, options.PosEqualEps);
                var slices = SlicesFromDualRawOffsets(
                    polyline,
                    rawOffset,
                    dualRawOffset,
                    index,
                    offset,
                    options
                );

                return StitchSlicesTogether<O, T>(
                    rawOffset,
                    slices,
                    polyline.IsClosed,
                    rawOffset.VertexCount,
                    options
                );
            }
        }

        public static List<O> ParallelOffset<O, T>(IPlineSource<T> polyline, T offset, PlineOffsetOptions<T> options)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (polyline.VertexCount < 2)
            {
                return new List<O>();
            }

            var cleaned = polyline.RemoveRepeatPos(options.PosEqualEps);
            List<O> result;

            if (cleaned != null)
            {
                if (cleaned.VertexCount < 2)
                {
                    result = new List<O>();
                }
                else
                {
                    result = ParallelOffsetForSource<O, T>(cleaned, offset, options, false);
                }
            }
            else
            {
                result = ParallelOffsetForSource<O, T>(polyline, offset, options, true);
            }

            foreach (var cursor in result)
            {
                cursor.SetUserDataValues(polyline.UserDataValues);
            }

            return result;
        }
    }
}