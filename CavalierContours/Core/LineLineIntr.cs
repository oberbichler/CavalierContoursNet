using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    /// <summary>
    /// Discriminates the result cases of finding the intersect between two line segments.
    /// </summary>
    public enum LineLineIntrKind : byte
    {
        /// <summary>
        /// No intersect. Either the segments are (or almost, using the epsilon) parallel and not
        /// collinear, or both segments are points distinct from each other, or one segment is a
        /// point distinct from the other segment.
        /// </summary>
        NoIntersect,

        /// <summary>
        /// There is a true intersect between the line segments: they are not parallel and meet at
        /// one point within both segments, or both segments are points lying over each other, or
        /// one segment is a point lying within the other segment (all position compares using the
        /// epsilon). <see cref="LineLineIntr{T}.Seg1T"/> and
        /// <see cref="LineLineIntr{T}.Seg2T"/> carry the parametric values of the intersect on the
        /// first and second segment.
        /// </summary>
        TrueIntersect,

        /// <summary>
        /// The lines are collinear and overlap by some amount, given as a parametric interval along
        /// the second segment. <see cref="LineLineIntr{T}.Seg2T"/> carries <c>seg2_t0</c>, the
        /// start of the coincidence, and <see cref="LineLineIntr{T}.Seg2T1"/> carries
        /// <c>seg2_t1</c>, the end of the coincidence, while
        /// <see cref="LineLineIntr{T}.Seg1T"/> is unused.
        /// </summary>
        Overlapping,

        /// <summary>
        /// There is an intersect between the infinite lines, but one or both of the segments must
        /// be extended to reach it, i.e. there is no intersect for <c>0 &lt;= t &lt;= 1</c> on both
        /// segments. <see cref="LineLineIntr{T}.Seg1T"/> and
        /// <see cref="LineLineIntr{T}.Seg2T"/> carry the parametric values of the intersect on the
        /// first and second segment.
        /// </summary>
        FalseIntersect
    }

    /// <summary>
    /// Holds the result of finding the intersect between two line segments.
    /// </summary>
    /// <typeparam name="T">Floating point type of the parametric values.</typeparam>
    /// <remarks>
    /// Which fields are meaningful depends on <see cref="Kind"/>; see the individual
    /// <see cref="LineLineIntrKind"/> members.
    /// </remarks>
    public readonly struct LineLineIntr<T> : IEquatable<LineLineIntr<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The result case this value represents.
        /// </summary>
        public readonly LineLineIntrKind Kind;

        /// <summary>
        /// Parametric value for the intersect on the first segment. Meaningful for
        /// <see cref="LineLineIntrKind.TrueIntersect"/> and
        /// <see cref="LineLineIntrKind.FalseIntersect"/>; unused for the other kinds.
        /// </summary>
        public readonly T Seg1T;

        /// <summary>
        /// Parametric value along the second segment. For
        /// <see cref="LineLineIntrKind.TrueIntersect"/> and
        /// <see cref="LineLineIntrKind.FalseIntersect"/> this is <c>seg2_t</c>, the intersect; for
        /// <see cref="LineLineIntrKind.Overlapping"/> it is <c>seg2_t0</c>, the start of the
        /// coincidence.
        /// </summary>
        public readonly T Seg2T;

        /// <summary>
        /// Parametric value for the end of the coincidence along the second segment
        /// (<c>seg2_t1</c>). Meaningful only for <see cref="LineLineIntrKind.Overlapping"/>.
        /// </summary>
        public readonly T Seg2T1; // Used specifically for Overlapping's second T value

        private LineLineIntr(LineLineIntrKind kind, T seg1T, T seg2T, T seg2T1)
        {
            Kind = kind;
            Seg1T = seg1T;
            Seg2T = seg2T;
            Seg2T1 = seg2T1;
        }

        /// <summary>
        /// Gets a result representing <see cref="LineLineIntrKind.NoIntersect"/>.
        /// </summary>
        public static LineLineIntr<T> NoIntersect => new(LineLineIntrKind.NoIntersect, default, default, default);

        /// <summary>
        /// Creates a <see cref="LineLineIntrKind.TrueIntersect"/> result.
        /// </summary>
        /// <param name="seg1T">Parametric value for the intersect on the first segment.</param>
        /// <param name="seg2T">Parametric value for the intersect on the second segment.</param>
        /// <returns>The true intersect result.</returns>
        public static LineLineIntr<T> TrueIntersect(T seg1T, T seg2T) => new(LineLineIntrKind.TrueIntersect, seg1T, seg2T, default);

        /// <summary>
        /// Creates a <see cref="LineLineIntrKind.FalseIntersect"/> result.
        /// </summary>
        /// <param name="seg1T">Parametric value for the intersect on the first segment.</param>
        /// <param name="seg2T">Parametric value for the intersect on the second segment.</param>
        /// <returns>The false intersect result.</returns>
        public static LineLineIntr<T> FalseIntersect(T seg1T, T seg2T) => new(LineLineIntrKind.FalseIntersect, seg1T, seg2T, default);

        /// <summary>
        /// Creates an <see cref="LineLineIntrKind.Overlapping"/> result.
        /// </summary>
        /// <param name="seg2T0">
        /// Parametric value for the start of the coincidence along the second segment; stored in
        /// <see cref="Seg2T"/>.
        /// </param>
        /// <param name="seg2T1">
        /// Parametric value for the end of the coincidence along the second segment; stored in
        /// <see cref="Seg2T1"/>.
        /// </param>
        /// <returns>The overlapping result.</returns>
        public static LineLineIntr<T> Overlapping(T seg2T0, T seg2T1) => new(LineLineIntrKind.Overlapping, default, seg2T0, seg2T1);

        /// <summary>
        /// The first overlap parameter (<c>seg2_t0</c>) along segment 2, valid only when
        /// <see cref="Kind"/> is <see cref="LineLineIntrKind.Overlapping"/>.
        /// </summary>
        /// <remarks>
        /// This is an alias for the same storage as <see cref="Seg2T"/>; it does not add a field.
        /// It exists because <see cref="Seg2T"/> means <c>seg2_t</c> for the
        /// <see cref="LineLineIntrKind.TrueIntersect"/> and
        /// <see cref="LineLineIntrKind.FalseIntersect"/> kinds but <c>seg2_t0</c> for
        /// <see cref="LineLineIntrKind.Overlapping"/>.
        /// </remarks>
        public T OverlapSeg2T0
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(Kind == LineLineIntrKind.Overlapping, "OverlapSeg2T0 is only meaningful for Overlapping results.");
                return Seg2T;
            }
        }

        /// <summary>
        /// The second overlap parameter (<c>seg2_t1</c>) along segment 2, valid only when
        /// <see cref="Kind"/> is <see cref="LineLineIntrKind.Overlapping"/>.
        /// </summary>
        /// <remarks>
        /// This is an alias for the same storage as <see cref="Seg2T1"/>; it does not add a field.
        /// It exists to pair with <see cref="OverlapSeg2T0"/> and make the <c>seg2_t0</c> /
        /// <c>seg2_t1</c> pairing explicit at the call site.
        /// </remarks>
        public T OverlapSeg2T1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert(Kind == LineLineIntrKind.Overlapping, "OverlapSeg2T1 is only meaningful for Overlapping results.");
                return Seg2T1;
            }
        }

        /// <summary>
        /// Exact field-wise equality comparison.
        /// </summary>
        /// <param name="other">The result to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if the kind and all parametric values compare exactly equal.
        /// </returns>
        /// <remarks>
        /// This is an IEEE 754 comparison matching Rust's derived <c>PartialEq</c>: <c>NaN</c> is
        /// not equal to itself and <c>0.0</c> equals <c>-0.0</c>. Field-wise comparison is correct
        /// despite the field overloading: <see cref="Kind"/> is compared first, so two values can
        /// only reach the payload comparison when they interpret the same fields the same way.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Matches Rust's derived PartialEq: IEEE 754 comparison, so NaN != NaN and 0.0 == -0.0.
        // T.Equals would treat NaN as equal to itself.
        //
        // Field-wise comparison is correct despite the field overloading documented above
        // (for Overlapping, Seg2T carries seg2_t0 and Seg2T1 carries seg2_t1, while Seg1T is
        // default): Kind is compared first, so two values can only reach the payload comparison
        // when they interpret the same fields the same way.
        public bool Equals(LineLineIntr<T> other)
        {
            return Kind == other.Kind
                && Seg1T == other.Seg1T
                && Seg2T == other.Seg2T
                && Seg2T1 == other.Seg2T1;
        }

        /// <summary>
        /// Exact field-wise equality comparison against a boxed value.
        /// </summary>
        /// <param name="obj">The value to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="LineLineIntr{T}"/> that
        /// compares equal per <see cref="Equals(LineLineIntr{T})"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is LineLineIntr<T> other && Equals(other);

        /// <summary>
        /// Returns a hash code combining the kind and all parametric values.
        /// </summary>
        /// <returns>The hash code for this result.</returns>
        public override int GetHashCode() => HashCode.Combine(Kind, Seg1T, Seg2T, Seg2T1);

        /// <summary>
        /// Exact field-wise equality comparison, see <see cref="Equals(LineLineIntr{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the results compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(LineLineIntr<T> left, LineLineIntr<T> right) => left.Equals(right);

        /// <summary>
        /// Exact field-wise inequality comparison, see <see cref="Equals(LineLineIntr{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the results do not compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(LineLineIntr<T> left, LineLineIntr<T> right) => !left.Equals(right);
    }

    /// <summary>
    /// Finds the intersects between two line segments.
    /// </summary>
    public static class LineLineIntersection
    {
        /// <summary>
        /// Finds the intersects between the two line segments defined by <c>v1-&gt;v2</c> and
        /// <c>u1-&gt;u2</c>.
        /// </summary>
        /// <typeparam name="T">Floating point type of the point components.</typeparam>
        /// <param name="v1">Start point of the first segment.</param>
        /// <param name="v2">End point of the first segment.</param>
        /// <param name="u1">Start point of the second segment.</param>
        /// <param name="u2">End point of the second segment.</param>
        /// <param name="epsilon">Tolerance used for fuzzy float comparisons.</param>
        /// <returns>
        /// The intersect result; see <see cref="LineLineIntrKind"/> for the meaning of each case
        /// and which parametric values it carries.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The result is given as parametric solution(s) using the general line segment equation
        /// <c>P(t) = p0 + t * (p1 - p0)</c>, where <c>t</c> goes from 0 to 1. For <c>t &lt; 0</c> or
        /// <c>t &gt; 1</c> the result is on the same line but not within the line segment. The
        /// cases where the lines may be parallel, collinear, or single points are all handled.
        /// </para>
        /// <para>
        /// Parametric values are multiplied by the segment length before being fuzzy compared, so
        /// that <paramref name="epsilon"/> is applied at a position scale: a difference in
        /// parametric value of 0.1 represents a much greater position difference for a segment of
        /// length 1,000,000 than for one of length 0.01.
        /// </para>
        /// </remarks>
        public static LineLineIntr<T> Intersect<T>(
            Vector2<T> v1,
            Vector2<T> v2,
            Vector2<T> u1,
            Vector2<T> u2,
            T epsilon)
            where T : struct, IFloatingPointIeee754<T>
        {
            Vector2<T> v = v2 - v1;
            Vector2<T> u = u2 - u1;
            T vPdotU = v.PerpDot(u);
            Vector2<T> w = v1 - u1;

            T seg1Length = (v2 - v1).Length();
            T seg2Length = (u2 - u1).Length();

            if (!vPdotU.FuzzyEqZero(epsilon))
            {
                T seg1T = u.PerpDot(w) / vPdotU;
                T seg2T = v.PerpDot(w) / vPdotU;
                if (!(seg1T * seg1Length).FuzzyInRange(T.Zero, seg1Length, epsilon)
                    || !(seg2T * seg2Length).FuzzyInRange(T.Zero, seg2Length, epsilon))
                {
                    return LineLineIntr<T>.FalseIntersect(seg1T, seg2T);
                }
                return LineLineIntr<T>.TrueIntersect(seg1T, seg2T);
            }

            T vPdotW = v.PerpDot(w);
            T uPdotW = u.PerpDot(w);

            if (!vPdotW.FuzzyEqZero(epsilon) || !uPdotW.FuzzyEqZero(epsilon))
            {
                return LineLineIntr<T>.NoIntersect;
            }

            bool vIsPoint = v1.FuzzyEqEps(v2, epsilon);
            bool uIsPoint = u1.FuzzyEqEps(u2, epsilon);

            if (vIsPoint && uIsPoint)
            {
                if (v1.FuzzyEqEps(u1, epsilon))
                {
                    return LineLineIntr<T>.TrueIntersect(T.Zero, T.Zero);
                }
                return LineLineIntr<T>.NoIntersect;
            }

            if (vIsPoint)
            {
                T seg2T = BaseMath.ParametricFromPoint(u1, u2, v1, epsilon);
                if ((seg2T * seg2Length).FuzzyInRange(T.Zero, seg2Length, epsilon))
                {
                    return LineLineIntr<T>.TrueIntersect(T.Zero, seg2T);
                }
                return LineLineIntr<T>.NoIntersect;
            }

            if (uIsPoint)
            {
                T seg1T = BaseMath.ParametricFromPoint(v1, v2, u1, epsilon);
                if ((seg1T * seg1Length).FuzzyInRange(T.Zero, seg1Length, epsilon))
                {
                    return LineLineIntr<T>.TrueIntersect(seg1T, T.Zero);
                }
                return LineLineIntr<T>.NoIntersect;
            }

            Vector2<T> w2 = v2 - u1;
            T seg2T0, seg2T1;
            if (u.X.FuzzyEqZero(epsilon))
            {
                seg2T0 = w.Y / u.Y;
                seg2T1 = w2.Y / u.Y;
            }
            else
            {
                seg2T0 = w.X / u.X;
                seg2T1 = w2.X / u.X;
            }

            if (seg2T0 > seg2T1)
            {
                T temp = seg2T0;
                seg2T0 = seg2T1;
                seg2T1 = temp;
            }

            if (!(seg2T0 * seg2Length).FuzzyLt(seg2Length, epsilon)
                || !(seg2T1 * seg2Length).FuzzyGt(T.Zero, epsilon))
            {
                return LineLineIntr<T>.NoIntersect;
            }

            seg2T0 = T.Max(seg2T0, T.Zero);
            seg2T1 = T.Min(seg2T1, T.One);

            if (((seg2T1 - seg2T0) * seg2Length).FuzzyEqZero(epsilon))
            {
                T seg1T = (v1.FuzzyEqEps(u1, epsilon) || v1.FuzzyEqEps(u2, epsilon)) ? T.Zero : T.One;
                return LineLineIntr<T>.TrueIntersect(seg1T, seg2T0);
            }

            return LineLineIntr<T>.Overlapping(seg2T0, seg2T1);
        }
    }
}
