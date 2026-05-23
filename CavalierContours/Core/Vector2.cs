using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    public readonly struct Vector2<T> : IEquatable<Vector2<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly T X;
        public readonly T Y;

        public Vector2(T x, T y)
        {
            X = x;
            Y = y;
        }

        public static Vector2<T> Zero => new(T.Zero, T.Zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Scale(T scaleFactor) => new(scaleFactor * X, scaleFactor * Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dot(Vector2<T> other) => X * other.X + Y * other.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T PerpDot(Vector2<T> other) => X * other.Y - Y * other.X;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T LengthSquared() => Dot(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Length() => T.Sqrt(LengthSquared());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Normalize() => Scale(T.One / Length());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> SafeNormalize()
        {
            T eps = Fuzzy<T>.Epsilon;
            return LengthSquared() <= eps * eps ? Zero : Normalize();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEqEps(Vector2<T> other, T fuzzyEpsilon)
        {
            return X.FuzzyEq(other.X, fuzzyEpsilon) && Y.FuzzyEq(other.Y, fuzzyEpsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FuzzyEq(Vector2<T> other)
        {
            return FuzzyEqEps(other, Fuzzy<T>.Epsilon);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> Perp() => new(-Y, X);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> UnitPerp() => Perp().Normalize();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2<T> SafeUnitPerp() => Perp().SafeNormalize();

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2<T> other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object? obj) => obj is Vector2<T> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"[{X}, {Y}]";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> operator +(Vector2<T> left, Vector2<T> right) => new(left.X + right.X, left.Y + right.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> operator -(Vector2<T> left, Vector2<T> right) => new(left.X - right.X, left.Y - right.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> operator -(Vector2<T> value) => new(-value.X, -value.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2<T> left, Vector2<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2<T> left, Vector2<T> right) => !left.Equals(right);
    }

    public static class Vector2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2<T> New<T>(T x, T y) where T : struct, IFloatingPointIeee754<T>
        {
            return new Vector2<T>(x, y);
        }
    }
}
