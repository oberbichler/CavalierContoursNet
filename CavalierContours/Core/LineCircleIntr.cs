using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    public enum LineCircleIntrKind : byte
    {
        NoIntersect,
        TangentIntersect,
        TwoIntersects
    }

    public readonly struct LineCircleIntr<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly LineCircleIntrKind Kind;
        public readonly T T0;
        public readonly T T1;

        private LineCircleIntr(LineCircleIntrKind kind, T t0, T t1)
        {
            Kind = kind;
            T0 = t0;
            T1 = t1;
        }

        public static LineCircleIntr<T> NoIntersect => new(LineCircleIntrKind.NoIntersect, default, default);
        public static LineCircleIntr<T> TangentIntersect(T t0) => new(LineCircleIntrKind.TangentIntersect, t0, default);
        public static LineCircleIntr<T> TwoIntersects(T t0, T t1) => new(LineCircleIntrKind.TwoIntersects, t0, t1);
    }

    public static class LineCircleIntersection
    {
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
                T xh = (p0.X + p1.X) / two - h;
                T yk = (p0.Y + p1.Y) / two - k;
                if ((xh * xh + yk * yk).FuzzyEq(radius * radius, epsilon))
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

            T x0 = -a * c / a2_b2 + h;
            T y0 = -b * c / a2_b2 + k;

            if (shortestDist.FuzzyEq(radius, epsilon))
            {
                T t = BaseMath.ParametricFromPoint(p0, p1, new Vector2<T>(x0, y0), epsilon);
                return LineCircleIntr<T>.TangentIntersect(t);
            }

            T d = r2 - c2 / a2_b2;
            T mult = T.Sqrt(T.Abs(d / a2_b2));

            T xSol1 = x0 + b * mult;
            T xSol2 = x0 - b * mult;
            T ySol1 = y0 - a * mult;
            T ySol2 = y0 + a * mult;

            T sol1 = BaseMath.ParametricFromPoint(p0, p1, new Vector2<T>(xSol1, ySol1), epsilon);
            T sol2 = BaseMath.ParametricFromPoint(p0, p1, new Vector2<T>(xSol2, ySol2), epsilon);

            (T t0, T t1) = BaseMath.MinMax(sol1, sol2);
            return LineCircleIntr<T>.TwoIntersects(t0, t1);
        }
    }
}
