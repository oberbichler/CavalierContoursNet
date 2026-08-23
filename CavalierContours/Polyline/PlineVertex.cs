using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// A polyline vertex, represented by an <see cref="X"/>, <see cref="Y"/> and
    /// <see cref="Bulge"/> value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="X"/> and <see cref="Y"/> describe the 2D position of the vertex.
    /// <see cref="Bulge"/> describes the curvature of the polyline segment that <em>starts</em>
    /// with this vertex; the bulge stored on the final vertex of an open polyline is therefore
    /// never used.
    /// </para>
    /// <para>
    /// Vertexes are compared with IEEE 754 semantics by <see cref="Equals(PlineVertex{T})"/>
    /// (matching the derived <c>PartialEq</c> upstream), so <c>NaN != NaN</c> and
    /// <c>0.0 == -0.0</c>. Use <see cref="FuzzyEq(PlineVertex{T})"/> or
    /// <see cref="FuzzyEqEps(PlineVertex{T}, T)"/> for tolerant comparison.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Floating point type used for the coordinates and the bulge.</typeparam>
    public readonly struct PlineVertex<T> : IEquatable<PlineVertex<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>X coordinate position of the vertex.</summary>
        public readonly T X;

        /// <summary>Y coordinate position of the vertex.</summary>
        public readonly T Y;

        /// <summary>
        /// Bulge of the polyline segment that starts with this vertex.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The bulge is defined as <c>tan(sweepAngle / 4)</c>, where <c>sweepAngle</c> is the
        /// signed angle swept by the circular arc going from this vertex to the next one. It is
        /// the single value the entire library builds arc geometry from.
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// <c>0</c> (fuzzy zero, see <see cref="BulgeIsZero"/>) means the segment is a straight
        /// line.
        /// </description></item>
        /// <item><description>
        /// A positive bulge means a counter-clockwise arc.
        /// </description></item>
        /// <item><description>
        /// A negative bulge means a clockwise arc.
        /// </description></item>
        /// <item><description>
        /// <c>|bulge| == 1</c> corresponds to a sweep angle of <c>PI</c>, i.e. a semicircle. A
        /// polyline arc segment can never sweep more than <c>PI</c>; a full circle is therefore
        /// expressed as two segments.
        /// </description></item>
        /// <item><description>
        /// The magnitude determines the curvature: the larger <c>|bulge|</c>, the tighter the arc
        /// for a given chord.
        /// </description></item>
        /// </list>
        /// <para>
        /// <c>BaseMath.AngleFromBulge</c> and <c>BaseMath.BulgeFromAngle</c> convert between bulge
        /// and sweep angle.
        /// </para>
        /// </remarks>
        public readonly T Bulge;

        /// <summary>Constructs a vertex from its position and bulge.</summary>
        /// <param name="x">X coordinate position of the vertex.</param>
        /// <param name="y">Y coordinate position of the vertex.</param>
        /// <param name="bulge">
        /// Bulge of the segment starting at this vertex, see <see cref="Bulge"/>.
        /// </param>
        public PlineVertex(T x, T y, T bulge)
        {
            X = x;
            Y = y;
            Bulge = bulge;
        }

        /// <summary>
        /// Constructs a vertex from a <c>[x, y, bulge]</c> slice.
        /// </summary>
        /// <param name="slice">Span that must contain exactly three elements: x, y and bulge.</param>
        /// <returns>The vertex built from the three elements of <paramref name="slice"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="slice"/> does not contain exactly three elements. Upstream returns
        /// <c>None</c> in this case instead of signalling an error.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlineVertex<T> FromSlice(ReadOnlySpan<T> slice)
        {
            if (slice.Length != 3)
            {
                throw new ArgumentException("Slice must contain exactly 3 elements", nameof(slice));
            }
            return new PlineVertex<T>(slice[0], slice[1], slice[2]);
        }

        /// <summary>Constructs a vertex using a 2D vector as the position.</summary>
        /// <param name="vector">Position of the vertex.</param>
        /// <param name="bulge">
        /// Bulge of the segment starting at this vertex, see <see cref="Bulge"/>.
        /// </param>
        /// <returns>A vertex at <paramref name="vector"/> with the given bulge.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlineVertex<T> FromVector2(Vector2<T> vector, T bulge)
        {
            return new PlineVertex<T>(vector.X, vector.Y, bulge);
        }

        /// <summary>
        /// Creates a copy of this vertex with a new bulge value but the same <see cref="X"/> and
        /// <see cref="Y"/> values.
        /// </summary>
        /// <param name="bulge">New bulge value, see <see cref="Bulge"/>.</param>
        /// <returns>A copy of this vertex with the bulge replaced.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> WithBulge(T bulge)
        {
            return new PlineVertex<T>(X, Y, bulge);
        }

        /// <summary>Returns the position of this vertex as a 2D vector.</summary>
        /// <returns>A vector holding <see cref="X"/> and <see cref="Y"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Pos() => new(X, Y);

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="Bulge"/> is fuzzy equal to zero, i.e. this
        /// vertex starts a straight line segment.
        /// </summary>
        /// <remarks>
        /// This is a fuzzy test using the default epsilon (<c>1e-8</c>), so a bulge may report as
        /// zero here while still being non-zero exactly.
        /// </remarks>
        /// <returns><see langword="true"/> if the segment starting here is a line segment.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BulgeIsZero() => Bulge.FuzzyEqZero();

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="Bulge"/> is strictly greater than zero,
        /// i.e. this vertex starts a counter-clockwise arc segment.
        /// </summary>
        /// <remarks>
        /// This is an exact comparison against zero, not a fuzzy one, so a vertex can be both
        /// <see cref="BulgeIsZero"/> and <see cref="BulgeIsPos"/>.
        /// </remarks>
        /// <returns><see langword="true"/> if the bulge is positive.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BulgeIsPos() => Bulge > T.Zero;

        /// <summary>
        /// Returns <see langword="true"/> if <see cref="Bulge"/> is strictly less than zero, i.e.
        /// this vertex starts a clockwise arc segment.
        /// </summary>
        /// <remarks>
        /// This is an exact comparison against zero, not a fuzzy one, so a vertex can be both
        /// <see cref="BulgeIsZero"/> and <see cref="BulgeIsNeg"/>.
        /// </remarks>
        /// <returns><see langword="true"/> if the bulge is negative.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BulgeIsNeg() => Bulge < T.Zero;

        /// <summary>
        /// Fuzzy equality comparison with another vertex using the epsilon given.
        /// </summary>
        /// <param name="other">Vertex to compare against.</param>
        /// <param name="fuzzyEpsilon">Epsilon used for all three component comparisons.</param>
        /// <returns>
        /// <see langword="true"/> if x, y and bulge are all fuzzy equal within
        /// <paramref name="fuzzyEpsilon"/>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEqEps(PlineVertex<T> other, T fuzzyEpsilon)
        {
            return X.FuzzyEq(other.X, fuzzyEpsilon)
                && Y.FuzzyEq(other.Y, fuzzyEpsilon)
                && Bulge.FuzzyEq(other.Bulge, fuzzyEpsilon);
        }

        /// <summary>
        /// Fuzzy equality comparison with another vertex using the default epsilon (<c>1e-8</c>).
        /// </summary>
        /// <param name="other">Vertex to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if x, y and bulge are all fuzzy equal within the default epsilon.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEq(PlineVertex<T> other)
        {
            return FuzzyEqEps(other, Fuzzy<T>.Epsilon);
        }

        /// <summary>Exact component-wise equality comparison with another vertex.</summary>
        /// <remarks>
        /// Matches the derived <c>PartialEq</c> upstream: IEEE 754 comparison, so <c>NaN</c> is not
        /// equal to itself and <c>0.0</c> equals <c>-0.0</c>. This makes the type an
        /// <see cref="IEquatable{T}"/> whose equality is not reflexive for <c>NaN</c> payloads.
        /// </remarks>
        /// <param name="other">Vertex to compare against.</param>
        /// <returns><see langword="true"/> if x, y and bulge all compare equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Matches Rust's derived PartialEq: IEEE 754 comparison, so NaN != NaN and 0.0 == -0.0.
        public bool Equals(PlineVertex<T> other) => X == other.X && Y == other.Y && Bulge == other.Bulge;

        /// <summary>Exact equality comparison against an arbitrary object.</summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="PlineVertex{T}"/> that
        /// compares equal according to <see cref="Equals(PlineVertex{T})"/>.
        /// </returns>
        public override bool Equals(object? obj) => obj is PlineVertex<T> other && Equals(other);

        /// <summary>Returns a hash code derived from x, y and bulge.</summary>
        /// <remarks>
        /// Because <see cref="Equals(PlineVertex{T})"/> uses IEEE 754 semantics, <c>0.0</c> and
        /// <c>-0.0</c> compare equal but do not necessarily hash equal.
        /// </remarks>
        /// <returns>A hash code for this vertex.</returns>
        public override int GetHashCode() => HashCode.Combine(X, Y, Bulge);

        /// <summary>
        /// Returns the vertex formatted as <c>[x, y, bulge]</c>, matching the upstream
        /// <c>Display</c> implementation.
        /// </summary>
        /// <returns>The string representation of this vertex.</returns>
        public override string ToString() => $"[{X}, {Y}, {Bulge}]";

        /// <summary>Exact equality operator, see <see cref="Equals(PlineVertex{T})"/>.</summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns><see langword="true"/> if both vertexes compare equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(PlineVertex<T> left, PlineVertex<T> right) => left.Equals(right);

        /// <summary>Exact inequality operator, see <see cref="Equals(PlineVertex{T})"/>.</summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns><see langword="true"/> if the vertexes do not compare equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PlineVertex<T> left, PlineVertex<T> right) => !left.Equals(right);
    }
}
