using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// Discriminates the possible outcomes of intersecting two polyline segments.
    /// </summary>
    public enum PlineSegIntrKind : byte
    {
        /// <summary>
        /// The two segments do not intersect. Neither point of the result carries meaning.
        /// </summary>
        NoIntersect,

        /// <summary>
        /// The segments touch tangentially in exactly one point, held in
        /// <see cref="PlineSegIntr{T}.Point1"/>.
        /// </summary>
        TangentIntersect,

        /// <summary>
        /// The segments cross in exactly one, non-tangent point, held in
        /// <see cref="PlineSegIntr{T}.Point1"/>.
        /// </summary>
        OneIntersect,

        /// <summary>
        /// The segments cross in two distinct points, held in
        /// <see cref="PlineSegIntr{T}.Point1"/> and <see cref="PlineSegIntr{T}.Point2"/> in the
        /// direction of the second segment.
        /// </summary>
        TwoIntersects,

        /// <summary>
        /// Both segments are lines and they lie on top of each other over a stretch.
        /// <see cref="PlineSegIntr{T}.Point1"/> is the start and
        /// <see cref="PlineSegIntr{T}.Point2"/> the end of that stretch in the direction of the
        /// second segment.
        /// </summary>
        OverlappingLines,

        /// <summary>
        /// Both segments are arcs and they lie on top of each other over a stretch.
        /// <see cref="PlineSegIntr{T}.Point1"/> is the start and
        /// <see cref="PlineSegIntr{T}.Point2"/> the end of that stretch in the direction of the
        /// second segment.
        /// </summary>
        OverlappingArcs
    }

    /// <summary>
    /// Result of intersecting two polyline segments.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
    /// <remarks>
    /// Which of the two points are meaningful depends on <see cref="Kind"/>. Whenever two points
    /// are reported, their order is part of the contract: they run along the direction of the
    /// second segment (<c>u1</c> towards <c>u2</c> in
    /// <see cref="PlineSegIntersection.Intersect{T}(PlineVertex{T}, PlineVertex{T}, PlineVertex{T}, PlineVertex{T}, T)"/>).
    /// Callers that build slices out of these points rely on that order.
    /// </remarks>
    public readonly struct PlineSegIntr<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The kind of intersection found, which decides how many of the points are meaningful.
        /// </summary>
        public readonly PlineSegIntrKind Kind;

        /// <summary>
        /// The single intersect point, or the first of two points according to the direction of the
        /// second segment. Meaningless for <see cref="PlineSegIntrKind.NoIntersect"/>.
        /// </summary>
        public readonly Vector2<T> Point1;

        /// <summary>
        /// The second intersect point according to the direction of the second segment. Only
        /// meaningful for <see cref="PlineSegIntrKind.TwoIntersects"/>,
        /// <see cref="PlineSegIntrKind.OverlappingLines"/> and
        /// <see cref="PlineSegIntrKind.OverlappingArcs"/>.
        /// </summary>
        public readonly Vector2<T> Point2;

        private PlineSegIntr(PlineSegIntrKind kind, Vector2<T> point1, Vector2<T> point2)
        {
            Kind = kind;
            Point1 = point1;
            Point2 = point2;
        }

        /// <summary>
        /// Gets a result stating that the two segments do not intersect.
        /// </summary>
        public static PlineSegIntr<T> NoIntersect => new(PlineSegIntrKind.NoIntersect, default, default);

        /// <summary>
        /// Creates a result holding one tangent intersect point.
        /// </summary>
        /// <param name="point">The tangent intersect point.</param>
        /// <returns>A result of kind <see cref="PlineSegIntrKind.TangentIntersect"/>.</returns>
        public static PlineSegIntr<T> TangentIntersect(Vector2<T> point) => new(PlineSegIntrKind.TangentIntersect, point, default);

        /// <summary>
        /// Creates a result holding one non-tangent intersect point.
        /// </summary>
        /// <param name="point">The intersect point.</param>
        /// <returns>A result of kind <see cref="PlineSegIntrKind.OneIntersect"/>.</returns>
        public static PlineSegIntr<T> OneIntersect(Vector2<T> point) => new(PlineSegIntrKind.OneIntersect, point, default);

        /// <summary>
        /// Creates a result holding two intersect points.
        /// </summary>
        /// <param name="point1">First intersect point in the direction of the second segment.</param>
        /// <param name="point2">Second intersect point in the direction of the second segment.</param>
        /// <returns>A result of kind <see cref="PlineSegIntrKind.TwoIntersects"/>.</returns>
        public static PlineSegIntr<T> TwoIntersects(Vector2<T> point1, Vector2<T> point2) => new(PlineSegIntrKind.TwoIntersects, point1, point2);

        /// <summary>
        /// Creates a result describing an overlap between two line segments.
        /// </summary>
        /// <param name="point1">Start of the overlap in the direction of the second segment.</param>
        /// <param name="point2">End of the overlap in the direction of the second segment.</param>
        /// <returns>A result of kind <see cref="PlineSegIntrKind.OverlappingLines"/>.</returns>
        public static PlineSegIntr<T> OverlappingLines(Vector2<T> point1, Vector2<T> point2) => new(PlineSegIntrKind.OverlappingLines, point1, point2);

        /// <summary>
        /// Creates a result describing an overlap between two arc segments.
        /// </summary>
        /// <param name="point1">Start of the overlap in the direction of the second segment.</param>
        /// <param name="point2">End of the overlap in the direction of the second segment.</param>
        /// <returns>A result of kind <see cref="PlineSegIntrKind.OverlappingArcs"/>.</returns>
        public static PlineSegIntr<T> OverlappingArcs(Vector2<T> point1, Vector2<T> point2) => new(PlineSegIntrKind.OverlappingArcs, point1, point2);
    }

    /// <summary>
    /// Computes the intersection of two polyline segments.
    /// </summary>
    public static class PlineSegIntersection
    {
        /// <summary>
        /// Finds the intersects between the polyline segments <c>v1-&gt;v2</c> and
        /// <c>u1-&gt;u2</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
        /// <param name="v1">Start vertex of the first segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the first segment.</param>
        /// <param name="u1">Start vertex of the second segment; its bulge defines the segment shape.</param>
        /// <param name="u2">End vertex of the second segment.</param>
        /// <param name="posEqualEps">Epsilon used for the fuzzy float comparisons.</param>
        /// <returns>
        /// The intersection result. Whenever it holds two points, they are ordered along the
        /// direction of the second segment, from <paramref name="u1"/> towards
        /// <paramref name="u2"/>.
        /// </returns>
        /// <remarks>
        /// A zero bulge makes a segment a line, any other bulge makes it an arc, so the three cases
        /// line/line, line/arc and arc/arc are handled separately. In the line/arc and arc/arc
        /// cases the raw circle intersect points are snapped to segment end points that already lie
        /// on the other segment, which keeps shared end points exact instead of nearly equal.
        /// </remarks>
        public static PlineSegIntr<T> Intersect<T>(
            PlineVertex<T> v1,
            PlineVertex<T> v2,
            PlineVertex<T> u1,
            PlineVertex<T> u2,
            T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>
        {
            bool vIsLine = v1.BulgeIsZero();
            bool uIsLine = u1.BulgeIsZero();

            if (vIsLine && uIsLine)
            {
                var intrResult = LineLineIntersection.Intersect(v1.Pos(), v2.Pos(), u1.Pos(), u2.Pos(), posEqualEps);
                switch (intrResult.Kind)
                {
                    case LineLineIntrKind.NoIntersect:
                    case LineLineIntrKind.FalseIntersect:
                        return PlineSegIntr<T>.NoIntersect;
                    case LineLineIntrKind.TrueIntersect:
                        return PlineSegIntr<T>.OneIntersect(BaseMath.PointFromParametric(v1.Pos(), v2.Pos(), intrResult.Seg1T));
                    case LineLineIntrKind.Overlapping:
                        return PlineSegIntr<T>.OverlappingLines(
                            BaseMath.PointFromParametric(u1.Pos(), u2.Pos(), intrResult.OverlapSeg2T0),
                            BaseMath.PointFromParametric(u1.Pos(), u2.Pos(), intrResult.OverlapSeg2T1)
                        );
                }
            }

            PlineSegIntr<T> ProcessLineArcIntr(Vector2<T> p0, Vector2<T> p1, PlineVertex<T> a1, PlineVertex<T> a2)
            {
                (T arcRadius, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(a1, a2);

                bool PointLiesOnArc(Vector2<T> pt)
                {
                    return BaseMath.PointWithinArcSweep(arcCenter, a1.Pos(), a2.Pos(), a1.BulgeIsNeg(), pt, posEqualEps)
                        && T.Abs(T.Sqrt(BaseMath.DistSquared(pt, arcCenter)) - arcRadius) < posEqualEps;
                }

                T lineLength = (p1 - p0).Length();

                Vector2<T>? PointInSweep(T t)
                {
                    if (!(t * lineLength).FuzzyInRange(T.Zero, lineLength, posEqualEps))
                    {
                        return null;
                    }

                    Vector2<T> p = BaseMath.PointFromParametric(p0, p1, t);
                    bool withinSweep = BaseMath.PointWithinArcSweep(arcCenter, a1.Pos(), a2.Pos(), a1.BulgeIsNeg(), p, posEqualEps);
                    return withinSweep ? p : null;
                }

                var intrResult = LineCircleIntersection.Intersect(p0, p1, arcRadius, arcCenter, posEqualEps);
                switch (intrResult.Kind)
                {
                    case LineCircleIntrKind.NoIntersect:
                        return PlineSegIntr<T>.NoIntersect;
                    case LineCircleIntrKind.TangentIntersect:
                        if (PointLiesOnArc(p0)) return PlineSegIntr<T>.TangentIntersect(p0);
                        if (PointLiesOnArc(p1)) return PlineSegIntr<T>.TangentIntersect(p1);
                        var tangentPt = PointInSweep(intrResult.T0);
                        return tangentPt.HasValue ? PlineSegIntr<T>.TangentIntersect(tangentPt.Value) : PlineSegIntr<T>.NoIntersect;
                    case LineCircleIntrKind.TwoIntersects:
                        var t0Point = PointInSweep(intrResult.T0);
                        var t1Point = PointInSweep(intrResult.T1);
                        if (!t0Point.HasValue && !t1Point.HasValue) return PlineSegIntr<T>.NoIntersect;
                        if (!t0Point.HasValue || !t1Point.HasValue)
                        {
                            var point = t0Point ?? t1Point!.Value;
                            if (PointLiesOnArc(p0)) return PlineSegIntr<T>.OneIntersect(p0);
                            if (PointLiesOnArc(p1)) return PlineSegIntr<T>.OneIntersect(p1);
                            return PlineSegIntr<T>.OneIntersect(point);
                        }
                        else
                        {
                            var point1 = t0Point.Value;
                            var point2 = t1Point.Value;
                            bool liesOnArcP0 = PointLiesOnArc(p0);
                            bool liesOnArcP1 = PointLiesOnArc(p1);

                            if (liesOnArcP0 && liesOnArcP1)
                            {
                                if (BaseMath.DistSquared(p0, point1) < BaseMath.DistSquared(p0, point2))
                                {
                                    point1 = p0; point2 = p1;
                                }
                                else
                                {
                                    point1 = p1; point2 = p0;
                                }
                            }
                            else if (liesOnArcP0)
                            {
                                if (BaseMath.DistSquared(p0, point1) < BaseMath.DistSquared(p0, point2))
                                {
                                    point1 = p0;
                                }
                                else
                                {
                                    point2 = p0;
                                }
                            }
                            else if (liesOnArcP1)
                            {
                                if (BaseMath.DistSquared(p1, point1) < BaseMath.DistSquared(p1, point2))
                                {
                                    point1 = p1;
                                }
                                else
                                {
                                    point2 = p1;
                                }
                            }

                            if (uIsLine || BaseMath.DistSquared(point1, a1.Pos()) < BaseMath.DistSquared(point2, a1.Pos()))
                            {
                                return PlineSegIntr<T>.TwoIntersects(point1, point2);
                            }
                            else
                            {
                                return PlineSegIntr<T>.TwoIntersects(point2, point1);
                            }
                        }
                }
                return PlineSegIntr<T>.NoIntersect;
            }

            if (vIsLine)
            {
                return ProcessLineArcIntr(v1.Pos(), v2.Pos(), u1, u2);
            }

            if (uIsLine)
            {
                return ProcessLineArcIntr(u1.Pos(), u2.Pos(), v1, v2);
            }

            (T arc1Radius, Vector2<T> arc1Center) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
            (T arc2Radius, Vector2<T> arc2Center) = PlineSeg.SegArcRadiusAndCenter(u1, u2);

            bool BothArcsSweepPoint(Vector2<T> pt)
            {
                return BaseMath.PointWithinArcSweep(arc1Center, v1.Pos(), v2.Pos(), v1.BulgeIsNeg(), pt, posEqualEps)
                    && BaseMath.PointWithinArcSweep(arc2Center, u1.Pos(), u2.Pos(), u1.BulgeIsNeg(), pt, posEqualEps);
            }

            bool PointLiesOnArc1(Vector2<T> pt)
            {
                return BaseMath.PointWithinArcSweep(arc1Center, v1.Pos(), v2.Pos(), v1.BulgeIsNeg(), pt, posEqualEps)
                    && T.Abs(T.Sqrt(BaseMath.DistSquared(pt, arc1Center)) - arc1Radius) < posEqualEps;
            }

            bool PointLiesOnArc2(Vector2<T> pt)
            {
                return BaseMath.PointWithinArcSweep(arc2Center, u1.Pos(), u2.Pos(), u1.BulgeIsNeg(), pt, posEqualEps)
                    && T.Abs(T.Sqrt(BaseMath.DistSquared(pt, arc2Center)) - arc2Radius) < posEqualEps;
            }

            var ccIntr = CircleCircleIntersection.Intersect(arc1Radius, arc1Center, arc2Radius, arc2Center, posEqualEps);
            switch (ccIntr.Kind)
            {
                case CircleCircleIntrKind.NoIntersect:
                    return PlineSegIntr<T>.NoIntersect;
                case CircleCircleIntrKind.TangentIntersect:
                    if (PointLiesOnArc1(u1.Pos())) return PlineSegIntr<T>.TangentIntersect(u1.Pos());
                    if (PointLiesOnArc1(u2.Pos())) return PlineSegIntr<T>.TangentIntersect(u2.Pos());
                    if (PointLiesOnArc2(v1.Pos())) return PlineSegIntr<T>.TangentIntersect(v1.Pos());
                    if (PointLiesOnArc2(v2.Pos())) return PlineSegIntr<T>.TangentIntersect(v2.Pos());
                    return BothArcsSweepPoint(ccIntr.Point1) ? PlineSegIntr<T>.TangentIntersect(ccIntr.Point1) : PlineSegIntr<T>.NoIntersect;
                case CircleCircleIntrKind.TwoIntersects:
                    Vector2<T> point1 = ccIntr.Point1;
                    Vector2<T> point2 = ccIntr.Point2;

                    Vector2<T>? endPt1 = null;
                    Vector2<T>? endPt2 = null;

                    void TryAddEndPointIntr(Vector2<T> intr)
                    {
                        if (!endPt1.HasValue) endPt1 = intr;
                        else if (!endPt1.Value.FuzzyEqEps(intr, posEqualEps) && !endPt2.HasValue) endPt2 = intr;
                    }

                    if (PointLiesOnArc1(u1.Pos())) TryAddEndPointIntr(u1.Pos());
                    if (PointLiesOnArc1(u2.Pos())) TryAddEndPointIntr(u2.Pos());
                    if (PointLiesOnArc2(v1.Pos())) TryAddEndPointIntr(v1.Pos());
                    if (PointLiesOnArc2(v2.Pos())) TryAddEndPointIntr(v2.Pos());

                    bool pt1InSweep = BothArcsSweepPoint(point1);
                    bool pt2InSweep = BothArcsSweepPoint(point2);

                    if (pt1InSweep && pt2InSweep)
                    {
                        if (endPt1.HasValue && endPt2.HasValue)
                        {
                            if (BaseMath.DistSquared(endPt1.Value, point1) < BaseMath.DistSquared(endPt2.Value, point1))
                            {
                                return PlineSegIntr<T>.TwoIntersects(endPt1.Value, endPt2.Value);
                            }
                            return PlineSegIntr<T>.TwoIntersects(endPt2.Value, endPt1.Value);
                        }
                        if (endPt1.HasValue)
                        {
                            if (BaseMath.DistSquared(endPt1.Value, point1) < BaseMath.DistSquared(endPt1.Value, point2))
                            {
                                return PlineSegIntr<T>.TwoIntersects(endPt1.Value, point2);
                            }
                            return PlineSegIntr<T>.TwoIntersects(point1, endPt1.Value);
                        }
                        return PlineSegIntr<T>.TwoIntersects(point1, point2);
                    }
                    if (pt1InSweep)
                    {
                        if (endPt1.HasValue && endPt2.HasValue) return PlineSegIntr<T>.TwoIntersects(endPt1.Value, endPt2.Value);
                        if (endPt1.HasValue) return PlineSegIntr<T>.OneIntersect(endPt1.Value);
                        return PlineSegIntr<T>.OneIntersect(point1);
                    }
                    if (pt2InSweep)
                    {
                        if (endPt1.HasValue && endPt2.HasValue) return PlineSegIntr<T>.TwoIntersects(endPt1.Value, endPt2.Value);
                        if (endPt1.HasValue) return PlineSegIntr<T>.OneIntersect(endPt1.Value);
                        return PlineSegIntr<T>.OneIntersect(point2);
                    }
                    if (endPt1.HasValue && endPt2.HasValue) return PlineSegIntr<T>.TwoIntersects(endPt1.Value, endPt2.Value);
                    if (endPt1.HasValue) return PlineSegIntr<T>.OneIntersect(endPt1.Value);
                    return PlineSegIntr<T>.NoIntersect;
                case CircleCircleIntrKind.Overlapping:
                    bool sameDirectionArcs = v1.BulgeIsNeg() == u1.BulgeIsNeg();
                    T arc1Start = BaseMath.NormalizeRadians(BaseMath.Angle(arc1Center, v1.Pos()));
                    T arc1Sweep = BaseMath.AngleFromBulge(v1.Bulge);

                    T arc2Start, arc2Sweep;
                    if (sameDirectionArcs)
                    {
                        arc2Start = BaseMath.NormalizeRadians(BaseMath.Angle(arc2Center, u1.Pos()));
                        arc2Sweep = BaseMath.AngleFromBulge(u1.Bulge);
                    }
                    else
                    {
                        arc2Start = BaseMath.NormalizeRadians(BaseMath.Angle(arc2Center, u2.Pos()));
                        arc2Sweep = BaseMath.AngleFromBulge(-u1.Bulge);
                    }

                    T arc1End = arc1Start + arc1Sweep;
                    T arc2End = arc2Start + arc2Sweep;
                    T avgRadius = (arc1Radius + arc2Radius) / T.CreateChecked(2);

                    bool touchAtStartOfArc1 = (avgRadius * BaseMath.DeltaAngle(arc1Start, arc2End)).FuzzyEqZero(posEqualEps);
                    bool touchAtStartOfArc2 = (avgRadius * BaseMath.DeltaAngle(arc2Start, arc1End)).FuzzyEqZero(posEqualEps);

                    if (touchAtStartOfArc1 && touchAtStartOfArc2)
                    {
                        return PlineSegIntr<T>.TwoIntersects(u1.Pos(), u2.Pos());
                    }
                    if (touchAtStartOfArc1)
                    {
                        return PlineSegIntr<T>.OneIntersect(v1.Pos());
                    }
                    if (touchAtStartOfArc2)
                    {
                        return PlineSegIntr<T>.OneIntersect(sameDirectionArcs ? u1.Pos() : u2.Pos());
                    }

                    bool arc2StartsInArc1 = BaseMath.AngleIsWithinSweep(arc2Start, arc1Start, arc1Sweep);
                    bool arc2EndsInArc1 = BaseMath.AngleIsWithinSweep(arc2End, arc1Start, arc1Sweep);

                    if (arc2StartsInArc1 && arc2EndsInArc1)
                    {
                        return PlineSegIntr<T>.OverlappingArcs(u1.Pos(), u2.Pos());
                    }
                    if (arc2StartsInArc1)
                    {
                        return sameDirectionArcs
                            ? PlineSegIntr<T>.OverlappingArcs(u1.Pos(), v2.Pos())
                            : PlineSegIntr<T>.OverlappingArcs(v2.Pos(), u2.Pos());
                    }
                    if (arc2EndsInArc1)
                    {
                        return sameDirectionArcs
                            ? PlineSegIntr<T>.OverlappingArcs(v1.Pos(), u2.Pos())
                            : PlineSegIntr<T>.OverlappingArcs(u1.Pos(), v1.Pos());
                    }

                    bool arc1StartsInArc2 = BaseMath.AngleIsWithinSweep(arc1Start, arc2Start, arc2Sweep);
                    if (arc1StartsInArc2)
                    {
                        return sameDirectionArcs
                            ? PlineSegIntr<T>.OverlappingArcs(v1.Pos(), v2.Pos())
                            : PlineSegIntr<T>.OverlappingArcs(v2.Pos(), v1.Pos());
                    }
                    return PlineSegIntr<T>.NoIntersect;
            }
            return PlineSegIntr<T>.NoIntersect;
        }
    }
}
