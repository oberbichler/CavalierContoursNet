using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    public static class Fuzzy<T> where T : struct, IFloatingPointIeee754<T>
    {
        public static readonly T Epsilon = T.CreateChecked(1e-8);
    }

    public static class FuzzyExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEq<T>(this T self, T other, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return T.Abs(self - other) < eps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEq<T>(this T self, T other) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyEq(other, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEqZero<T>(this T self, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return T.Abs(self) < eps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEqZero<T>(this T self) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyEqZero(Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyGt<T>(this T self, T other, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return (self + eps) > other;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyGt<T>(this T self, T other) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyGt(other, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyLt<T>(this T self, T other, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return self < (other + eps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyLt<T>(this T self, T other) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyLt(other, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyInRange<T>(this T self, T min, T max, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyGt(min, eps) && self.FuzzyLt(max, eps);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyInRange<T>(this T self, T min, T max) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyInRange(min, max, Fuzzy<T>.Epsilon);
        }
    }
}
