using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    public readonly struct PlineVertex<T> : IEquatable<PlineVertex<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly T X;
        public readonly T Y;
        public readonly T Bulge;

        public PlineVertex(T x, T y, T bulge)
        {
            X = x;
            Y = y;
            Bulge = bulge;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlineVertex<T> FromSlice(ReadOnlySpan<T> slice)
        {
            if (slice.Length != 3)
            {
                throw new ArgumentException("Slice must contain exactly 3 elements", nameof(slice));
            }
            return new PlineVertex<T>(slice[0], slice[1], slice[2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlineVertex<T> FromVector2(Vector2<T> vector, T bulge)
        {
            return new PlineVertex<T>(vector.X, vector.Y, bulge);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> WithBulge(T bulge)
        {
            return new PlineVertex<T>(X, Y, bulge);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Pos() => new(X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BulgeIsZero() => Bulge.FuzzyEqZero();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BulgeIsPos() => Bulge > T.Zero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool BulgeIsNeg() => Bulge < T.Zero;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEqEps(PlineVertex<T> other, T fuzzyEpsilon)
        {
            return X.FuzzyEq(other.X, fuzzyEpsilon)
                && Y.FuzzyEq(other.Y, fuzzyEpsilon)
                && Bulge.FuzzyEq(other.Bulge, fuzzyEpsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEq(PlineVertex<T> other)
        {
            return FuzzyEqEps(other, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PlineVertex<T> other) => X.Equals(other.X) && Y.Equals(other.Y) && Bulge.Equals(other.Bulge);

        public override bool Equals(object? obj) => obj is PlineVertex<T> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y, Bulge);

        public override string ToString() => $"[{X}, {Y}, {Bulge}]";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(PlineVertex<T> left, PlineVertex<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PlineVertex<T> left, PlineVertex<T> right) => !left.Equals(right);
    }
}
