using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    /// <summary>
    /// Discriminates the result cases of finding the intersect between two circles.
    /// </summary>
    public enum CircleCircleIntrKind : byte
    {
        /// <summary>
        /// No intersects found: the circles are concentric with different radii, or the distance
        /// between the centers is too large (circles apart) or too small (one circle inside the
        /// other) relative to the radii for intersects to occur.
        /// </summary>
        NoIntersect,

        /// <summary>
        /// One tangent intersect point found: the circles touch at a single point.
        /// <see cref="CircleCircleIntr{T}.Point1"/> holds the tangent intersect point;
        /// <see cref="CircleCircleIntr{T}.Point2"/> is unused.
        /// </summary>
        TangentIntersect,

        /// <summary>
        /// Simple case of two intersect points found: the circle outlines cross.
        /// <see cref="CircleCircleIntr{T}.Point1"/> holds the first intersect point and
        /// <see cref="CircleCircleIntr{T}.Point2"/> the second.
        /// </summary>
        TwoIntersects,

        /// <summary>
        /// The circles overlap each other, i.e. they are the same circle: centers fuzzy coincident
        /// and radii fuzzy equal. Neither point field is used.
        /// </summary>
        Overlapping
    }

    /// <summary>
    /// Holds the result of finding the intersect between two circles.
    /// </summary>
    /// <typeparam name="T">Floating point type of the point components.</typeparam>
    /// <remarks>
    /// Which fields are meaningful depends on <see cref="Kind"/>; see the individual
    /// <see cref="CircleCircleIntrKind"/> members.
    /// </remarks>
    public readonly struct CircleCircleIntr<T> : IEquatable<CircleCircleIntr<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The result case this value represents.
        /// </summary>
        public readonly CircleCircleIntrKind Kind;

        /// <summary>
        /// Holds the tangent intersect point for
        /// <see cref="CircleCircleIntrKind.TangentIntersect"/>, or the first intersect point for
        /// <see cref="CircleCircleIntrKind.TwoIntersects"/>. Unused for the other kinds.
        /// </summary>
        public readonly Vector2<T> Point1;

        /// <summary>
        /// Holds the second intersect point. Meaningful only for
        /// <see cref="CircleCircleIntrKind.TwoIntersects"/>.
        /// </summary>
        public readonly Vector2<T> Point2;

        private CircleCircleIntr(CircleCircleIntrKind kind, Vector2<T> point1, Vector2<T> point2)
        {
            Kind = kind;
            Point1 = point1;
            Point2 = point2;
        }

        /// <summary>
        /// Gets a result representing <see cref="CircleCircleIntrKind.NoIntersect"/>.
        /// </summary>
        public static CircleCircleIntr<T> NoIntersect => new(CircleCircleIntrKind.NoIntersect, default, default);

        /// <summary>
        /// Gets a result representing <see cref="CircleCircleIntrKind.Overlapping"/>, i.e. the two
        /// circles are the same circle.
        /// </summary>
        public static CircleCircleIntr<T> Overlapping => new(CircleCircleIntrKind.Overlapping, default, default);

        /// <summary>
        /// Creates a <see cref="CircleCircleIntrKind.TangentIntersect"/> result.
        /// </summary>
        /// <param name="point">The tangent intersect point.</param>
        /// <returns>The tangent intersect result.</returns>
        public static CircleCircleIntr<T> TangentIntersect(Vector2<T> point) => new(CircleCircleIntrKind.TangentIntersect, point, default);

        /// <summary>
        /// Creates a <see cref="CircleCircleIntrKind.TwoIntersects"/> result.
        /// </summary>
        /// <param name="point1">The first intersect point.</param>
        /// <param name="point2">The second intersect point.</param>
        /// <returns>The two intersects result.</returns>
        public static CircleCircleIntr<T> TwoIntersects(Vector2<T> point1, Vector2<T> point2) => new(CircleCircleIntrKind.TwoIntersects, point1, point2);

        /// <summary>
        /// Exact field-wise equality comparison.
        /// </summary>
        /// <param name="other">The result to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if the kind and both points compare exactly equal.
        /// </returns>
        /// <remarks>
        /// This is an IEEE 754 comparison matching Rust's derived <c>PartialEq</c>: <c>NaN</c> is
        /// not equal to itself and <c>0.0</c> equals <c>-0.0</c>. The
        /// <see cref="Vector2{T}"/> equality operator is itself component-wise IEEE 754, so this
        /// propagates.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Matches Rust's derived PartialEq: IEEE 754 comparison, so NaN != NaN and 0.0 == -0.0.
        // Vector2<T>.operator == is itself component-wise IEEE 754, so this propagates.
        public bool Equals(CircleCircleIntr<T> other)
        {
            return Kind == other.Kind && Point1 == other.Point1 && Point2 == other.Point2;
        }

        /// <summary>
        /// Exact field-wise equality comparison against a boxed value.
        /// </summary>
        /// <param name="obj">The value to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="CircleCircleIntr{T}"/>
        /// that compares equal per <see cref="Equals(CircleCircleIntr{T})"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is CircleCircleIntr<T> other && Equals(other);

        /// <summary>
        /// Returns a hash code combining the kind and both points.
        /// </summary>
        /// <returns>The hash code for this result.</returns>
        public override int GetHashCode() => HashCode.Combine(Kind, Point1, Point2);

        /// <summary>
        /// Exact field-wise equality comparison, see <see cref="Equals(CircleCircleIntr{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the results compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CircleCircleIntr<T> left, CircleCircleIntr<T> right) => left.Equals(right);

        /// <summary>
        /// Exact field-wise inequality comparison, see
        /// <see cref="Equals(CircleCircleIntr{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the results do not compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CircleCircleIntr<T> left, CircleCircleIntr<T> right) => !left.Equals(right);
    }

    /// <summary>
    /// Finds the intersects between two circles defined by the radius and center.
    /// </summary>
    public static class CircleCircleIntersection
    {
        /// <summary>
        /// Finds the intersects between the two circles defined by
        /// <paramref name="radius1"/>/<paramref name="center1"/> and
        /// <paramref name="radius2"/>/<paramref name="center2"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="radius1">Radius of the first circle.</param>
        /// <param name="center1">Center of the first circle.</param>
        /// <param name="radius2">Radius of the second circle.</param>
        /// <param name="center2">Center of the second circle.</param>
        /// <param name="epsilon">Tolerance used for fuzzy float comparisons.</param>
        /// <returns>
        /// The geometric solution: <see cref="CircleCircleIntrKind.NoIntersect"/> if the circles
        /// are too far apart, <see cref="CircleCircleIntrKind.Overlapping"/> if the circles are
        /// similar in radii and center, and otherwise either
        /// <see cref="CircleCircleIntrKind.TangentIntersect"/> with a single intersection point or
        /// <see cref="CircleCircleIntrKind.TwoIntersects"/> with two intersection points.
        /// </returns>
        /// <remarks>
        /// Reference algorithm: <c>http://paulbourke.net/geometry/circlesphere/</c>.
        /// </remarks>
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

            if (diff < T.Zero)
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
