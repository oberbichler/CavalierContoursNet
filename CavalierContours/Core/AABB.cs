using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace CavalierContours.Core
{
    public readonly struct AABB<T> : IEquatable<AABB<T>>
        where T : struct, IFloatingPointIeee754<T>
    {
        public readonly T MinX;
        public readonly T MinY;
        public readonly T MaxX;
        public readonly T MaxY;

        public AABB(T minX, T minY, T maxX, T maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public static AABB<T> Default => new(T.Zero, T.Zero, T.Zero, T.Zero);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool OverlapsAABB(in AABB<T> other)
        {
            return Overlaps(other.MinX, other.MinY, other.MaxX, other.MaxY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(T minX, T minY, T maxX, T maxY)
        {
            if (MaxX < minX || MaxY < minY || MinX > maxX || MinY > maxY)
            {
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsAABB(in AABB<T> other)
        {
            return Contains(other.MinX, other.MinY, other.MaxX, other.MaxY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T minX, T minY, T maxX, T maxY)
        {
            return MinX <= minX && MinY <= minY && MaxX >= maxX && MaxY >= maxY;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AABB<T> other)
        {
            return MinX.Equals(other.MinX) && MinY.Equals(other.MinY) &&
                   MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY);
        }

        public override bool Equals(object? obj) => obj is AABB<T> other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY);

        public override string ToString() => $"[MinX: {MinX}, MinY: {MinY}, MaxX: {MaxX}, MaxY: {MaxY}]";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AABB<T> left, AABB<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AABB<T> left, AABB<T> right) => !left.Equals(right);
    }
}
