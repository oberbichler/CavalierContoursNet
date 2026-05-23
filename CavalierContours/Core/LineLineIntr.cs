using System;
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

    public readonly struct LineLineIntr<T>
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
