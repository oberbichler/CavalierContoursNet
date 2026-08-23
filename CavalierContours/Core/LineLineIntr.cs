using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    public enum LineLineIntrKind : byte
    {
        NoIntersect,
        TrueIntersect,
        Overlapping,
        FalseIntersect
    }

    public readonly struct LineLineIntr<T> : IEquatable<LineLineIntr<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly LineLineIntrKind Kind;
        public readonly T Seg1T;
        public readonly T Seg2T;
        public readonly T Seg2T1; // Used specifically for Overlapping's second T value

        private LineLineIntr(LineLineIntrKind kind, T seg1T, T seg2T, T seg2T1)
        {
            Kind = kind;
            Seg1T = seg1T;
            Seg2T = seg2T;
            Seg2T1 = seg2T1;
        }

        public static LineLineIntr<T> NoIntersect => new(LineLineIntrKind.NoIntersect, default, default, default);
        public static LineLineIntr<T> TrueIntersect(T seg1T, T seg2T) => new(LineLineIntrKind.TrueIntersect, seg1T, seg2T, default);
        public static LineLineIntr<T> FalseIntersect(T seg1T, T seg2T) => new(LineLineIntrKind.FalseIntersect, seg1T, seg2T, default);
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

        public override bool Equals(object? obj) => obj is LineLineIntr<T> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, Seg1T, Seg2T, Seg2T1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(LineLineIntr<T> left, LineLineIntr<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(LineLineIntr<T> left, LineLineIntr<T> right) => !left.Equals(right);
    }

    public static class LineLineIntersection
    {
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
