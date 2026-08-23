using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// Result of splitting a polyline segment, see
    /// <see cref="PlineSeg.SegSplitAtPoint{T}(PlineVertex{T}, PlineVertex{T}, Vector2{T}, T)"/>.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
    public readonly struct SplitResult<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Updated start vertex: same position as the start of the segment, but with the bulge
        /// trimmed so that the segment now ends at the split point.
        /// </summary>
        public readonly PlineVertex<T> UpdatedStart;

        /// <summary>
        /// Vertex at the split point: its position equals the split point and its bulge is set so
        /// that the remaining curve to the original end vertex is unchanged.
        /// </summary>
        public readonly PlineVertex<T> SplitVertex;

        /// <summary>Constructs a split result from its two vertexes.</summary>
        /// <param name="updatedStart">Updated start vertex, see <see cref="UpdatedStart"/>.</param>
        /// <param name="splitVertex">Vertex at the split point, see <see cref="SplitVertex"/>.</param>
        public SplitResult(PlineVertex<T> updatedStart, PlineVertex<T> splitVertex)
        {
            UpdatedStart = updatedStart;
            SplitVertex = splitVertex;
        }
    }

    /// <summary>
    /// Geometric operations on a single polyline segment, which is defined by an ordered pair of
    /// vertexes <c>v1</c> and <c>v2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bulge of <c>v1</c> alone decides the shape of the segment: if it is fuzzy zero the
    /// segment is the straight line from <c>v1</c> to <c>v2</c>, otherwise it is a circular arc
    /// from <c>v1</c> to <c>v2</c> sweeping <c>4 * atan(v1.Bulge)</c>, counter-clockwise for a
    /// positive bulge and clockwise for a negative one. The bulge of <c>v2</c> is never read.
    /// </para>
    /// </remarks>
    public static class PlineSeg
    {
        /// <summary>
        /// Gets the radius and center of the arc segment defined by <paramref name="v1"/> to
        /// <paramref name="v2"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Arc segments only. The behaviour is undefined if <c>v1.Bulge</c> is zero or if
        /// <paramref name="v1"/> lies on top of <paramref name="v2"/>: both cases divide by zero
        /// and produce infinities or NaN. Debug builds assert on them, release builds do not.
        /// </para>
        /// <para>
        /// The center is placed to the left of the chord for a positive bulge and to the right for
        /// a negative one, so the returned center is consistent with the segment's sweep direction.
        /// The radius itself is always non-negative and independent of the bulge sign.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the arc.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <returns>The arc radius and the arc center point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T Radius, Vector2<T> Center) SegArcRadiusAndCenter<T>(PlineVertex<T> v1, PlineVertex<T> v2)
            where T : struct, IFloatingPointIeee754<T>
        {
            Debug.Assert(!v1.BulgeIsZero(), "v1 to v2 must be an arc");
            Debug.Assert(!v1.Pos().FuzzyEq(v2.Pos()), "v1 must not be on top of v2");

            T absBulge = T.Abs(v1.Bulge);
            Vector2<T> chordV = v2.Pos() - v1.Pos();
            T chordLen = chordV.Length();
            T four = T.CreateChecked(4);
            T two = T.CreateChecked(2);
            T radius = chordLen * (absBulge * absBulge + T.One) / (four * absBulge);

            T s = absBulge * chordLen / two;
            T m = radius - s;
            T offsX = -m * chordV.Y / chordLen;
            T offsY = m * chordV.X / chordLen;
            if (v1.BulgeIsNeg())
            {
                offsX = -offsX;
                offsY = -offsY;
            }

            Vector2<T> center = new(
                v1.X + chordV.X / two + offsX,
                v1.Y + chordV.Y / two + offsY
            );

            return (radius, center);
        }

        /// <summary>
        /// Splits the polyline segment defined by <paramref name="v1"/> to <paramref name="v2"/> at
        /// <paramref name="pointOnSeg"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Handles both line and arc segments. For a line segment the start vertex is returned
        /// unchanged (its bulge stays zero) and the split vertex is <paramref name="pointOnSeg"/>
        /// with zero bulge. For an arc segment the sweep angle is divided at the split point and
        /// each half is converted back into a bulge, keeping the original sweep direction, so the
        /// two resulting segments trace exactly the same arc.
        /// </para>
        /// <para>
        /// The point is assumed to lie on the segment; no check is performed. Three degenerate
        /// cases are short-circuited: if <paramref name="v1"/> coincides with
        /// <paramref name="v2"/> or with <paramref name="pointOnSeg"/>, the updated start is placed
        /// on top of the split vertex and carries a zero bulge while the split vertex keeps the
        /// original bulge; if <paramref name="pointOnSeg"/> coincides with <paramref name="v2"/>,
        /// the start vertex is returned unchanged and the split vertex is <paramref name="v2"/>
        /// with zero bulge.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <param name="pointOnSeg">Point on the segment at which to split.</param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons described above.</param>
        /// <returns>
        /// The updated start vertex and the vertex at the split point, see
        /// <see cref="SplitResult{T}"/>.
        /// </returns>
        public static SplitResult<T> SegSplitAtPoint<T>(PlineVertex<T> v1, PlineVertex<T> v2, Vector2<T> pointOnSeg, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>
        {
            if (v1.BulgeIsZero())
            {
                var updatedStart = v1;
                var splitVertex = new PlineVertex<T>(pointOnSeg.X, pointOnSeg.Y, T.Zero);
                return new SplitResult<T>(updatedStart, splitVertex);
            }

            if (v1.Pos().FuzzyEqEps(v2.Pos(), posEqualEps) || v1.Pos().FuzzyEqEps(pointOnSeg, posEqualEps))
            {
                var updatedStart = new PlineVertex<T>(pointOnSeg.X, pointOnSeg.Y, T.Zero);
                var splitVertex = new PlineVertex<T>(pointOnSeg.X, pointOnSeg.Y, v1.Bulge);
                return new SplitResult<T>(updatedStart, splitVertex);
            }

            if (v2.Pos().FuzzyEqEps(pointOnSeg, posEqualEps))
            {
                var updatedStart = v1;
                var splitVertex = new PlineVertex<T>(v2.X, v2.Y, T.Zero);
                return new SplitResult<T>(updatedStart, splitVertex);
            }

            (_, Vector2<T> arcCenter) = SegArcRadiusAndCenter(v1, v2);

            T pointPosAngle = BaseMath.Angle(arcCenter, pointOnSeg);
            T arcStartAngle = BaseMath.Angle(arcCenter, v1.Pos());
            T theta1 = BaseMath.DeltaAngleSigned(arcStartAngle, pointPosAngle, v1.BulgeIsNeg());
            T bulge1 = BaseMath.BulgeFromAngle(theta1);

            T arcEndAngle = BaseMath.Angle(arcCenter, v2.Pos());
            T theta2 = BaseMath.DeltaAngleSigned(pointPosAngle, arcEndAngle, v1.BulgeIsNeg());
            T bulge2 = BaseMath.BulgeFromAngle(theta2);

            return new SplitResult<T>(
                new PlineVertex<T>(v1.X, v1.Y, bulge1),
                new PlineVertex<T>(pointOnSeg.X, pointOnSeg.Y, bulge2)
            );
        }

        /// <summary>
        /// Finds the tangent direction vector on the polyline segment defined by
        /// <paramref name="v1"/> to <paramref name="v2"/> at <paramref name="pointOnSeg"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Handles both line and arc segments. For a line segment the chord vector
        /// <c>v2 - v1</c> is returned and <paramref name="pointOnSeg"/> is ignored. For an arc the
        /// vector from the arc center to <paramref name="pointOnSeg"/> is rotated by 90 degrees,
        /// counter-clockwise for a positive bulge and clockwise for a negative one, so the tangent
        /// always points along the direction of travel.
        /// </para>
        /// <para>
        /// The result is <em>not</em> normalized and it is a direction only; add
        /// <paramref name="pointOnSeg"/> if a positioned vector is needed.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <param name="pointOnSeg">Point on the segment at which the tangent is evaluated.</param>
        /// <returns>The unnormalized tangent direction vector.</returns>
        public static Vector2<T> SegTangentVector<T>(PlineVertex<T> v1, PlineVertex<T> v2, Vector2<T> pointOnSeg)
            where T : struct, IFloatingPointIeee754<T>
        {
            if (v1.BulgeIsZero())
            {
                return v2.Pos() - v1.Pos();
            }

            (_, Vector2<T> arcCenter) = SegArcRadiusAndCenter(v1, v2);
            if (v1.BulgeIsPos())
            {
                return new Vector2<T>(
                    -(pointOnSeg.Y - arcCenter.Y),
                    pointOnSeg.X - arcCenter.X
                );
            }

            return new Vector2<T>(
                pointOnSeg.Y - arcCenter.Y,
                -(pointOnSeg.X - arcCenter.X)
            );
        }

        /// <summary>
        /// Finds the point on the polyline segment defined by <paramref name="v1"/> to
        /// <paramref name="v2"/> that is closest to <paramref name="point"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Handles both line and arc segments. For a line segment the closest point on the finite
        /// line segment is returned. For an arc, if <paramref name="point"/> projects into the
        /// arc's sweep the projection onto the circle is returned, otherwise the nearer of the two
        /// end vertexes is returned; the sweep test uses the bulge sign to know which of the two
        /// circle arcs between the vertexes the segment actually is.
        /// </para>
        /// <para>
        /// If <paramref name="point"/> is at the arc center the start vertex is returned to avoid
        /// normalizing a zero length vector. If several points are equally close, which one is
        /// returned is unspecified.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <param name="point">Point to find the closest segment point for.</param>
        /// <param name="epsilon">Epsilon used for the fuzzy float comparisons.</param>
        /// <returns>The closest point on the segment.</returns>
        public static Vector2<T> SegClosestPoint<T>(PlineVertex<T> v1, PlineVertex<T> v2, Vector2<T> point, T epsilon)
            where T : struct, IFloatingPointIeee754<T>
        {
            if (v1.BulgeIsZero())
            {
                return BaseMath.LineSegClosestPoint(v1.Pos(), v2.Pos(), point);
            }

            (T arcRadius, Vector2<T> arcCenter) = SegArcRadiusAndCenter(v1, v2);
            if (point.FuzzyEqEps(arcCenter, epsilon))
            {
                return v1.Pos();
            }

            if (BaseMath.PointWithinArcSweep(arcCenter, v1.Pos(), v2.Pos(), v1.BulgeIsNeg(), point, epsilon))
            {
                Vector2<T> vToPoint = (point - arcCenter).Normalize();
                return vToPoint.Scale(arcRadius) + arcCenter;
            }

            T dist1 = BaseMath.DistSquared(v1.Pos(), point);
            T dist2 = BaseMath.DistSquared(v2.Pos(), point);
            return dist1 < dist2 ? v1.Pos() : v2.Pos();
        }

        /// <summary>
        /// Computes a fast approximate axis aligned bounding box of the polyline segment defined by
        /// <paramref name="v1"/> to <paramref name="v2"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Handles both line and arc segments. For a line segment the box is exact. For an arc the
        /// box is the union of the chord's box and the chord translated by the sagitta offset,
        /// which is derived directly from the bulge; this is cheaper than computing the true
        /// extents but the box may be larger than the true one. It is never smaller.
        /// </para>
        /// <para>
        /// Use
        /// <see cref="SegBoundingBox{T}(PlineVertex{T}, PlineVertex{T})"/> when the exact box is
        /// required.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <returns>An axis aligned bounding box that contains the segment.</returns>
        public static AABB<T> SegFastApproxBoundingBox<T>(PlineVertex<T> v1, PlineVertex<T> v2)
            where T : struct, IFloatingPointIeee754<T>
        {
            T two = T.CreateChecked(2);
            if (v1.BulgeIsZero())
            {
                (T xMin, T xMax) = BaseMath.MinMax(v1.X, v2.X);
                (T yMin, T yMax) = BaseMath.MinMax(v1.Y, v2.Y);
                return new AABB<T>(xMin, yMin, xMax, yMax);
            }

            T b = v1.Bulge;
            T offsX = b * (v2.Y - v1.Y) / two;
            T offsY = -b * (v2.X - v1.X) / two;

            (T ptXMin, T ptXMax) = BaseMath.MinMax(v1.X + offsX, v2.X + offsX);
            (T ptYMin, T ptYMax) = BaseMath.MinMax(v1.Y + offsY, v2.Y + offsY);

            (T endPointXMin, T endPointXMax) = BaseMath.MinMax(v1.X, v2.X);
            (T endPointYMin, T endPointYMax) = BaseMath.MinMax(v1.Y, v2.Y);

            T minX = T.Min(endPointXMin, ptXMin);
            T minY = T.Min(endPointYMin, ptYMin);
            T maxX = T.Max(endPointXMax, ptXMax);
            T maxY = T.Max(endPointYMax, ptYMax);

            return new AABB<T>(minX, minY, maxX, maxY);
        }

        internal static AABB<T> ArcSegBoundingBox<T>(PlineVertex<T> v1, PlineVertex<T> v2)
            where T : struct, IFloatingPointIeee754<T>
        {
            Debug.Assert(!v1.BulgeIsZero(), "expected arc");

            if (v1.Pos().FuzzyEq(v2.Pos()))
            {
                return new AABB<T>(v1.X, v1.Y, v1.X, v1.Y);
            }

            (T arcRadius, Vector2<T> arcCenter) = SegArcRadiusAndCenter(v1, v2);
            T startAngle = BaseMath.Angle(arcCenter, v1.Pos());
            T endAngle = BaseMath.Angle(arcCenter, v2.Pos());
            T sweepAngle = BaseMath.DeltaAngleSigned(startAngle, endAngle, v1.BulgeIsNeg());

            bool CrossesAngle(T angle) => BaseMath.AngleIsWithinSweep(angle, startAngle, sweepAngle);

            T minX = CrossesAngle(T.Pi) ? arcCenter.X - arcRadius : T.Min(v1.X, v2.X);
            T minY = CrossesAngle(T.CreateChecked(1.5) * T.Pi) ? arcCenter.Y - arcRadius : T.Min(v1.Y, v2.Y);
            T maxX = CrossesAngle(T.Zero) ? arcCenter.X + arcRadius : T.Max(v1.X, v2.X);
            T maxY = CrossesAngle(T.CreateChecked(0.5) * T.Pi) ? arcCenter.Y + arcRadius : T.Max(v1.Y, v2.Y);

            return new AABB<T>(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Computes the exact axis aligned bounding box of the polyline segment defined by
        /// <paramref name="v1"/> to <paramref name="v2"/>.
        /// </summary>
        /// <remarks>
        /// Handles both line and arc segments. For a line segment the box spans the two end points.
        /// For an arc the box additionally accounts for every axis extreme of the circle that the
        /// sweep actually crosses, which is determined from the bulge sign and the start and end
        /// angles. This is noticeably slower than
        /// <see cref="SegFastApproxBoundingBox{T}(PlineVertex{T}, PlineVertex{T})"/> for arcs.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <returns>The exact axis aligned bounding box of the segment.</returns>
        public static AABB<T> SegBoundingBox<T>(PlineVertex<T> v1, PlineVertex<T> v2)
            where T : struct, IFloatingPointIeee754<T>
        {
            if (v1.BulgeIsZero())
            {
                (T xMin, T xMax) = BaseMath.MinMax(v1.X, v2.X);
                (T yMin, T yMax) = BaseMath.MinMax(v1.Y, v2.Y);
                return new AABB<T>(xMin, yMin, xMax, yMax);
            }
            return ArcSegBoundingBox(v1, v2);
        }

        /// <summary>
        /// Calculates the path length of the polyline segment defined by <paramref name="v1"/> to
        /// <paramref name="v2"/>.
        /// </summary>
        /// <remarks>
        /// Handles both line and arc segments. For a line segment this is the distance between the
        /// two positions, for an arc it is <c>radius * |sweepAngle|</c>, with the sweep taken from
        /// the bulge. Zero is returned if the two vertexes are fuzzy equal, including their bulges.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <returns>The length of the segment, always non-negative.</returns>
        public static T SegLength<T>(PlineVertex<T> v1, PlineVertex<T> v2)
            where T : struct, IFloatingPointIeee754<T>
        {
            if (v1.FuzzyEq(v2))
            {
                return T.Zero;
            }

            if (v1.BulgeIsZero())
            {
                return T.Sqrt(BaseMath.DistSquared(v1.Pos(), v2.Pos()));
            }

            (T arcRadius, Vector2<T> arcCenter) = SegArcRadiusAndCenter(v1, v2);
            T startAngle = BaseMath.Angle(arcCenter, v1.Pos());
            T endAngle = BaseMath.Angle(arcCenter, v2.Pos());
            return arcRadius * T.Abs(BaseMath.DeltaAngle(startAngle, endAngle));
        }

        /// <summary>
        /// Finds the midpoint of the polyline segment defined by <paramref name="v1"/> to
        /// <paramref name="v2"/>.
        /// </summary>
        /// <remarks>
        /// Handles both line and arc segments. For a line segment this is the average of the two
        /// positions. For an arc it is the point at half the sweep angle along the arc, so the
        /// bulge sign decides on which side of the chord the midpoint lies; it is a midpoint by
        /// arc length, not by chord length.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="v1">Start vertex of the segment; its bulge defines the segment shape.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <returns>The midpoint of the segment.</returns>
        public static Vector2<T> SegMidpoint<T>(PlineVertex<T> v1, PlineVertex<T> v2)
            where T : struct, IFloatingPointIeee754<T>
        {
            if (v1.BulgeIsZero())
            {
                return BaseMath.Midpoint(v1.Pos(), v2.Pos());
            }

            (T arcRadius, Vector2<T> arcCenter) = SegArcRadiusAndCenter(v1, v2);
            T angle1 = BaseMath.Angle(arcCenter, v1.Pos());
            T angle2 = BaseMath.Angle(arcCenter, v2.Pos());
            T two = T.CreateChecked(2);
            T angleOffset = BaseMath.DeltaAngleSigned(angle1, angle2, v1.BulgeIsNeg()) / two;
            T midAngle = angle1 + angleOffset;
            return BaseMath.PointOnCircle(arcRadius, arcCenter, midAngle);
        }
    }
}
