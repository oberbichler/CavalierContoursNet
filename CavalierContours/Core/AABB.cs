using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    /// <summary>
    /// Simple 2D axis aligned bounding box which holds the extents of a 2D box.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the box extents.</typeparam>
    public readonly struct AABB<T> : IEquatable<AABB<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Min x extent of the axis aligned bounding box.
        /// </summary>
        public readonly T MinX;

        /// <summary>
        /// Min y extent of the axis aligned bounding box.
        /// </summary>
        public readonly T MinY;

        /// <summary>
        /// Max x extent of the axis aligned bounding box.
        /// </summary>
        public readonly T MaxX;

        /// <summary>
        /// Max y extent of the axis aligned bounding box.
        /// </summary>
        public readonly T MaxY;

        /// <summary>
        /// Create a new axis aligned bounding box from the extents given.
        /// </summary>
        /// <param name="minX">Min x extent of the box.</param>
        /// <param name="minY">Min y extent of the box.</param>
        /// <param name="maxX">Max x extent of the box.</param>
        /// <param name="maxY">Max y extent of the box.</param>
        public AABB(T minX, T minY, T maxX, T maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        /// <summary>
        /// Gets the default box with all four extents set to zero.
        /// </summary>
        public static AABB<T> Default => new(T.Zero, T.Zero, T.Zero, T.Zero);

        /// <summary>
        /// Tests if this box overlaps another box (inclusive).
        /// </summary>
        /// <param name="other">The box to test against.</param>
        /// <returns>
        /// <see langword="true"/> if the boxes overlap; the check is inclusive of edges and corners
        /// touching.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OverlapsAABB(in AABB<T> other)
        {
            return Overlaps(other.MinX, other.MinY, other.MaxX, other.MaxY);
        }

        /// <summary>
        /// Tests if this box overlaps another box (inclusive). Same as
        /// <see cref="OverlapsAABB(in AABB{T})"/> but accepts the box extents directly.
        /// </summary>
        /// <param name="minX">Min x extent of the other box.</param>
        /// <param name="minY">Min y extent of the other box.</param>
        /// <param name="maxX">Max x extent of the other box.</param>
        /// <param name="maxY">Max y extent of the other box.</param>
        /// <returns>
        /// <see langword="true"/> if the boxes overlap; the check is inclusive of edges and corners
        /// touching.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(T minX, T minY, T maxX, T maxY)
        {
            if (MaxX < minX || MaxY < minY || MinX > maxX || MinY > maxY)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Tests if this box fully contains another box (inclusive).
        /// </summary>
        /// <param name="other">The box to test against.</param>
        /// <returns>
        /// <see langword="true"/> if the other box lies entirely within this box; the check is
        /// inclusive of coincident edges.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsAABB(in AABB<T> other)
        {
            return Contains(other.MinX, other.MinY, other.MaxX, other.MaxY);
        }

        /// <summary>
        /// Tests if this box fully contains another box (inclusive). Same as
        /// <see cref="ContainsAABB(in AABB{T})"/> but accepts the box extents directly.
        /// </summary>
        /// <param name="minX">Min x extent of the other box.</param>
        /// <param name="minY">Min y extent of the other box.</param>
        /// <param name="maxX">Max x extent of the other box.</param>
        /// <param name="maxY">Max y extent of the other box.</param>
        /// <returns>
        /// <see langword="true"/> if the other box lies entirely within this box; the check is
        /// inclusive of coincident edges.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T minX, T minY, T maxX, T maxY)
        {
            return MinX <= minX && MinY <= minY && MaxX >= maxX && MaxY >= maxY;
        }

        /// <summary>
        /// Exact extent-wise equality comparison.
        /// </summary>
        /// <param name="other">The box to compare against.</param>
        /// <returns><see langword="true"/> if all four extents compare exactly equal.</returns>
        /// <remarks>
        /// This is an IEEE 754 comparison matching Rust's derived <c>PartialEq</c>: <c>NaN</c> is
        /// not equal to itself and <c>0.0</c> equals <c>-0.0</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Matches Rust's derived PartialEq: IEEE 754 comparison, so NaN != NaN and 0.0 == -0.0.
        public bool Equals(AABB<T> other)
        {
            return MinX == other.MinX && MinY == other.MinY &&
                   MaxX == other.MaxX && MaxY == other.MaxY;
        }

        /// <summary>
        /// Exact extent-wise equality comparison against a boxed value.
        /// </summary>
        /// <param name="obj">The value to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is an <see cref="AABB{T}"/> that
        /// compares equal per <see cref="Equals(AABB{T})"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is AABB<T> other && Equals(other);

        /// <summary>
        /// Returns a hash code combining the four extents.
        /// </summary>
        /// <returns>The hash code for this box.</returns>
        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY);

        /// <summary>
        /// Formats the box as <c>[MinX: .., MinY: .., MaxX: .., MaxY: ..]</c>.
        /// </summary>
        /// <returns>The string representation of this box.</returns>
        public override string ToString() => $"[MinX: {MinX}, MinY: {MinY}, MaxX: {MaxX}, MaxY: {MaxY}]";

        /// <summary>
        /// Exact extent-wise equality comparison, see <see cref="Equals(AABB{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if all four extents compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AABB<T> left, AABB<T> right) => left.Equals(right);

        /// <summary>
        /// Exact extent-wise inequality comparison, see <see cref="Equals(AABB{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the boxes do not compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AABB<T> left, AABB<T> right) => !left.Equals(right);
    }
}
