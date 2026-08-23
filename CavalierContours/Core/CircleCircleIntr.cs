using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    public enum CircleCircleIntrKind : byte
    {
        NoIntersect,
        TangentIntersect,
        TwoIntersects,
        Overlapping
    }

    public readonly struct CircleCircleIntr<T> : IEquatable<CircleCircleIntr<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly CircleCircleIntrKind Kind;
        public readonly Vector2<T> Point1;
        public readonly Vector2<T> Point2;

        private CircleCircleIntr(CircleCircleIntrKind kind, Vector2<T> point1, Vector2<T> point2)
        {
            Kind = kind;
            Point1 = point1;
            Point2 = point2;
        }

        public static CircleCircleIntr<T> NoIntersect => new(CircleCircleIntrKind.NoIntersect, default, default);
        public static CircleCircleIntr<T> Overlapping => new(CircleCircleIntrKind.Overlapping, default, default);
        public static CircleCircleIntr<T> TangentIntersect(Vector2<T> point) => new(CircleCircleIntrKind.TangentIntersect, point, default);
        public static CircleCircleIntr<T> TwoIntersects(Vector2<T> point1, Vector2<T> point2) => new(CircleCircleIntrKind.TwoIntersects, point1, point2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Matches Rust's derived PartialEq: IEEE 754 comparison, so NaN != NaN and 0.0 == -0.0.
        // Vector2<T>.operator == is itself component-wise IEEE 754, so this propagates.
        public bool Equals(CircleCircleIntr<T> other)
        {
            return Kind == other.Kind && Point1 == other.Point1 && Point2 == other.Point2;
        }

        public override bool Equals(object? obj) => obj is CircleCircleIntr<T> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, Point1, Point2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CircleCircleIntr<T> left, CircleCircleIntr<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CircleCircleIntr<T> left, CircleCircleIntr<T> right) => !left.Equals(right);
    }

    public static class CircleCircleIntersection
    {
        public static CircleCircleIntr<T> Intersect<T>(
            T radius1,
            Vector2<T> center1,
            T radius2,
            Vector2<T> center2,
            T epsilon)
            where T : struct, IFloatingPointIeee754<T>
        {
            Vector2<T> cv = center2 - center1;
            T d2 = cv.Dot(cv);
            T d = T.Sqrt(d2);

            if (d.FuzzyEqZero(epsilon))
            {
                if (radius1.FuzzyEq(radius2, epsilon))
                {
                    return CircleCircleIntr<T>.Overlapping;
                }
                return CircleCircleIntr<T>.NoIntersect;
            }

            if (!d.FuzzyLt(radius1 + radius2, epsilon) || !d.FuzzyGt(T.Abs(radius1 - radius2), epsilon))
            {
                return CircleCircleIntr<T>.NoIntersect;
            }

            T rad1Sq = radius1 * radius1;
            T two = T.CreateChecked(2);
            T a = (rad1Sq - radius2 * radius2 + d2) / (two * d);
            Vector2<T> midpoint = center1 + cv.Scale(a / d);
            T diff = rad1Sq - a * a;

            if (diff <= T.Zero)
            {
                return CircleCircleIntr<T>.TangentIntersect(midpoint);
            }

            T h = T.Sqrt(diff);
            T hOverD = h / d;
            T xTerm = hOverD * cv.Y;
            T yTerm = hOverD * cv.X;

            Vector2<T> pt1 = new(midpoint.X + xTerm, midpoint.Y - yTerm);
            Vector2<T> pt2 = new(midpoint.X - xTerm, midpoint.Y + yTerm);

            if (pt1.FuzzyEqEps(pt2, epsilon))
            {
                return CircleCircleIntr<T>.TangentIntersect(pt1);
            }

            return CircleCircleIntr<T>.TwoIntersects(pt1, pt2);
        }
    }
}
