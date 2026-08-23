using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    /// <summary>
    /// Holds the default epsilon used for fuzzy comparisons of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Floating point type the epsilon is produced for.</typeparam>
    public static class Fuzzy<T> where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The default epsilon value for fuzzy comparisons.
        /// </summary>
        /// <remarks>
        /// The value is hardcoded to <c>1e-8</c> regardless of <typeparamref name="T"/>, matching
        /// the Rust implementation which uses <c>1.0e-8</c> for both <c>f32</c> and <c>f64</c>.
        /// Note that <c>1e-8</c> is below the resolution of <see cref="float"/> near 1.0, and for
        /// <see cref="Half"/> it rounds to zero, which turns every fuzzy comparison into an exact
        /// one.
        /// </remarks>
        public static readonly T Epsilon = T.CreateChecked(1e-8);
    }

    /// <summary>
    /// Fuzzy equality and fuzzy ordering comparisons for floating point values.
    /// </summary>
    /// <remarks>
    /// These comparisons account for floating point precision issues in geometric computations,
    /// where exact equality is rarely achievable.
    /// </remarks>
    public static class FuzzyExtensions
    {
        /// <summary>
        /// Returns <see langword="true"/> if this value is approximately equal to the other one,
        /// using a provided epsilon value.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <param name="other">The value to compare against.</param>
        /// <param name="eps">Tolerance for the comparison.</param>
        /// <returns><see langword="true"/> if <c>|self - other| &lt; eps</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEq<T>(this T self, T other, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return T.Abs(self - other) < eps;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this value is approximately equal to the other one,
        /// using <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <param name="other">The value to compare against.</param>
        /// <returns><see langword="true"/> if the values are approximately equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEq<T>(this T self, T other) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyEq(other, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this value is approximately equal to zero, using a
        /// provided epsilon value.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <param name="eps">Tolerance for the comparison.</param>
        /// <returns><see langword="true"/> if <c>|self| &lt; eps</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEqZero<T>(this T self, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return T.Abs(self) < eps;
        }

        /// <summary>
        /// Returns <see langword="true"/> if this value is approximately equal to zero, using
        /// <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <returns><see langword="true"/> if the value is approximately zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyEqZero<T>(this T self) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyEqZero(Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this value is fuzzy greater than the other, using a
        /// provided epsilon value.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <param name="other">The value to compare against.</param>
        /// <param name="eps">Tolerance for the comparison.</param>
        /// <returns><see langword="true"/> if <c>self + eps &gt; other</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyGt<T>(this T self, T other, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return (self + eps) > other;
        }

        /// <summary>
        /// Fuzzy greater than, using <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <param name="other">The value to compare against.</param>
        /// <returns><see langword="true"/> if this value is fuzzy greater than the other.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyGt<T>(this T self, T other) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyGt(other, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this value is fuzzy less than the other, using a
        /// provided epsilon value.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <param name="other">The value to compare against.</param>
        /// <param name="eps">Tolerance for the comparison.</param>
        /// <returns><see langword="true"/> if <c>self &lt; other + eps</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyLt<T>(this T self, T other, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return self < (other + eps);
        }

        /// <summary>
        /// Fuzzy less than, using <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to compare.</param>
        /// <param name="other">The value to compare against.</param>
        /// <returns><see langword="true"/> if this value is fuzzy less than the other.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyLt<T>(this T self, T other) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyLt(other, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Test if this value is in range between <paramref name="min"/> and
        /// <paramref name="max"/>, with some epsilon for fuzzy comparing.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to test.</param>
        /// <param name="min">Lower bound of the range.</param>
        /// <param name="max">Upper bound of the range.</param>
        /// <param name="eps">Tolerance applied to both bounds.</param>
        /// <returns>
        /// <see langword="true"/> if the value is fuzzy greater than <paramref name="min"/> and
        /// fuzzy less than <paramref name="max"/>, so the bounds themselves are included.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyInRange<T>(this T self, T min, T max, T eps) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyGt(min, eps) && self.FuzzyLt(max, eps);
        }

        /// <summary>
        /// Same as <see cref="FuzzyInRange{T}(T, T, T, T)"/> using <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type being compared.</typeparam>
        /// <param name="self">The value to test.</param>
        /// <param name="min">Lower bound of the range.</param>
        /// <param name="max">Upper bound of the range.</param>
        /// <returns><see langword="true"/> if the value is fuzzy within the range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FuzzyInRange<T>(this T self, T min, T max) where T : struct, IFloatingPointIeee754<T>
        {
            return self.FuzzyInRange(min, max, Fuzzy<T>.Epsilon);
        }
    }
}
