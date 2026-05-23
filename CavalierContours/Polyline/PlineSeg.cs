using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    public readonly struct SplitResult<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly PlineVertex<T> UpdatedStart;
        public readonly PlineVertex<T> SplitVertex;

        public SplitResult(PlineVertex<T> updatedStart, PlineVertex<T> splitVertex)
        {
            UpdatedStart = updatedStart;
            SplitVertex = splitVertex;
        }
    }

    public static class PlineSeg
    {
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
