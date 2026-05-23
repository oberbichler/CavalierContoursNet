using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    public static class BaseMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T Min, T Max) MinMax<T>(T v1, T v2) where T : IComparable<T>
        {
            return v1.CompareTo(v2) < 0 ? (v1, v2) : (v2, v1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T NormalizeRadians<T>(T angle) where T : struct, IFloatingPointIeee754<T>
        {
            T tau = T.Tau;
            if (angle >= T.Zero && angle <= tau)
            {
                return angle;
            }
            return angle - T.Floor(angle / tau) * tau;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeltaAngle<T>(T angle1, T angle2) where T : struct, IFloatingPointIeee754<T>
        {
            T diff = NormalizeRadians(angle2 - angle1);
            if (diff > T.Pi)
            {
                diff -= T.Tau;
            }
            return diff;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DeltaAngleSigned<T>(T angle1, T angle2, bool negative) where T : struct, IFloatingPointIeee754<T>
        {
            T diff = DeltaAngle(angle1, angle2);
            return negative ? -T.Abs(diff) : T.Abs(diff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsBetweenEps<T>(T testAngle, T startAngle, T endAngle, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            T endSweep = NormalizeRadians(endAngle - startAngle);
            T midSweep = NormalizeRadians(testAngle - startAngle);
            return midSweep < endSweep + epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsBetween<T>(T testAngle, T startAngle, T endAngle) where T : struct, IFloatingPointIeee754<T>
        {
            return AngleIsBetweenEps(testAngle, startAngle, endAngle, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsWithinSweepEps<T>(T testAngle, T startAngle, T sweepAngle, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            T endAngle = startAngle + sweepAngle;
            if (sweepAngle < T.Zero)
            {
                return AngleIsBetweenEps(testAngle, endAngle, startAngle, epsilon);
            }
            return AngleIsBetweenEps(testAngle, startAngle, endAngle, epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AngleIsWithinSweep<T>(T testAngle, T startAngle, T sweepAngle) where T : struct, IFloatingPointIeee754<T>
        {
            return AngleIsWithinSweepEps(testAngle, startAngle, sweepAngle, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (T, T) QuadraticSolutions<T>(T a, T b, T c, T sqrtDiscriminant) where T : struct, IFloatingPointIeee754<T>
        {
            T four = T.CreateChecked(4);
            T two = T.CreateChecked(2);
            Debug.Assert(T.Abs((b * b - four * a * c)) < T.CreateChecked(1e-5), "discriminant is not valid");
            
            T denom = two * a;
            T sol1 = b < T.Zero ? (-b + sqrtDiscriminant) / denom : (-b - sqrtDiscriminant) / denom;
            T sol2 = (c / a) / sol1;
            return (sol1, sol2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T DistSquared<T>(Vector2<T> p0, Vector2<T> p1) where T : struct, IFloatingPointIeee754<T>
        {
            Vector2<T> d = p0 - p1;
            return d.Dot(d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Angle<T>(Vector2<T> p0, Vector2<T> p1) where T : struct, IFloatingPointIeee754<T>
        {
            return T.Atan2(p1.Y - p0.Y, p1.X - p0.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> Midpoint<T>(Vector2<T> p0, Vector2<T> p1) where T : struct, IFloatingPointIeee754<T>
        {
            T two = T.CreateChecked(2);
            return new Vector2<T>((p0.X + p1.X) / two, (p0.Y + p1.Y) / two);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> PointOnCircle<T>(T radius, Vector2<T> center, T angle) where T : struct, IFloatingPointIeee754<T>
        {
            (T s, T c) = T.SinCos(angle);
            return new Vector2<T>(center.X + radius * c, center.Y + radius * s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> PointFromParametric<T>(Vector2<T> p0, Vector2<T> p1, T t) where T : struct, IFloatingPointIeee754<T>
        {
            return p0 + (p1 - p0).Scale(t);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ParametricFromPoint<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            T xDiff = p1.X - p0.X;
            T yDiff = p1.Y - p0.Y;

            Debug.Assert(
                ((xDiff * (p0.Y - point.Y) - (p0.X - point.X) * yDiff) / T.Sqrt(xDiff * xDiff + yDiff * yDiff))
                .FuzzyEqZero(epsilon),
                "point does not lie on the line defined by p0 to p1"
            );

            if (T.Abs(xDiff) < T.Abs(yDiff))
            {
                return (point.Y - p0.Y) / yDiff;
            }
            else
            {
                return (point.X - p0.X) / xDiff;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> LineSegClosestPoint<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            Vector2<T> v = p1 - p0;
            Vector2<T> w = point - p0;
            T c1 = w.Dot(v);
            if (c1 < Fuzzy<T>.Epsilon)
            {
                return p0;
            }

            T c2 = v.LengthSquared();
            if (c2 < c1 + Fuzzy<T>.Epsilon)
            {
                return p1;
            }

            T b = c1 / c2;
            return p0 + v.Scale(b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T PerpDotTestValue<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return (p1.X - p0.X) * (point.Y - p0.Y) - (p1.Y - p0.Y) * (point.X - p0.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return PerpDotTestValue(p0, p1, point) > T.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftOrEqual<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return PerpDotTestValue(p0, p1, point) >= T.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftOrCoincidentEps<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            Debug.Assert(epsilon > T.Zero);
            return PerpDotTestValue(p0, p1, point) > -epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeftOrCoincident<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return IsLeftOrCoincidentEps(p0, p1, point, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRightOrCoincidentEps<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            Debug.Assert(epsilon > T.Zero);
            return PerpDotTestValue(p0, p1, point) < epsilon;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRightOrCoincident<T>(Vector2<T> p0, Vector2<T> p1, Vector2<T> point) where T : struct, IFloatingPointIeee754<T>
        {
            return IsRightOrCoincidentEps(p0, p1, point, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointWithinArcSweep<T>(Vector2<T> center, Vector2<T> arcStart, Vector2<T> arcEnd, bool isClockwise, Vector2<T> point, T epsilon) where T : struct, IFloatingPointIeee754<T>
        {
            if (isClockwise)
            {
                return IsRightOrCoincidentEps(center, arcStart, point, epsilon) && IsLeftOrCoincidentEps(center, arcEnd, point, epsilon);
            }
            else
            {
                return IsLeftOrCoincidentEps(center, arcStart, point, epsilon) && IsRightOrCoincidentEps(center, arcEnd, point, epsilon);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T BulgeFromAngle<T>(T angle) where T : struct, IFloatingPointIeee754<T>
        {
            T four = T.CreateChecked(4);
            return T.Tan(angle / four);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AngleFromBulge<T>(T bulge) where T : struct, IFloatingPointIeee754<T>
        {
            T four = T.CreateChecked(4);
            return four * T.Atan(bulge);
        }
    }
}
