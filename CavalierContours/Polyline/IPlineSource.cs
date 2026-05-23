using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    public interface IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        int VertexCount { get; }
        bool IsClosed { get; }
        PlineVertex<T> Get(int index);
        int UserDataCount { get; }
        IEnumerable<ulong> UserDataValues { get; }
    }

    public interface IPlineSourceMut<T> : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        void SetVertex(int index, PlineVertex<T> vertex);
        void InsertVertex(int index, PlineVertex<T> vertex);
        PlineVertex<T> Remove(int index);
        void AddVertex(PlineVertex<T> vertex);
        void SetIsClosed(bool isClosed);
        void Clear();
        void ExtendVertexes(IEnumerable<PlineVertex<T>> vertexes);
        void SetUserDataValues(IEnumerable<ulong> values);
        void AddUserDataValues(IEnumerable<ulong> values);
    }

    public static class PlineSourceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            return pline.VertexCount == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlineVertex<T>? Last<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            return vc == 0 ? null : pline.Get(vc - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SegmentCount<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            if (vc < 2) return 0;
            return pline.IsClosed ? vc : vc - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextWrappingIndex<T>(this IPlineSource<T> pline, int i)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int next = i + 1;
            return next >= pline.VertexCount ? 0 : next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PrevWrappingIndex<T>(this IPlineSource<T> pline, int i)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            return i == 0 ? pline.VertexCount - 1 : i - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FwdWrappingDist<T>(this IPlineSource<T> pline, int startIndex, int endIndex)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            Debug.Assert(startIndex < vc);
            return startIndex <= endIndex ? endIndex - startIndex : vc - startIndex + endIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FwdWrappingIndex<T>(this IPlineSource<T> pline, int startIndex, int offset)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            Debug.Assert(startIndex < vc);
            Debug.Assert(offset <= vc);
            int sum = startIndex + offset;
            return sum < vc ? sum : sum - vc;
        }

        public static IEnumerable<(PlineVertex<T> V1, PlineVertex<T> V2)> IterSegments<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            if (vc < 2) yield break;

            for (int i = 0; i < vc - 1; i++)
            {
                yield return (pline.Get(i), pline.Get(i + 1));
            }

            if (pline.IsClosed)
            {
                yield return (pline.Get(vc - 1), pline.Get(0));
            }
        }

        public static IEnumerable<PlineVertex<T>> IterVertexes<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            for (int i = 0; i < vc; i++)
            {
                yield return pline.Get(i);
            }
        }

        public static IEnumerable<(int I, int J)> IterSegmentIndexes<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            if (vc < 2) yield break;

            for (int i = 0; i < vc - 1; i++)
            {
                yield return (i, i + 1);
            }

            if (pline.IsClosed)
            {
                yield return (vc - 1, 0);
            }
        }

        public static void Add<T>(this IPlineSourceMut<T> pline, T x, T y, T bulge)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            pline.AddVertex(new PlineVertex<T>(x, y, bulge));
        }

        public static bool FuzzyEqEps<T>(this IPlineSource<T> self, IPlineSource<T> other, T eps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (self.IsClosed != other.IsClosed || self.VertexCount != other.VertexCount) return false;
            int vc = self.VertexCount;
            for (int i = 0; i < vc; i++)
            {
                if (!self.Get(i).FuzzyEqEps(other.Get(i), eps)) return false;
            }
            return true;
        }

        public static bool FuzzyEq<T>(this IPlineSource<T> self, IPlineSource<T> other)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            return self.FuzzyEqEps(other, Fuzzy<T>.Epsilon);
        }

        public static AABB<T>? Extents<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (pline.SegmentCount() == 0) return null;

            var v1 = pline.Get(0);
            T minX = v1.X;
            T minY = v1.Y;
            T maxX = v1.X;
            T maxY = v1.Y;

            foreach (var (sv1, sv2) in pline.IterSegments())
            {
                if (sv1.BulgeIsZero())
                {
                    minX = T.Min(minX, sv2.X);
                    maxX = T.Max(maxX, sv2.X);
                    minY = T.Min(minY, sv2.Y);
                    maxY = T.Max(maxY, sv2.Y);
                }
                else
                {
                    AABB<T> arcBox = PlineSeg.ArcSegBoundingBox(sv1, sv2);
                    minX = T.Min(minX, arcBox.MinX);
                    minY = T.Min(minY, arcBox.MinY);
                    maxX = T.Max(maxX, arcBox.MaxX);
                    maxY = T.Max(maxY, arcBox.MaxY);
                }
            }

            return new AABB<T>(minX, minY, maxX, maxY);
        }

        public static T PathLength<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            T len = T.Zero;
            foreach (var (v1, v2) in pline.IterSegments())
            {
                len += PlineSeg.SegLength(v1, v2);
            }
            return len;
        }

        public static T Area<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (!pline.IsClosed) return T.Zero;

            T doubleTotalArea = T.Zero;
            T two = T.CreateChecked(2);
            T four = T.CreateChecked(4);

            foreach (var (v1, v2) in pline.IterSegments())
            {
                doubleTotalArea += v1.X * v2.Y - v1.Y * v2.X;
                if (!v1.BulgeIsZero())
                {
                    T b = T.Abs(v1.Bulge);
                    T sweepAngle = BaseMath.AngleFromBulge(b);
                    T triangleBase = (v2.Pos() - v1.Pos()).Length();
                    T radius = triangleBase * ((b * b + T.One) / (four * b));
                    T sagitta = b * triangleBase / two;
                    T triangleHeight = radius - sagitta;
                    T doubleSectorArea = sweepAngle * radius * radius;
                    T doubleTriangleArea = triangleBase * triangleHeight;
                    T doubleArcArea = doubleSectorArea - doubleTriangleArea;
                    if (v1.BulgeIsNeg())
                    {
                        doubleArcArea = -doubleArcArea;
                    }
                    doubleTotalArea += doubleArcArea;
                }
            }

            return doubleTotalArea / two;
        }

        public static PlineOrientation Orientation<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (!pline.IsClosed) return PlineOrientation.Open;
            return pline.Area() < T.Zero ? PlineOrientation.Clockwise : PlineOrientation.CounterClockwise;
        }

        public static Polyline<T>? RemoveRepeatPos<T>(this IPlineSource<T> pline, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            if (vc < 2) return null;

            Polyline<T>? result = null;
            Vector2<T> prevPos = pline.Get(0).Pos();

            for (int i = 1; i < vc; i++)
            {
                var v = pline.Get(i);
                bool isRepeat = v.Pos().FuzzyEqEps(prevPos, posEqualEps);

                if (isRepeat)
                {
                    if (result == null)
                    {
                        result = new Polyline<T>(pline.IsClosed);
                        for (int j = 0; j < i; j++) result.AddVertex(pline.Get(j));
                    }
                    var last = result.Last()!.Value;
                    result.SetVertex(result.VertexCount - 1, last.WithBulge(v.Bulge));
                }
                else
                {
                    result?.AddVertex(v);
                    prevPos = v.Pos();
                }
            }

            if (pline.IsClosed && pline.Last()!.Value.Pos().FuzzyEqEps(pline.Get(0).Pos(), posEqualEps))
            {
                if (result == null)
                {
                    result = new Polyline<T>(pline.IsClosed);
                    for (int j = 0; j < vc; j++) result.AddVertex(pline.Get(j));
                }
                result.RemoveAt(result.VertexCount - 1);
            }

            return result;
        }

        public static StaticAABB2DIndex<T> CreateApproxAabbIndex<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            if (vc < 2) return new StaticAABB2DIndexBuilder<T>(0).Build();

            int segCount = pline.IsClosed ? vc : vc - 1;
            var builder = new StaticAABB2DIndexBuilder<T>(segCount);

            foreach (var (v1, v2) in pline.IterSegments())
            {
                AABB<T> approxAabb = PlineSeg.SegFastApproxBoundingBox(v1, v2);
                builder.Add(approxAabb.MinX, approxAabb.MinY, approxAabb.MaxX, approxAabb.MaxY);
            }

            return builder.Build();
        }

        public static StaticAABB2DIndex<T> CreateAabbIndex<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            if (vc < 2) return new StaticAABB2DIndexBuilder<T>(0).Build();

            int segCount = pline.IsClosed ? vc : vc - 1;
            var builder = new StaticAABB2DIndexBuilder<T>(segCount);

            foreach (var (v1, v2) in pline.IterSegments())
            {
                AABB<T> actualAabb = PlineSeg.SegBoundingBox(v1, v2);
                builder.Add(actualAabb.MinX, actualAabb.MinY, actualAabb.MaxX, actualAabb.MaxY);
            }

            return builder.Build();
        }

        public static ClosestPointResult<T>? ClosestPoint<T>(this IPlineSource<T> pline, Vector2<T> point, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (pline.IsEmpty()) return null;

            Vector2<T> firstPos = pline.Get(0).Pos();
            if (pline.VertexCount == 1)
            {
                T dist = (firstPos - point).Length();
                return new ClosestPointResult<T>(0, firstPos, dist);
            }

            int bestIndex = 0;
            Vector2<T> bestPoint = firstPos;
            T bestDistSq = T.MaxValue;

            foreach (var (i, j) in pline.IterSegmentIndexes())
            {
                var v1 = pline.Get(i);
                var v2 = pline.Get(j);
                Vector2<T> cp = PlineSeg.SegClosestPoint(v1, v2, point, posEqualEps);
                T distSq = (point - cp).LengthSquared();
                if (distSq < bestDistSq)
                {
                    bestIndex = i;
                    bestPoint = cp;
                    bestDistSq = distSq;
                }
            }

            return new ClosestPointResult<T>(bestIndex, bestPoint, T.Sqrt(bestDistSq));
        }

        public static int WindingNumber<T>(this IPlineSource<T> pline, Vector2<T> point)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (!pline.IsClosed || pline.VertexCount < 2) return 0;

            int ProcessLineWinding(PlineVertex<T> v1, PlineVertex<T> v2, Vector2<T> pt)
            {
                int r = 0;
                if (v1.Y <= pt.Y)
                {
                    if (v2.Y > pt.Y && BaseMath.IsLeft(v1.Pos(), v2.Pos(), pt))
                    {
                        r += 1;
                    }
                }
                else if (v2.Y <= pt.Y && !BaseMath.IsLeft(v1.Pos(), v2.Pos(), pt))
                {
                    r -= 1;
                }
                return r;
            }

            int ProcessArcWinding(PlineVertex<T> v1, PlineVertex<T> v2, Vector2<T> pt)
            {
                bool isCcw = v1.BulgeIsPos();
                bool pointIsLeft = isCcw ? BaseMath.IsLeft(v1.Pos(), v2.Pos(), pt) : BaseMath.IsLeftOrEqual(v1.Pos(), v2.Pos(), pt);

                bool DistToCenterLessThanRadius()
                {
                    (T arcRadius, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                    T dist2 = BaseMath.DistSquared(arcCenter, pt);
                    return dist2 < arcRadius * arcRadius;
                }

                int r = 0;
                if (v1.Y <= pt.Y)
                {
                    if (v2.Y > pt.Y)
                    {
                        if (isCcw)
                        {
                            if (pointIsLeft || DistToCenterLessThanRadius()) r += 1;
                        }
                        else if (pointIsLeft && !DistToCenterLessThanRadius())
                        {
                            r += 1;
                        }
                    }
                    else
                    {
                        if (isCcw && !pointIsLeft && v2.X < pt.X && pt.X < v1.X && DistToCenterLessThanRadius()) r += 1;
                        else if (!isCcw && pointIsLeft && v1.X < pt.X && pt.X < v2.X && DistToCenterLessThanRadius()) r -= 1;
                    }
                }
                else if (v2.Y <= pt.Y)
                {
                    if (isCcw)
                    {
                        if (!pointIsLeft && !DistToCenterLessThanRadius()) r -= 1;
                    }
                    else if (pointIsLeft)
                    {
                        if (DistToCenterLessThanRadius()) r -= 1;
                    }
                    else
                    {
                        r -= 1;
                    }
                }
                else
                {
                    if (isCcw && !pointIsLeft && v1.X < pt.X && pt.X < v2.X && DistToCenterLessThanRadius()) r += 1;
                    else if (!isCcw && pointIsLeft && v2.X < pt.X && pt.X < v1.X && DistToCenterLessThanRadius()) r -= 1;
                }
                return r;
            }

            int winding = 0;
            foreach (var (v1, v2) in pline.IterSegments())
            {
                winding += v1.BulgeIsZero() ? ProcessLineWinding(v1, v2, point) : ProcessArcWinding(v1, v2, point);
            }
            return winding;
        }

        public static Polyline<T> ArcsToApproxLines<T>(this IPlineSource<T> pline, T errorDistance)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new Polyline<T>(pline.IsClosed);
            if (pline.VertexCount == 0) return result;

            T absError = T.Abs(errorDistance);

            foreach (var (v1, v2) in pline.IterSegments())
            {
                if (v1.BulgeIsZero())
                {
                    result.Add(v1.X, v1.Y, T.Zero);
                    continue;
                }

                (T arcRadius, Vector2<T> arcCenter) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                if (arcRadius.FuzzyLt(errorDistance))
                {
                    result.Add(v1.X, v1.Y, T.Zero);
                    continue;
                }

                T startAngle = BaseMath.Angle(arcCenter, v1.Pos());
                T endAngle = BaseMath.Angle(arcCenter, v2.Pos());
                T angleDiff = T.Abs(BaseMath.DeltaAngle(startAngle, endAngle));

                T two = T.CreateChecked(2);
                T segSubAngle = two * T.Acos(T.Abs(T.One - absError / arcRadius));
                T segCount = T.Ceiling(angleDiff / segSubAngle);
                T segAngleOffset = v1.BulgeIsNeg() ? -angleDiff / segCount : angleDiff / segCount;

                result.Add(v1.X, v1.Y, T.Zero);
                int intCount = int.CreateChecked(segCount);
                for (int i = 1; i < intCount; i++)
                {
                    T anglePos = T.CreateChecked(i);
                    T angle = anglePos * segAngleOffset + startAngle;
                    Vector2<T> pos = BaseMath.PointOnCircle(arcRadius, arcCenter, angle);
                    result.Add(pos.X, pos.Y, T.Zero);
                }
            }

            if (!pline.IsClosed)
            {
                result.AddVertex(pline.Last()!.Value);
            }

            return result;
        }

        public static (bool Success, int SegIndex, Vector2<T> Point, T AccLength) FindPointAtPathLength<T>(this IPlineSource<T> pline, T targetPathLength)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            if (targetPathLength <= T.Zero)
            {
                return (true, 0, pline.Get(0).Pos(), T.Zero);
            }

            T accLength = T.Zero;
            int i = 0;
            foreach (var (v1, v2) in pline.IterSegments())
            {
                T segLen = PlineSeg.SegLength(v1, v2);
                T sumLen = accLength + segLen;
                if (sumLen < targetPathLength)
                {
                    accLength = sumLen;
                    i++;
                    continue;
                }

                T t = (targetPathLength - accLength) / segLen;

                if (v1.BulgeIsZero())
                {
                    Vector2<T> pt = v1.Pos() + (v2.Pos() - v1.Pos()).Scale(t);
                    return (true, i, pt, accLength);
                }
                else
                {
                    (T radius, Vector2<T> center) = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                    T startAngle = BaseMath.Angle(center, v1.Pos());
                    T totalSweepAngle = BaseMath.AngleFromBulge(v1.Bulge);
                    T targetAngle = startAngle + totalSweepAngle * t;

                    Vector2<T> pt = BaseMath.PointOnCircle(radius, center, targetAngle);
                    return (true, i, pt, accLength);
                }
            }

            return (false, 0, default, accLength);
        }

        public static void AddOrReplaceVertex<T>(this IPlineSourceMut<T> self, PlineVertex<T> vertex, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = self.VertexCount;
            if (vc == 0)
            {
                self.AddVertex(vertex);
                return;
            }

            var last = self.Get(vc - 1);
            if (last.Pos().FuzzyEqEps(vertex.Pos(), posEqualEps))
            {
                self.SetVertex(vc - 1, last.WithBulge(vertex.Bulge));
                return;
            }

            self.AddVertex(vertex);
        }

        public static void ExtendRemoveRepeat<T>(this IPlineSourceMut<T> self, IPlineSource<T> other, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int otherCount = other.VertexCount;
            for (int i = 0; i < otherCount; i++)
            {
                self.AddOrReplaceVertex(other.Get(i), posEqualEps);
            }
        }

        public static O CreateFromRemoveRepeat<O, T>(IPlineSource<T> pline, T posEqualEps)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new O();
            result.SetIsClosed(pline.IsClosed);
            int plineCount = pline.VertexCount;
            for (int i = 0; i < plineCount; i++)
            {
                result.AddOrReplaceVertex(pline.Get(i), posEqualEps);
            }

            if (pline.IsClosed && result.VertexCount >= 2)
            {
                var last = result.Get(result.VertexCount - 1);
                if (last.Pos().FuzzyEqEps(result.Get(0).Pos(), posEqualEps))
                {
                    result.Remove(result.VertexCount - 1);
                }
            }

            result.SetUserDataValues(pline.UserDataValues);
            return result;
        }

        public static O CreateFrom<O, T>(IPlineSource<T> pline)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            var result = new O();
            result.SetIsClosed(pline.IsClosed);
            int count = pline.VertexCount;
            for (int i = 0; i < count; i++)
            {
                result.AddVertex(pline.Get(i));
            }
            result.SetUserDataValues(pline.UserDataValues);
            return result;
        }

        private enum RedundantCase
        {
            IncludeVertex,
            DiscardVertex,
            UpdateV1BulgeForArc
        }

        public static Polyline<T>? RemoveRedundant<T>(this IPlineSource<T> self, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = self.VertexCount;
            if (vc < 2)
            {
                return null;
            }

            if (vc == 2)
            {
                var v1_val = self.Get(0);
                var v2_val = self.Get(1);
                if (v1_val.Pos().FuzzyEqEps(v2_val.Pos(), posEqualEps))
                {
                    var res = new Polyline<T>(1, self.IsClosed);
                    res.AddVertex(v2_val); // take bulge from last vertex
                    res.SetUserDataValues(self.UserDataValues);
                    return res;
                }
                return null;
            }

            bool IsCollinearSameDir(PlineVertex<T> v1_val, PlineVertex<T> v2_val, PlineVertex<T> v3_val)
            {
                if (v2_val.Pos().FuzzyEqEps(v3_val.Pos(), posEqualEps))
                {
                    return true;
                }

                bool collinear = (v1_val.X * (v2_val.Y - v3_val.Y) + v2_val.X * (v3_val.Y - v1_val.Y) + v3_val.X * (v1_val.Y - v2_val.Y))
                    .FuzzyEqZero(posEqualEps);
                bool sameDirection = (v3_val.Pos() - v2_val.Pos()).Dot(v2_val.Pos() - v1_val.Pos()) > -posEqualEps;

                return collinear && sameDirection;
            }

            var v1 = self.Get(0);
            var v2 = self.Get(1);

            // remove all repeat positions at the start
            int i = 2;
            while (v1.Pos().FuzzyEqEps(v2.Pos(), posEqualEps))
            {
                v1 = v1.WithBulge(v2.Bulge);
                if (i >= vc)
                {
                    break;
                }
                v2 = self.Get(i);
                i += 1;
            }

            Polyline<T> CopySelf(int count)
            {
                var pl = new Polyline<T>(count, self.IsClosed);
                for (int idx = 0; idx < count; idx++)
                {
                    pl.AddVertex(self.Get(idx));
                }
                pl.SetUserDataValues(self.UserDataValues);
                return pl;
            }

            Polyline<T>? result = null;
            if (i != 2)
            {
                var pl = new Polyline<T>(1, self.IsClosed);
                pl.AddVertex(v1);
                result = pl;
            }

            if (i >= vc)
            {
                result?.SetUserDataValues(self.UserDataValues);
                return result;
            }

            (T Radius, Vector2<T> Center)? v1_v2_arc = null;
            bool v1BulgeIsZero = v1.BulgeIsZero();
            bool v2BulgeIsZero = v2.BulgeIsZero();
            bool v1BulgeIsPos = v1.BulgeIsPos();
            bool v2BulgeIsPos = v2.BulgeIsPos();

            int iterCount = self.IsClosed ? vc - 1 : vc - 2;
            int enumIndex = i;

            for (int step = 0; step < iterCount; step++, enumIndex++)
            {
                var v3 = self.Get(enumIndex % vc);
                RedundantCase state;
                T computedBulge = T.Zero;

                if (v2.Pos().FuzzyEqEps(v3.Pos(), posEqualEps))
                {
                    state = RedundantCase.DiscardVertex;
                }
                else if (v1BulgeIsZero && v2BulgeIsZero)
                {
                    bool isFinalVertexForOpen = !self.IsClosed && enumIndex == vc;
                    if (!isFinalVertexForOpen && IsCollinearSameDir(v1, v2, v3))
                    {
                        state = RedundantCase.DiscardVertex;
                    }
                    else
                    {
                        state = RedundantCase.IncludeVertex;
                    }
                }
                else if (!v1BulgeIsZero
                    && !v2BulgeIsZero
                    && (v1BulgeIsPos == v2BulgeIsPos)
                    && !v2.Pos().FuzzyEqEps(v3.Pos(), posEqualEps))
                {
                    if (!v1_v2_arc.HasValue)
                    {
                        v1_v2_arc = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                    }
                    var (arcRadius1, arcCenter1) = v1_v2_arc.Value;
                    var (arcRadius2, arcCenter2) = PlineSeg.SegArcRadiusAndCenter(v2, v3);

                    if (arcRadius1.FuzzyEq(arcRadius2, posEqualEps)
                        && arcCenter1.FuzzyEqEps(arcCenter2, posEqualEps))
                    {
                        T angle1 = BaseMath.Angle(arcCenter1, v1.Pos());
                        T angle2 = BaseMath.Angle(arcCenter1, v2.Pos());
                        T angle3 = BaseMath.Angle(arcCenter1, v3.Pos());
                        T totalSweep = T.Abs(BaseMath.DeltaAngle(angle1, angle2)) + T.Abs(BaseMath.DeltaAngle(angle2, angle3));

                        T two = T.CreateChecked(2);
                        T avgRadius = (arcRadius1 + arcRadius2) / two;

                        if ((avgRadius * totalSweep).FuzzyLt(avgRadius * T.Pi, posEqualEps))
                        {
                            computedBulge = v1BulgeIsPos ? BaseMath.BulgeFromAngle(totalSweep) : -BaseMath.BulgeFromAngle(totalSweep);
                            state = RedundantCase.UpdateV1BulgeForArc;
                        }
                        else
                        {
                            state = RedundantCase.IncludeVertex;
                        }
                    }
                    else
                    {
                        state = RedundantCase.IncludeVertex;
                    }
                }
                else
                {
                    state = RedundantCase.IncludeVertex;
                }

                switch (state)
                {
                    case RedundantCase.IncludeVertex:
                        if (result != null)
                        {
                            result.AddVertex(v2);
                        }
                        v1 = v2;
                        v2 = v3;
                        v1_v2_arc = null;
                        v1BulgeIsZero = v2BulgeIsZero;
                        v2BulgeIsZero = v3.BulgeIsZero();
                        v1BulgeIsPos = v2BulgeIsPos;
                        v2BulgeIsPos = v3.BulgeIsPos();
                        break;

                    case RedundantCase.DiscardVertex:
                        if (result == null)
                        {
                            result = CopySelf(enumIndex - 1);
                        }
                        v2 = v3;
                        v1_v2_arc = null;
                        v2BulgeIsZero = v3.BulgeIsZero();
                        v2BulgeIsPos = v3.BulgeIsPos();
                        break;

                    case RedundantCase.UpdateV1BulgeForArc:
                        if (result == null)
                        {
                            result = CopySelf(enumIndex - 1);
                        }
                        var lastVertex = result.Get(result.VertexCount - 1);
                        result.SetVertex(result.VertexCount - 1, lastVertex.WithBulge(computedBulge));
                        v1 = v1.WithBulge(computedBulge);
                        v2 = v3;
                        v1BulgeIsZero = v2BulgeIsZero;
                        v2BulgeIsZero = v3.BulgeIsZero();
                        v1BulgeIsPos = v2BulgeIsPos;
                        v2BulgeIsPos = v3.BulgeIsPos();
                        break;
                }
            }

            if (self.IsClosed)
            {
                if (result != null)
                {
                    if (result.Get(result.VertexCount - 1).Pos().FuzzyEqEps(result.Get(0).Pos(), posEqualEps))
                    {
                        result.RemoveAt(result.VertexCount - 1);
                    }
                }
                else
                {
                    if (self.Get(vc - 1).Pos().FuzzyEqEps(self.Get(0).Pos(), posEqualEps))
                    {
                        result = CopySelf(vc);
                        result.RemoveAt(result.VertexCount - 1);
                    }
                }

                var v3 = (result != null) ? result.Get(1) : self.Get(1);

                if (v1BulgeIsZero && v2BulgeIsZero && IsCollinearSameDir(v1, v2, v3))
                {
                    if (result == null)
                    {
                        result = CopySelf(vc);
                    }
                    var lastVertex = result.Remove(result.VertexCount - 1);
                    result.SetVertex(0, lastVertex);
                }
                else if (!v1BulgeIsZero
                    && !v2BulgeIsZero
                    && (v1BulgeIsPos == v2BulgeIsPos)
                    && !v2.Pos().FuzzyEqEps(v3.Pos(), posEqualEps))
                {
                    if (!v1_v2_arc.HasValue)
                    {
                        v1_v2_arc = PlineSeg.SegArcRadiusAndCenter(v1, v2);
                    }
                    var (arcRadius1, arcCenter1) = v1_v2_arc.Value;
                    var (arcRadius2, arcCenter2) = PlineSeg.SegArcRadiusAndCenter(v2, v3);

                    if (arcRadius1.FuzzyEq(arcRadius2, posEqualEps)
                        && arcCenter1.FuzzyEqEps(arcCenter2, posEqualEps))
                    {
                        T angle1 = BaseMath.Angle(arcCenter1, v1.Pos());
                        T angle2 = BaseMath.Angle(arcCenter1, v2.Pos());
                        T angle3 = BaseMath.Angle(arcCenter1, v3.Pos());
                        T totalSweep = T.Abs(BaseMath.DeltaAngle(angle1, angle2)) + T.Abs(BaseMath.DeltaAngle(angle2, angle3));

                        T two = T.CreateChecked(2);
                        T avgRadius = (arcRadius1 + arcRadius2) / two;
                        if ((avgRadius * totalSweep).FuzzyLt(avgRadius * T.Pi, posEqualEps))
                        {
                            T bulge = v1BulgeIsPos ? BaseMath.BulgeFromAngle(totalSweep) : -BaseMath.BulgeFromAngle(totalSweep);
                            if (result == null)
                            {
                                result = CopySelf(vc);
                            }
                            var lastVertex = result.Remove(result.VertexCount - 1);
                            result.SetVertex(0, lastVertex.WithBulge(bulge));
                        }
                    }
                }
            }
            else
            {
                if (result != null)
                {
                    result.AddOrReplaceVertex(self.Get(vc - 1), posEqualEps);
                }
                else
                {
                    if (self.Get(vc - 2).FuzzyEqEps(self.Get(vc - 1), posEqualEps))
                    {
                        result = CopySelf(vc);
                        result.RemoveAt(result.VertexCount - 1);
                    }
                }
            }

            result?.SetUserDataValues(self.UserDataValues);
            return result;
        }

        public static Polyline<T>? RotateStart<T>(this IPlineSource<T> self, int startIndex, Vector2<T> point, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = self.VertexCount;
            if (!self.IsClosed || vc < 2 || startIndex < 0 || startIndex > vc - 1)
            {
                return null;
            }

            IEnumerable<PlineVertex<T>> WrappingVertexesStartingAt(int start)
            {
                for (int idx = start; idx < vc; idx++)
                {
                    yield return self.Get(idx);
                }
                for (int idx = 0; idx < start; idx++)
                {
                    yield return self.Get(idx);
                }
            }

            var startV = self.Get(startIndex);
            Polyline<T> result;

            if (startV.Pos().FuzzyEqEps(point, posEqualEps))
            {
                result = new Polyline<T>(vc, true);
                result.ExtendVertexes(WrappingVertexesStartingAt(startIndex));
            }
            else
            {
                int nextIndex = self.NextWrappingIndex(startIndex);
                if (point.FuzzyEqEps(self.Get(nextIndex).Pos(), posEqualEps))
                {
                    result = new Polyline<T>(vc, true);
                    result.ExtendVertexes(WrappingVertexesStartingAt(nextIndex));
                }
                else
                {
                    result = new Polyline<T>(vc + 1, true);
                    var split = PlineSeg.SegSplitAtPoint(
                        self.Get(startIndex),
                        self.Get(nextIndex),
                        point,
                        posEqualEps
                    );
                    result.AddVertex(split.SplitVertex);
                    result.ExtendVertexes(WrappingVertexesStartingAt(nextIndex));
                    result.SetVertex(result.VertexCount - 1, split.UpdatedStart);
                }
            }

            result.SetUserDataValues(self.UserDataValues);
            return result;
        }

        public static void InvertDirection<T>(this IPlineSourceMut<T> self)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = self.VertexCount;
            if (vc < 2) return;

            int start = 0;
            int end = vc - 1;
            while (start < end)
            {
                var s = self.Get(start);
                var e = self.Get(end);
                self.SetVertex(start, e);
                self.SetVertex(end, s);
                start++;
                end--;
            }

            T firstBulge = self.Get(0).Bulge;
            for (int i = 1; i < vc; i++)
            {
                T b = -self.Get(i).Bulge;
                self.SetVertex(i - 1, self.Get(i - 1).WithBulge(b));
            }

            if (self.IsClosed)
            {
                self.SetVertex(vc - 1, self.Get(vc - 1).WithBulge(-firstBulge));
            }
        }
    }
}
