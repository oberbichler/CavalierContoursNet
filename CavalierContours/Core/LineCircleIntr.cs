using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    /// <summary>
    /// Discriminates the result cases of finding the intersect between a line segment and a circle.
    /// </summary>
    public enum LineCircleIntrKind : byte
    {
        /// <summary>
        /// No intersects found: the line misses the circle by more than the epsilon, or the
        /// degenerate segment (a single point) is not on the circle.
        /// </summary>
        NoIntersect,

        /// <summary>
        /// One tangent intersect point found: the line touches the circle, or the two candidate
        /// intersect points are fuzzy equal in position, or a degenerate segment lies on the
        /// circle. <see cref="LineCircleIntr{T}.T0"/> holds the line segment parametric value for
        /// where the intersect point is; <see cref="LineCircleIntr{T}.T1"/> is unused.
        /// </summary>
        TangentIntersect,

        /// <summary>
        /// Simple case of two intersect points found: the line passes through the circle.
        /// <see cref="LineCircleIntr{T}.T0"/> and <see cref="LineCircleIntr{T}.T1"/> hold the line
        /// segment parametric values for the first and second intersect point, ordered ascending.
        /// </summary>
        TwoIntersects
    }

    /// <summary>
    /// Holds the result of finding the intersect between a line segment and a circle.
    /// </summary>
    /// <typeparam name="T">Floating point type of the parametric values.</typeparam>
    /// <remarks>
    /// Which fields are meaningful depends on <see cref="Kind"/>; see the individual
    /// <see cref="LineCircleIntrKind"/> members.
    /// </remarks>
    public readonly struct LineCircleIntr<T> : IEquatable<LineCircleIntr<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The result case this value represents.
        /// </summary>
        public readonly LineCircleIntrKind Kind;

        /// <summary>
        /// Holds the line segment parametric value for where the (first) intersect point is.
        /// Unused for <see cref="LineCircleIntrKind.NoIntersect"/>.
        /// </summary>
        public readonly T T0;

        /// <summary>
        /// Holds the line segment parametric value for where the second intersect point is.
        /// Meaningful only for <see cref="LineCircleIntrKind.TwoIntersects"/>.
        /// </summary>
        public readonly T T1;

        private LineCircleIntr(LineCircleIntrKind kind, T t0, T t1)
        {
            Kind = kind;
            T0 = t0;
            T1 = t1;
        }

        /// <summary>
        /// Gets a result representing <see cref="LineCircleIntrKind.NoIntersect"/>.
        /// </summary>
        public static LineCircleIntr<T> NoIntersect => new(LineCircleIntrKind.NoIntersect, default, default);

        /// <summary>
        /// Creates a <see cref="LineCircleIntrKind.TangentIntersect"/> result.
        /// </summary>
        /// <param name="t0">
        /// The line segment parametric value for where the tangent intersect point is.
        /// </param>
        /// <returns>The tangent intersect result.</returns>
        public static LineCircleIntr<T> TangentIntersect(T t0) => new(LineCircleIntrKind.TangentIntersect, t0, default);

        /// <summary>
        /// Creates a <see cref="LineCircleIntrKind.TwoIntersects"/> result.
        /// </summary>
        /// <param name="t0">
        /// The line segment parametric value for where the first intersect point is.
        /// </param>
        /// <param name="t1">
        /// The line segment parametric value for where the second intersect point is.
        /// </param>
        /// <returns>The two intersects result.</returns>
        public static LineCircleIntr<T> TwoIntersects(T t0, T t1) => new(LineCircleIntrKind.TwoIntersects, t0, t1);

        /// <summary>
        /// Exact field-wise equality comparison.
        /// </summary>
        /// <param name="other">The result to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if the kind and both parametric values compare exactly equal.
        /// </returns>
        /// <remarks>
        /// This is an IEEE 754 comparison matching Rust's derived <c>PartialEq</c>: <c>NaN</c> is
        /// not equal to itself and <c>0.0</c> equals <c>-0.0</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Matches Rust's derived PartialEq: IEEE 754 comparison, so NaN != NaN and 0.0 == -0.0.
        // T.Equals would treat NaN as equal to itself.
        public bool Equals(LineCircleIntr<T> other)
        {
            return Kind == other.Kind && T0 == other.T0 && T1 == other.T1;
        }

        /// <summary>
        /// Exact field-wise equality comparison against a boxed value.
        /// </summary>
        /// <param name="obj">The value to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="LineCircleIntr{T}"/>
        /// that compares equal per <see cref="Equals(LineCircleIntr{T})"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is LineCircleIntr<T> other && Equals(other);

        /// <summary>
        /// Returns a hash code combining the kind and both parametric values.
        /// </summary>
        /// <returns>The hash code for this result.</returns>
        public override int GetHashCode() => HashCode.Combine(Kind, T0, T1);

        /// <summary>
        /// Exact field-wise equality comparison, see <see cref="Equals(LineCircleIntr{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the results compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(LineCircleIntr<T> left, LineCircleIntr<T> right) => left.Equals(right);

        /// <summary>
        /// Exact field-wise inequality comparison, see <see cref="Equals(LineCircleIntr{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the results do not compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(LineCircleIntr<T> left, LineCircleIntr<T> right) => !left.Equals(right);
    }

    /// <summary>
    /// Finds the intersects between a line segment and a circle.
    /// </summary>
    public static class LineCircleIntersection
    {
        /// <summary>
        /// Finds the intersects between the line segment <c>p0-&gt;p1</c> and the circle defined by
        /// <paramref name="radius"/> and <paramref name="circleCenter"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="p0">Start point of the line segment.</param>
        /// <param name="p1">End point of the line segment.</param>
        /// <param name="radius">Radius of the circle.</param>
        /// <param name="circleCenter">Center of the circle.</param>
        /// <param name="epsilon">Positional tolerance used for fuzzy comparisons.</param>
        /// <returns>
        /// The intersect result; see <see cref="LineCircleIntrKind"/> for the meaning of each case.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The result is given as parametric solution(s) for the line segment equation
        /// <c>P(t) = p0 + t * (p1 - p0)</c> for <c>t = 0</c> to <c>t = 1</c>. If <c>t &lt; 0</c> or
        /// <c>t &gt; 1</c> the intersect occurs only when extending the segment out past the points
        /// <paramref name="p0"/> and <paramref name="p1"/> given: for <c>t &lt; 0</c> the intersect
        /// is nearest to <paramref name="p0"/>, for <c>t &gt; 1</c> nearest to
        /// <paramref name="p1"/>.
        /// </para>
        /// <para>
        /// Intersects are "sticky" and "snap" to tangent points using fuzzy comparisons. Two
        /// intersect points that are fuzzy equal are returned as one tangent intersect, and a line
        /// outside the circle but within <paramref name="epsilon"/> of its radius is also returned
        /// as a tangent intersect.
        /// </para>
        /// <para>
        /// The merge of two candidate intersect points into a single tangent intersect is decided
        /// by comparing the two computed positions with each other, not by comparing the shortest
        /// distance from the line to the center against the radius. The latter would merge points
        /// that are much farther apart than <paramref name="epsilon"/> when the circle radius is
        /// large, because a small radial deviation there corresponds to a large chord length.
        /// </para>
        /// <para>
        /// Implementation solves for the cartesian intersect points geometrically with the circle
        /// shifted to the origin, using the line equation <c>Ax + By + C = 0</c>, and converts the
        /// results back to parametric <c>t</c>. This was found to be more numerically stable than
        /// solving for <c>t</c> via the quadratic equation.
        /// </para>
        /// </remarks>
        public static LineCircleIntr<T> Intersect<T>(
            Vector2<T> p0,
            Vector2<T> p1,
            T radius,
            Vector2<T> circleCenter,
            T epsilon)
            where T : struct, IFloatingPointIeee754<T>
        {
            T dx = p1.X - p0.X;
            T dy = p1.Y - p0.Y;
            T h = circleCenter.X;
            T k = circleCenter.Y;

            T two = T.CreateChecked(2);

            if (p0.FuzzyEqEps(p1, epsilon))
            {
                // p0 == p1, test if the point is on the circle, using the average of the points'
                // x and y values for fuzziness. Compare the distance against the radius, not the
                // squares: squaring makes the effective tolerance depend on the radius.
                T xh = (p0.X + p1.X) / two - h;
                T yk = (p0.Y + p1.Y) / two - k;
                if (T.Sqrt(xh * xh + yk * yk).FuzzyEq(radius, epsilon))
                {
                    return LineCircleIntr<T>.TangentIntersect(T.Zero);
                }
                return LineCircleIntr<T>.NoIntersect;
            }

            Vector2<T> p0Shifted = p0 - circleCenter;
            Vector2<T> p1Shifted = p1 - circleCenter;

            (T a, T b, T c) = dx.FuzzyEqZero()
                ? (T.One, T.Zero, -(p1Shifted.X + p0Shifted.X) / two)
                : (dy / dx, -T.One, p1Shifted.Y - (dy / dx) * p1Shifted.X);

            T a2 = a * a;
            T b2 = b * b;
            T c2 = c * c;
            T r2 = radius * radius;
            T a2_b2 = a2 + b2;

            T shortestDist = T.Abs(c) / T.Sqrt(a2_b2);

            if (shortestDist > radius + epsilon)
            {
                return LineCircleIntr<T>.NoIntersect;
            }

            // Adding h and k back to the solution terms (shifting from origin back to real
            // coordinates). Note 0.8.0 divides here; the reciprocal-multiply form was only
            // introduced in 0.9.0 and rounds differently.
            T x0 = -a * c / a2_b2 + h;
            T y0 = -b * c / a2_b2 + k;

            if (shortestDist >= radius)
            {
                T tangentT = BaseMath.ParametricFromPoint(p0, p1, new Vector2<T>(x0, y0), epsilon);
                return LineCircleIntr<T>.TangentIntersect(tangentT);
            }

            T d = r2 - c2 / a2_b2;
            // Taking abs avoids NaN if round-off makes d slightly negative after shortestDist
            // compared less than radius above.
            T mult = T.Sqrt(T.Abs(d / a2_b2));

            var point1 = new Vector2<T>(x0 + b * mult, y0 - a * mult);
            var point2 = new Vector2<T>(x0 - b * mult, y0 + a * mult);

            // Apply the positional epsilon to the intersection positions. Avoid comparing
            // shortestDist with the radius: that merges points much farther apart than epsilon
            // when the circle radius is large.
            if (point1.FuzzyEqEps(point2, epsilon))
            {
                T tangentT = BaseMath.ParametricFromPoint(p0, p1, new Vector2<T>(x0, y0), epsilon);
                return LineCircleIntr<T>.TangentIntersect(tangentT);
            }

            T sol1 = BaseMath.ParametricFromPoint(p0, p1, point1, epsilon);
            T sol2 = BaseMath.ParametricFromPoint(p0, p1, point2, epsilon);

            (T t0, T t1) = BaseMath.MinMax(sol1, sol2);
            return LineCircleIntr<T>.TwoIntersects(t0, t1);
        }
    }
}
