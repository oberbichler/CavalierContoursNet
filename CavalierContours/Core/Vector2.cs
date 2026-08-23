using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    /// <summary>
    /// A 2D vector with x and y components.
    /// </summary>
    /// <remarks>
    /// This is the fundamental 2D vector type used throughout the library for representing points,
    /// directions, and performing vector operations.
    /// </remarks>
    /// <typeparam name="T">Floating point component type used for the x and y components.</typeparam>
    public readonly struct Vector2<T> : IEquatable<Vector2<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// The x-coordinate component.
        /// </summary>
        public readonly T X;

        /// <summary>
        /// The y-coordinate component.
        /// </summary>
        public readonly T Y;

        /// <summary>
        /// Create a new vector with x and y components.
        /// </summary>
        /// <param name="x">The x-coordinate component.</param>
        /// <param name="y">The y-coordinate component.</param>
        public Vector2(T x, T y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Gets the zero vector (x = 0, y = 0).
        /// </summary>
        public static Vector2<T> Zero => new(T.Zero, T.Zero);

        /// <summary>
        /// Uniformly scale the vector by <paramref name="scaleFactor"/>.
        /// </summary>
        /// <param name="scaleFactor">Factor applied to both components.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Scale(T scaleFactor) => new(scaleFactor * X, scaleFactor * Y);

        /// <summary>
        /// Dot product.
        /// </summary>
        /// <param name="other">The vector to take the dot product with.</param>
        /// <returns><c>X * other.X + Y * other.Y</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dot(Vector2<T> other) => X * other.X + Y * other.Y;

        /// <summary>
        /// Compute the perpendicular dot product (<c>X * other.Y - Y * other.X</c>).
        /// </summary>
        /// <param name="other">The vector to take the perpendicular dot product with.</param>
        /// <returns>The perpendicular dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T PerpDot(Vector2<T> other) => X * other.Y - Y * other.X;

        /// <summary>
        /// Squared length of the vector.
        /// </summary>
        /// <returns>The dot product of the vector with itself.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LengthSquared() => Dot(this);

        /// <summary>
        /// Length of the vector.
        /// </summary>
        /// <returns>The euclidean length of the vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Length() => T.Sqrt(LengthSquared());

        /// <summary>
        /// Normalize the vector (length = 1).
        /// </summary>
        /// <returns>The vector scaled by the reciprocal of its length.</returns>
        /// <remarks>
        /// No length check is performed; normalizing a zero length vector produces non-finite
        /// components. Use <see cref="SafeNormalize"/> when the vector may be degenerate.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Normalize() => Scale(T.One / Length());

        /// <summary>
        /// Normalize the vector (length = 1), returning <see cref="Zero"/> for degenerate vectors.
        /// </summary>
        /// <returns>
        /// <see cref="Zero"/> if the squared length is not greater than
        /// <see cref="Fuzzy{T}.Epsilon"/> squared, otherwise the result of <see cref="Normalize"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> SafeNormalize()
        {
            T eps = Fuzzy<T>.Epsilon;
            return LengthSquared() <= eps * eps ? Zero : Normalize();
        }

        /// <summary>
        /// Fuzzy equal comparison with another vector using the <paramref name="fuzzyEpsilon"/>
        /// given.
        /// </summary>
        /// <param name="other">The vector to compare against.</param>
        /// <param name="fuzzyEpsilon">Tolerance applied component-wise.</param>
        /// <returns><see langword="true"/> if both components compare fuzzy equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEqEps(Vector2<T> other, T fuzzyEpsilon)
        {
            return X.FuzzyEq(other.X, fuzzyEpsilon) && Y.FuzzyEq(other.Y, fuzzyEpsilon);
        }

        /// <summary>
        /// Fuzzy equal comparison with another vector using <see cref="Fuzzy{T}.Epsilon"/>.
        /// </summary>
        /// <param name="other">The vector to compare against.</param>
        /// <returns><see langword="true"/> if both components compare fuzzy equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEq(Vector2<T> other)
        {
            return FuzzyEqEps(other, Fuzzy<T>.Epsilon);
        }

        /// <summary>
        /// Create perpendicular vector.
        /// </summary>
        /// <returns>The vector rotated counter clockwise by 90 degrees, <c>(-Y, X)</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Perp() => new(-Y, X);

        /// <summary>
        /// Create perpendicular unit vector (length = 1).
        /// </summary>
        /// <returns>The result of <see cref="Perp"/> passed through <see cref="Normalize"/>.</returns>
        /// <remarks>
        /// Inherits the degenerate behaviour of <see cref="Normalize"/>; use
        /// <see cref="SafeUnitPerp"/> when the vector may be zero length.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> UnitPerp() => Perp().Normalize();

        /// <summary>
        /// Create perpendicular unit vector, returning <see cref="Zero"/> for degenerate vectors.
        /// </summary>
        /// <returns>
        /// The result of <see cref="Perp"/> passed through <see cref="SafeNormalize"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> SafeUnitPerp() => Perp().SafeNormalize();

        /// <summary>
        /// Rotate this point around an <paramref name="origin"/> point by some
        /// <paramref name="angle"/> in radians.
        /// </summary>
        /// <param name="origin">The point rotated about.</param>
        /// <param name="angle">The rotation angle in radians, positive is counter clockwise.</param>
        /// <returns>The rotated point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> RotateAbout(Vector2<T> origin, T angle)
        {
            Vector2<T> translated = this - origin;
            T s = T.Sin(angle);
            T c = T.Cos(angle);
            Vector2<T> rotated = new(
                translated.X * c - translated.Y * s,
                translated.X * s + translated.Y * c
            );
            return rotated + origin;
        }

        /// <summary>
        /// Exact component-wise equality comparison.
        /// </summary>
        /// <param name="other">The vector to compare against.</param>
        /// <returns><see langword="true"/> if both components compare exactly equal.</returns>
        /// <remarks>
        /// This is an IEEE 754 comparison matching Rust's derived <c>PartialEq</c>: <c>NaN</c> is
        /// not equal to itself and <c>0.0</c> equals <c>-0.0</c>. Use <see cref="FuzzyEq"/> for
        /// tolerant geometric comparisons.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Matches Rust's derived PartialEq: IEEE 754 comparison, so NaN != NaN and 0.0 == -0.0.
        // T.Equals would treat NaN as equal to itself.
        public bool Equals(Vector2<T> other) => X == other.X && Y == other.Y;

        /// <summary>
        /// Exact component-wise equality comparison against a boxed value.
        /// </summary>
        /// <param name="obj">The value to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="Vector2{T}"/> that
        /// compares equal per <see cref="Equals(Vector2{T})"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is Vector2<T> other && Equals(other);

        /// <summary>
        /// Returns a hash code combining the x and y components.
        /// </summary>
        /// <returns>The hash code for this vector.</returns>
        public override int GetHashCode() => HashCode.Combine(X, Y);

        /// <summary>
        /// Formats the vector as <c>[x, y]</c>.
        /// </summary>
        /// <returns>The string representation of this vector.</returns>
        public override string ToString() => $"[{X}, {Y}]";

        /// <summary>
        /// Component-wise addition.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>The component-wise sum.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> operator +(Vector2<T> left, Vector2<T> right) => new(left.X + right.X, left.Y + right.Y);

        /// <summary>
        /// Component-wise subtraction.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>The component-wise difference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> operator -(Vector2<T> left, Vector2<T> right) => new(left.X - right.X, left.Y - right.Y);

        /// <summary>
        /// Component-wise negation.
        /// </summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The vector with both components negated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> operator -(Vector2<T> value) => new(-value.X, -value.Y);

        /// <summary>
        /// Exact component-wise equality comparison, see <see cref="Equals(Vector2{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if both components compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2<T> left, Vector2<T> right) => left.Equals(right);

        /// <summary>
        /// Exact component-wise inequality comparison, see <see cref="Equals(Vector2{T})"/>.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if the vectors do not compare exactly equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2<T> left, Vector2<T> right) => !left.Equals(right);
    }

    /// <summary>
    /// Shorthand constructors for <see cref="Vector2{T}"/>.
    /// </summary>
    public static class Vector2
    {
        /// <summary>
        /// Shorthand constructor for creating a <see cref="Vector2{T}"/>, equivalent to
        /// <c>new Vector2&lt;T&gt;(x, y)</c>.
        /// </summary>
        /// <typeparam name="T">Floating point component type used for the x and y components.</typeparam>
        /// <param name="x">The x-coordinate component.</param>
        /// <param name="y">The y-coordinate component.</param>
        /// <returns>The new vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> New<T>(T x, T y) where T : struct, IFloatingPointIeee754<T>
        {
            return new Vector2<T>(x, y);
        }
    }
}
