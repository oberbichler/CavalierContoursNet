using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// A read-only source of polyline data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A polyline is a sequence of vertexes plus a flag indicating whether it is closed, meaning
    /// the last vertex forms a segment with the first vertex, or open, meaning there is no segment
    /// between last and first. Polylines can represent complex 2D shapes made of straight line
    /// segments and circular arc segments.
    /// </para>
    /// <para>
    /// Each vertex has a 2D position and a bulge value; the bulge determines the curvature of the
    /// segment from that vertex to the next one. A bulge of zero gives a straight line, a positive
    /// bulge a counter-clockwise arc, a negative bulge a clockwise arc, and the magnitude sets the
    /// curvature. See <see cref="PlineVertex{T}.Bulge"/> for the exact definition.
    /// </para>
    /// <para>
    /// This interface only declares raw data access. The operations that can be performed on a
    /// read-only polyline are provided as extension methods on
    /// <see cref="PlineSourceExtensions"/>. For the mutable counterpart see
    /// <see cref="IPlineSourceMut{T}"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Floating point type used for the vertex coordinates and bulges.</typeparam>
    public interface IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>Total number of vertexes in the polyline.</summary>
        int VertexCount { get; }

        /// <summary>
        /// Whether the polyline is closed (last vertex forms a segment with the first vertex) or
        /// open (no segment between last and first vertex).
        /// </summary>
        bool IsClosed { get; }

        /// <summary>Gets the vertex at the given index position.</summary>
        /// <remarks>
        /// This corresponds to the upstream <c>at</c>, not to the <c>Option</c>-returning
        /// <c>get</c>: implementations are expected to signal out of range indexes rather than
        /// returning a sentinel.
        /// </remarks>
        /// <param name="index">Zero based vertex index.</param>
        /// <returns>The vertex at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of bounds.
        /// </exception>
        PlineVertex<T> Get(int index);

        /// <summary>
        /// Number of user data values stored with this polyline.
        /// </summary>
        /// <remarks>
        /// User data values are 64-bit unsigned integers that an application may associate with a
        /// polyline. They are preserved across offset calls, and a polyline composed from slices of
        /// several source polylines carries the user data of each of them, so values may repeat.
        /// </remarks>
        int UserDataCount { get; }

        /// <summary>
        /// The user data values stored with this polyline, see <see cref="UserDataCount"/>.
        /// </summary>
        IReadOnlyList<ulong> UserDataValues { get; }
    }

    /// <summary>
    /// A mutable source of polyline data.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="IPlineSource{T}"/> with the operations that modify the vertex sequence,
    /// the closed flag and the user data. Further mutating operations built on top of these are
    /// provided as extension methods on <see cref="PlineSourceExtensions"/>.
    /// </remarks>
    /// <typeparam name="T">Floating point type used for the vertex coordinates and bulges.</typeparam>
    public interface IPlineSourceMut<T> : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>Replaces the vertex data at the given index position.</summary>
        /// <param name="index">Zero based vertex index.</param>
        /// <param name="vertex">New vertex data.</param>
        void SetVertex(int index, PlineVertex<T> vertex);

        /// <summary>Inserts a new vertex at the given index position.</summary>
        /// <param name="index">Zero based index at which the vertex is inserted.</param>
        /// <param name="vertex">Vertex to insert.</param>
        void InsertVertex(int index, PlineVertex<T> vertex);

        /// <summary>Removes the vertex at the given index position and returns it.</summary>
        /// <param name="index">Zero based vertex index.</param>
        /// <returns>The vertex that was removed.</returns>
        PlineVertex<T> Remove(int index);

        /// <summary>Appends a vertex to the end of the polyline.</summary>
        /// <param name="vertex">Vertex to append.</param>
        void AddVertex(PlineVertex<T> vertex);

        /// <summary>Sets whether the polyline is closed or open.</summary>
        /// <param name="isClosed">
        /// <see langword="true"/> for a closed polyline, <see langword="false"/> for an open one.
        /// </param>
        void SetIsClosed(bool isClosed);

        /// <summary>Removes all vertexes of the polyline.</summary>
        void Clear();

        /// <summary>Appends all vertexes from the sequence given to the end of the polyline.</summary>
        /// <param name="vertexes">Vertexes to append, in order.</param>
        void ExtendVertexes(IEnumerable<PlineVertex<T>> vertexes);

        /// <summary>
        /// Clears all existing user data values and replaces them with the values provided.
        /// </summary>
        /// <param name="values">New user data values.</param>
        void SetUserDataValues(IEnumerable<ulong> values);

        /// <summary>Appends user data values to the values already stored.</summary>
        /// <param name="values">User data values to append.</param>
        void AddUserDataValues(IEnumerable<ulong> values);
    }

    /// <summary>
    /// The operations that can be performed on polylines exposed through
    /// <see cref="IPlineSource{T}"/> and <see cref="IPlineSourceMut{T}"/>.
    /// </summary>
    /// <remarks>
    /// These correspond to the default methods of the upstream <c>PlineSource</c>,
    /// <c>PlineSourceMut</c> and <c>PlineCreation</c> traits, which C# expresses as extension
    /// methods so that implementers only have to supply raw data access.
    /// </remarks>
    public static class PlineSourceExtensions
    {
        /// <summary>Returns whether the polyline has no vertexes.</summary>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to test.</param>
        /// <returns><see langword="true"/> if the vertex count is zero.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            return pline.VertexCount == 0;
        }

        /// <summary>Gets the last vertex of the polyline.</summary>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to read from.</param>
        /// <returns>
        /// The last vertex, or <see langword="null"/> if the polyline is empty.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlineVertex<T>? Last<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            int vc = pline.VertexCount;
            return vc == 0 ? null : pline.Get(vc - 1);
        }

        /// <summary>Total number of segments in the polyline.</summary>
        /// <remarks>
        /// Zero for fewer than two vertexes; otherwise the vertex count for a closed polyline,
        /// because of the implicit closing segment, and the vertex count minus one for an open one.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to inspect.</param>
        /// <returns>The number of segments.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SegmentCount<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            int vc = pline.VertexCount;
            if (vc < 2) return 0;
            return pline.IsClosed ? vc : vc - 1;
        }

        /// <summary>Returns the next vertex index, wrapping around to zero at the end.</summary>
        /// <remarks>
        /// <para>
        /// The polyline is treated as circular <em>regardless of <see cref="IPlineSource{T}.IsClosed"/></em>:
        /// this method does not consult the closed flag at all. If
        /// <c>i + 1 &gt;= VertexCount</c> then <c>0</c> is returned, otherwise <c>i + 1</c>.
        /// </para>
        /// <para>
        /// For a closed polyline the wrap is geometrically meaningful, since index 0 really does
        /// follow the last vertex along the closing segment. For an open polyline the wrap is a
        /// pure index operation with no geometric meaning: there is no segment from the last vertex
        /// back to the first, so callers must not treat the wrapped result as a continuation of the
        /// path.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline whose vertex count defines the wrap point.</param>
        /// <param name="i">Current vertex index.</param>
        /// <returns><c>i + 1</c>, or <c>0</c> if that would reach or pass the vertex count.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextWrappingIndex<T>(this IPlineSource<T> pline, int i)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            int next = i + 1;
            return next >= pline.VertexCount ? 0 : next;
        }

        /// <summary>
        /// Returns the previous vertex index, wrapping around to the last vertex at index zero.
        /// </summary>
        /// <remarks>
        /// <para>
        /// As with <see cref="NextWrappingIndex{T}(IPlineSource{T}, int)"/> the polyline is treated
        /// as circular <em>regardless of <see cref="IPlineSource{T}.IsClosed"/></em>. If <c>i</c>
        /// is <c>0</c> then <c>VertexCount - 1</c> is returned, otherwise <c>i - 1</c>.
        /// </para>
        /// <para>
        /// For a closed polyline the wrap follows the closing segment backwards. For an open
        /// polyline it is a pure index operation and jumping from index 0 to the last vertex does
        /// not correspond to any segment. On an empty polyline the result is <c>-1</c>.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline whose vertex count defines the wrap point.</param>
        /// <param name="i">Current vertex index.</param>
        /// <returns><c>i - 1</c>, or <c>VertexCount - 1</c> if <paramref name="i"/> is zero.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PrevWrappingIndex<T>(this IPlineSource<T> pline, int i)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            return i == 0 ? pline.VertexCount - 1 : i - 1;
        }

        /// <summary>
        /// Returns the number of forward steps needed to walk from one vertex index to another,
        /// wrapping past the end of the vertex list if necessary.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If <paramref name="startIndex"/> is less than or equal to <paramref name="endIndex"/>
        /// the result is simply their difference; otherwise it is
        /// <c>VertexCount - startIndex + endIndex</c>. Equal indexes give zero, never the full
        /// vertex count.
        /// </para>
        /// <para>
        /// <see cref="IPlineSource{T}.IsClosed"/> is not consulted. Only for a closed polyline does
        /// a wrapping result count actual segments; for an open polyline a wrapping distance counts
        /// a step from the last vertex to the first that does not exist geometrically, so callers
        /// working on open polylines must ensure <paramref name="startIndex"/> does not exceed
        /// <paramref name="endIndex"/>.
        /// </para>
        /// <para>
        /// <paramref name="startIndex"/> is assumed to be in range; this is only asserted in debug
        /// builds.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline whose vertex count defines the wrap point.</param>
        /// <param name="startIndex">Index to start from, must be less than the vertex count.</param>
        /// <param name="endIndex">Index to walk to.</param>
        /// <returns>The forward wrapping distance between the two indexes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FwdWrappingDist<T>(this IPlineSource<T> pline, int startIndex, int endIndex)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            int vc = pline.VertexCount;
            Debug.Assert(startIndex < vc);
            return startIndex <= endIndex ? endIndex - startIndex : vc - startIndex + endIndex;
        }

        /// <summary>
        /// Returns the vertex index reached by advancing <paramref name="offset"/> positions from
        /// <paramref name="startIndex"/>, wrapping past the end of the vertex list.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The inverse of <see cref="FwdWrappingDist{T}(IPlineSource{T}, int, int)"/>. The sum of
        /// index and offset is reduced by the vertex count at most once, so an offset equal to the
        /// vertex count maps back onto <paramref name="startIndex"/>.
        /// </para>
        /// <para>
        /// <see cref="IPlineSource{T}.IsClosed"/> is not consulted, so the same caveat applies as
        /// for the other wrapping helpers: on an open polyline a wrapped index is not a
        /// continuation of the path.
        /// </para>
        /// <para>
        /// <paramref name="startIndex"/> is assumed to be in range and <paramref name="offset"/> is
        /// assumed not to wrap more than once; both are only asserted in debug builds. A larger
        /// offset silently returns an out of range index.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline whose vertex count defines the wrap point.</param>
        /// <param name="startIndex">Index to start from, must be less than the vertex count.</param>
        /// <param name="offset">
        /// Number of positions to advance, must not be greater than the vertex count.
        /// </param>
        /// <returns>The vertex index after applying the offset.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FwdWrappingIndex<T>(this IPlineSource<T> pline, int startIndex, int offset)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            int vc = pline.VertexCount;
            Debug.Assert(startIndex < vc);
            Debug.Assert(offset <= vc);
            int sum = startIndex + offset;
            return sum < vc ? sum : sum - vc;
        }

        /// <summary>Iterates over all polyline segments as vertex pairs.</summary>
        /// <remarks>
        /// Yields <c>(v[0], v[1])</c> up to <c>(v[n-2], v[n-1])</c>, and for a closed polyline the
        /// additional closing pair <c>(v[n-1], v[0])</c>. Nothing is yielded for fewer than two
        /// vertexes. The shape of each segment is defined by the bulge of the first vertex of the
        /// pair.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to iterate.</param>
        /// <returns>A lazily evaluated sequence of segment vertex pairs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static IEnumerable<(PlineVertex<T> V1, PlineVertex<T> V2)> IterSegments<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            // Validated in a non-iterator wrapper: the body of an iterator method does not run
            // until the first MoveNext, which would defer the guard past the offending call.
            ArgumentNullException.ThrowIfNull(pline);
            return IterSegmentsCore(pline);
        }

        private static IEnumerable<(PlineVertex<T> V1, PlineVertex<T> V2)> IterSegmentsCore<T>(IPlineSource<T> pline)
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

        /// <summary>Iterates over all polyline vertexes in order.</summary>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to iterate.</param>
        /// <returns>A lazily evaluated sequence of all vertexes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static IEnumerable<PlineVertex<T>> IterVertexes<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            // See IterSegments for why the guard lives in a non-iterator wrapper.
            ArgumentNullException.ThrowIfNull(pline);
            return IterVertexesCore(pline);
        }

        private static IEnumerable<PlineVertex<T>> IterVertexesCore<T>(IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            int vc = pline.VertexCount;
            for (int i = 0; i < vc; i++)
            {
                yield return pline.Get(i);
            }
        }

        /// <summary>Iterates over all polyline segments as pairs of vertex index positions.</summary>
        /// <remarks>
        /// Yields <c>(0, 1)</c> up to <c>(n-2, n-1)</c>, and for a closed polyline the additional
        /// closing pair <c>(n-1, 0)</c>, where <c>n</c> is the vertex count. Nothing is yielded for
        /// fewer than two vertexes.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to iterate.</param>
        /// <returns>A lazily evaluated sequence of segment index pairs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static IEnumerable<(int I, int J)> IterSegmentIndexes<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            // See IterSegments for why the guard lives in a non-iterator wrapper.
            ArgumentNullException.ThrowIfNull(pline);
            return IterSegmentIndexesCore(pline);
        }

        private static IEnumerable<(int I, int J)> IterSegmentIndexesCore<T>(IPlineSource<T> pline)
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

        /// <summary>
        /// Appends a vertex to the end of the polyline, accepting the components rather than a
        /// vertex structure.
        /// </summary>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to append to.</param>
        /// <param name="x">X coordinate position of the new vertex.</param>
        /// <param name="y">Y coordinate position of the new vertex.</param>
        /// <param name="bulge">
        /// Bulge of the segment starting at the new vertex, see <see cref="PlineVertex{T}.Bulge"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static void Add<T>(this IPlineSourceMut<T> pline, T x, T y, T bulge)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            pline.AddVertex(new PlineVertex<T>(x, y, bulge));
        }

        /// <summary>
        /// Fuzzy comparison against another polyline using the epsilon given for the vertex
        /// comparisons.
        /// </summary>
        /// <remarks>
        /// The polylines must agree in their closed flag and vertex count, and every vertex pair
        /// must be fuzzy equal in x, y and bulge. This is a vertex-wise comparison, not a
        /// geometric one: two polylines tracing the same path with different vertex layouts are not
        /// equal.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="self">First polyline.</param>
        /// <param name="other">Second polyline.</param>
        /// <param name="eps">Epsilon used for the vertex comparisons.</param>
        /// <returns><see langword="true"/> if the polylines are fuzzy equal.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="self"/> or <paramref name="other"/> is null.</exception>
        public static bool FuzzyEqEps<T>(this IPlineSource<T> self, IPlineSource<T> other, T eps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(self);
            ArgumentNullException.ThrowIfNull(other);

            if (self.IsClosed != other.IsClosed || self.VertexCount != other.VertexCount) return false;
            int vc = self.VertexCount;
            for (int i = 0; i < vc; i++)
            {
                if (!self.Get(i).FuzzyEqEps(other.Get(i), eps)) return false;
            }
            return true;
        }

        /// <summary>
        /// Fuzzy comparison against another polyline using the default epsilon (<c>1e-8</c>), see
        /// <see cref="FuzzyEqEps{T}(IPlineSource{T}, IPlineSource{T}, T)"/>.
        /// </summary>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="self">First polyline.</param>
        /// <param name="other">Second polyline.</param>
        /// <returns><see langword="true"/> if the polylines are fuzzy equal.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="self"/> or <paramref name="other"/> is null.</exception>
        public static bool FuzzyEq<T>(this IPlineSource<T> self, IPlineSource<T> other)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(self);
            ArgumentNullException.ThrowIfNull(other);

            return self.FuzzyEqEps(other, Fuzzy<T>.Epsilon);
        }

        /// <summary>Computes the XY extents of the polyline.</summary>
        /// <remarks>
        /// Arc segments are accounted for exactly, so the box also covers the bulge of an arc that
        /// reaches beyond its end points. Only positions reachable along actual segments are
        /// considered; for an open polyline the closing segment does not exist and is therefore not
        /// included.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to measure.</param>
        /// <returns>
        /// The axis aligned bounding box of the polyline, or <see langword="null"/> if the polyline
        /// has no segments, that is fewer than two vertexes.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static AABB<T>? Extents<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>Returns the total path length of the polyline.</summary>
        /// <remarks>
        /// The sum of the lengths of all segments, arcs measured along their sweep. For a closed
        /// polyline the closing segment is included, so closing an open polyline increases its path
        /// length.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to measure.</param>
        /// <returns>The total path length, zero for a polyline without segments.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static T PathLength<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            T len = T.Zero;
            foreach (var (v1, v2) in pline.IterSegments())
            {
                len += PlineSeg.SegLength(v1, v2);
            }
            return len;
        }

        /// <summary>Computes the signed area enclosed by a closed polyline.</summary>
        /// <remarks>
        /// <para>
        /// The area is <em>signed</em>: it is positive when the polyline runs counter-clockwise and
        /// negative when it runs clockwise. Inverting the direction of a polyline therefore flips
        /// the sign but not the magnitude. <see cref="Orientation{T}(IPlineSource{T})"/> is built
        /// directly on this sign.
        /// </para>
        /// <para>
        /// The computation uses the shoelace formula over the vertex positions, extended for arcs:
        /// for each arc segment the area of the circular segment between chord and arc is added for
        /// a counter-clockwise arc and subtracted for a clockwise one.
        /// </para>
        /// <para>
        /// Zero is always returned for an open polyline. For a self-intersecting polyline the
        /// result is the algebraic sum of the enclosed regions weighted by winding, which is
        /// usually not the area a human would measure.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to measure.</param>
        /// <returns>
        /// The signed enclosed area, or zero if the polyline is open.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static T Area<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            if (!pline.IsClosed) return T.Zero;

            T doubleTotalArea = T.Zero;
            T two = T.CreateChecked(2);
            T four = T.CreateChecked(4);

            foreach (var (v1, v2) in pline.IterSegments())
            {
                // Grouping matters: upstream accumulates as (acc + t1) - t2, the compound
                // assignment would be acc + (t1 - t2) and rounds differently. Orientation()
                // decides on the sign of this sum.
                doubleTotalArea = doubleTotalArea + v1.X * v2.Y - v1.Y * v2.X;
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

        /// <summary>Returns the orientation of the polyline.</summary>
        /// <remarks>
        /// Determined purely from the sign of <see cref="Area{T}(IPlineSource{T})"/>: negative is
        /// clockwise, everything else, including exactly zero, is counter-clockwise. For a
        /// self-intersecting polyline the area is not a reliable indicator of direction and the
        /// result may not be useful.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to inspect.</param>
        /// <returns>
        /// <c>PlineOrientation.Open</c> for an open polyline, otherwise
        /// <c>PlineOrientation.Clockwise</c> or <c>PlineOrientation.CounterClockwise</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static PlineOrientation Orientation<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            if (!pline.IsClosed) return PlineOrientation.Open;
            return pline.Area() < T.Zero ? PlineOrientation.Clockwise : PlineOrientation.CounterClockwise;
        }

        /// <summary>
        /// Removes all consecutive vertexes that repeat the previous position.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When a vertex repeats the previous position it is dropped and its bulge is moved onto
        /// the vertex that is kept, so the shape of the path is preserved. For a closed polyline a
        /// final vertex that repeats the first position is dropped as well.
        /// </para>
        /// <para>
        /// The returned polyline does not carry the user data of the source over, matching
        /// upstream, which builds the result from a vertex iterator only.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to clean up.</param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons.</param>
        /// <returns>
        /// A new polyline with the repeat position vertexes removed, or <see langword="null"/> if
        /// there was nothing to do. <see langword="null"/> is not a failure: it means either that
        /// the polyline has fewer than two vertexes, or that no vertex was removed, and it exists
        /// so the allocation and copy can be avoided. Callers should treat it as "keep the input".
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static Polyline<T>? RemoveRepeatPos<T>(this IPlineSource<T> pline, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>
        /// Creates a fast approximate spatial index over all polyline segments.
        /// </summary>
        /// <remarks>
        /// The index key of each segment is the positional index of its starting vertex. The boxes
        /// are computed with
        /// <see cref="PlineSeg.SegFastApproxBoundingBox{T}(PlineVertex{T}, PlineVertex{T})"/>, so
        /// they are guaranteed never to be smaller than the true segment box but may be larger.
        /// Use <see cref="CreateAabbIndex{T}(IPlineSource{T})"/> when exact boxes are required.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to index.</param>
        /// <returns>
        /// A spatial index of the segment bounding boxes, empty if the polyline has fewer than two
        /// vertexes.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static StaticAABB2DIndex<T> CreateApproxAabbIndex<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>Creates a spatial index over all polyline segments using exact bounding boxes.</summary>
        /// <remarks>
        /// The index key of each segment is the positional index of its starting vertex. The boxes
        /// are the true segment boxes computed with
        /// <see cref="PlineSeg.SegBoundingBox{T}(PlineVertex{T}, PlineVertex{T})"/>, which is
        /// slower for arcs than <see cref="CreateApproxAabbIndex{T}(IPlineSource{T})"/>.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to index.</param>
        /// <returns>
        /// A spatial index of the segment bounding boxes, empty if the polyline has fewer than two
        /// vertexes.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static StaticAABB2DIndex<T> CreateAabbIndex<T>(this IPlineSource<T> pline)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>
        /// Finds the point on the polyline that is closest to the point given.
        /// </summary>
        /// <remarks>
        /// Every segment is tested and the nearest result wins; ties are broken in favour of the
        /// earlier segment. A polyline with a single vertex returns that vertex with the distance
        /// to it. The reported segment index is the index of the starting vertex of the segment the
        /// closest point lies on.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to search.</param>
        /// <param name="point">Point to find the closest polyline point for.</param>
        /// <param name="posEqualEps">Epsilon used for the fuzzy float comparisons.</param>
        /// <returns>
        /// The segment start index, the closest point and its distance, or <see langword="null"/>
        /// if the polyline is empty.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static ClosestPointResult<T>? ClosestPoint<T>(this IPlineSource<T> pline, Vector2<T> point, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>Calculates the winding number of the polyline around the point given.</summary>
        /// <remarks>
        /// <para>
        /// The winding number counts how many times, and in which direction, the polyline path
        /// turns around the point. For a closed polyline without self intersections there are only
        /// three possible results:
        /// </para>
        /// <list type="bullet">
        /// <item><description><c>1</c> — the polyline winds around the point counter-clockwise.</description></item>
        /// <item><description><c>0</c> — the point lies outside the polyline.</description></item>
        /// <item><description><c>-1</c> — the polyline winds around the point clockwise.</description></item>
        /// </list>
        /// <para>
        /// For a self-intersecting closed polyline the magnitude may exceed one: a path that loops
        /// around the point twice counter-clockwise yields <c>2</c>, and twice clockwise yields
        /// <c>-2</c>. In general the result is the number of counter-clockwise turns minus the
        /// number of clockwise turns, so it is unbounded in both directions. The sign convention is
        /// the same as for <see cref="Area{T}(IPlineSource{T})"/>: counter-clockwise is positive.
        /// </para>
        /// <para>
        /// Zero is always returned for an open polyline, and for a polyline with fewer than two
        /// vertexes.
        /// </para>
        /// <para>
        /// If the point lies exactly on the polyline the result is undefined and may be any
        /// integer. Use <see cref="ClosestPoint{T}(IPlineSource{T}, Vector2{T}, T)"/> to detect
        /// that case first by checking whether the distance to the polyline is zero.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to test against.</param>
        /// <param name="point">Point to compute the winding number for.</param>
        /// <returns>The winding number, see the remarks for its interpretation.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static int WindingNumber<T>(this IPlineSource<T> pline, Vector2<T> point)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>
        /// Returns a new polyline with all arc segments approximated by line segments.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <paramref name="errorDistance"/> is the maximum distance from any generated line segment
        /// to the arc it approximates. The line end points are placed on the arc path, so the
        /// approximation is inscribed in the arc. All arcs of one segment get an equal share of the
        /// sweep angle.
        /// </para>
        /// <para>
        /// An arc whose radius is smaller than <paramref name="errorDistance"/> is collapsed to its
        /// start point. A segment whose bulge is fuzzy zero is copied unchanged, which means a
        /// bulge that is fuzzy but not exactly zero survives into the result; this matches
        /// upstream.
        /// </para>
        /// <para>
        /// The closed flag is carried over, and for an open polyline the final vertex is appended
        /// so the path is not shortened. User data is not carried over.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to convert.</param>
        /// <param name="errorDistance">
        /// Maximum allowed deviation of a line segment from the arc; its absolute value is used.
        /// </param>
        /// <returns>
        /// A new polyline containing only line segments, empty if the input has no vertexes.
        /// </returns>
        /// <exception cref="OverflowException">
        /// The computed segment count is not representable as an <see cref="int"/>, which happens
        /// for a degenerate <paramref name="errorDistance"/>. Upstream returns <c>None</c> in this
        /// case instead of signalling an error.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static Polyline<T> ArcsToApproxLines<T>(this IPlineSource<T> pline, T errorDistance)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

            var result = new Polyline<T>(pline.IsClosed);
            if (pline.VertexCount == 0) return result;

            T absError = T.Abs(errorDistance);

            foreach (var (v1, v2) in pline.IterSegments())
            {
                if (v1.BulgeIsZero())
                {
                    // Upstream uses result.add_vertex(v1), which keeps a bulge that is fuzzy
                    // zero but not exactly zero.
                    result.AddVertex(v1);
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
                // abs goes outside acos, matching upstream (one - abs_error/arc_radius).acos().abs()
                T segSubAngle = two * T.Abs(T.Acos(T.One - absError / arcRadius));
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

        /// <summary>
        /// Finds the segment index and the point on the polyline at the given distance along the
        /// path.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The path is walked from the first vertex. A non-positive
        /// <paramref name="targetPathLength"/> returns the first vertex position at segment index
        /// zero. Within the segment that contains the target length the point is interpolated
        /// linearly for a line segment and along the sweep angle for an arc segment.
        /// </para>
        /// <para>
        /// Where upstream returns a <c>Result</c> whose error carries the total path length, this
        /// port returns a tuple with a success flag. <c>AccLength</c> is the path length
        /// accumulated up to the start of the reported segment on success, and the total path
        /// length of the polyline on failure.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to walk.</param>
        /// <param name="targetPathLength">Distance along the path to locate.</param>
        /// <returns>
        /// <c>Success</c> is false when <paramref name="targetPathLength"/> exceeds the total path
        /// length; in that case <c>SegIndex</c> and <c>Point</c> are default values and
        /// <c>AccLength</c> is the total path length. On success <c>SegIndex</c> is the index of
        /// the segment the point lies on, <c>Point</c> is the located position, and
        /// <c>AccLength</c> is the path length up to the start of that segment.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="targetPathLength"/> is non-positive and the polyline is empty, so the
        /// first vertex cannot be read.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static (bool Success, int SegIndex, Vector2<T> Point, T AccLength) FindPointAtPathLength<T>(this IPlineSource<T> pline, T targetPathLength)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>
        /// Appends a vertex unless its position is fuzzy equal to the last vertex already present.
        /// </summary>
        /// <remarks>
        /// If the positions coincide the vertex is not appended; instead the bulge of the existing
        /// last vertex is replaced by the bulge of <paramref name="vertex"/>, which preserves the
        /// curvature of the segment that would have started at the duplicate position.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="self">Polyline to append to.</param>
        /// <param name="vertex">Vertex to append or merge.</param>
        /// <param name="posEqualEps">Epsilon used for the positional comparison.</param>
        /// <exception cref="ArgumentNullException"><paramref name="self"/> is null.</exception>
        public static void AddOrReplaceVertex<T>(this IPlineSourceMut<T> self, PlineVertex<T> vertex, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(self);

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

        /// <summary>
        /// Copies all vertexes from another polyline to the end of this one, dropping consecutive
        /// repeat positions in the process.
        /// </summary>
        /// <remarks>
        /// Each vertex is appended with
        /// <see cref="AddOrReplaceVertex{T}(IPlineSourceMut{T}, PlineVertex{T}, T)"/>, so a vertex
        /// that repeats the current last position only updates its bulge. The closed flag and the
        /// user data of <paramref name="other"/> are not copied.
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="self">Polyline to append to.</param>
        /// <param name="other">Polyline whose vertexes are copied.</param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons.</param>
        /// <exception cref="ArgumentNullException"><paramref name="self"/> or <paramref name="other"/> is null.</exception>
        public static void ExtendRemoveRepeat<T>(this IPlineSourceMut<T> self, IPlineSource<T> other, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(self);
            ArgumentNullException.ThrowIfNull(other);

            int otherCount = other.VertexCount;
            for (int i = 0; i < otherCount; i++)
            {
                self.AddOrReplaceVertex(other.Get(i), posEqualEps);
            }
        }

        /// <summary>
        /// Creates a new polyline as a copy of the one given, removing repeat position vertexes in
        /// the process.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="CreateFrom{O, T}(IPlineSource{T})"/>, consecutive vertexes sharing a
        /// position are merged, and for a closed polyline a final vertex repeating the first
        /// position is dropped. The closed flag and the user data values are carried over.
        /// </remarks>
        /// <typeparam name="O">
        /// Concrete mutable polyline type to create; must have a parameterless constructor.
        /// </typeparam>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to copy.</param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons.</param>
        /// <returns>The newly created polyline.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static O CreateFromRemoveRepeat<O, T>(IPlineSource<T> pline, T posEqualEps)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>Creates a new polyline as a verbatim copy of the one given.</summary>
        /// <remarks>
        /// Vertexes, the closed flag and the user data values are all carried over.
        /// </remarks>
        /// <typeparam name="O">
        /// Concrete mutable polyline type to create; must have a parameterless constructor.
        /// </typeparam>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="pline">Polyline to copy.</param>
        /// <returns>The newly created polyline.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline"/> is null.</exception>
        public static O CreateFrom<O, T>(IPlineSource<T> pline)
            where O : IPlineSourceMut<T>, new()
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline);

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

        /// <summary>Removes all redundant vertexes from the polyline.</summary>
        /// <remarks>
        /// <para>
        /// A vertex is redundant when removing it does not change the path. That is the case for
        /// vertexes sitting on top of each other, for a vertex in the middle of a straight run of
        /// collinear line segments travelling in the same direction, and for a vertex between two
        /// arc segments that share a center and radius and whose combined sweep stays below
        /// <c>PI</c>. In the arc case the two segments are merged by rewriting the bulge of the
        /// preceding vertex.
        /// </para>
        /// <para>
        /// The sweep limit is what keeps the result representable: a polyline arc segment can never
        /// sweep more than <c>PI</c>, so a circle described by several arcs collapses at most down
        /// to two vertexes.
        /// </para>
        /// <para>
        /// For a closed polyline the wrap-around at the start vertex is considered too, so the
        /// first vertex may be removed as well.
        /// </para>
        /// <para>
        /// The returned polyline deliberately does not carry the user data of the source over. This
        /// matches upstream, which builds the result from a vertex iterator only and never calls
        /// the user data setter.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="self">Polyline to clean up.</param>
        /// <param name="posEqualEps">
        /// Epsilon used for the positional, collinearity and arc comparisons.
        /// </param>
        /// <returns>
        /// A new polyline with the redundant vertexes removed, or <see langword="null"/> if there
        /// was nothing to do. <see langword="null"/> is not a failure: it means either that the
        /// polyline has fewer than two vertexes, or that no vertex was found to be redundant, and
        /// it exists so the allocation and copy can be avoided. Callers should treat it as "keep
        /// the input".
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="self"/> is null.</exception>
        public static Polyline<T>? RemoveRedundant<T>(this IPlineSource<T> self, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(self);

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

            return result;
        }

        /// <summary>
        /// Rotates the vertexes of a closed polyline so that the first vertex is positioned at the
        /// point given.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This does not change the shape of the curve, only where the vertex sequence begins. If
        /// <paramref name="point"/> lies on top of the vertex at <paramref name="startIndex"/>, or
        /// on top of the following one, the vertexes are simply rotated and the vertex count stays
        /// the same. Otherwise the segment is split at <paramref name="point"/> and the result has
        /// one vertex more than the source.
        /// </para>
        /// <para>
        /// The result is always closed.
        /// </para>
        /// <para>
        /// The returned polyline deliberately does not carry the user data of the source over. This
        /// matches upstream, which builds the result from a vertex iterator only and never calls
        /// the user data setter.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="self">Polyline to rotate.</param>
        /// <param name="startIndex">
        /// Index of the segment <paramref name="point"/> lies on before rotation.
        /// </param>
        /// <param name="point">New start position.</param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons.</param>
        /// <returns>
        /// A new closed polyline starting at <paramref name="point"/>, or <see langword="null"/>
        /// for invalid input. <see langword="null"/> is not a failure of the rotation itself; it
        /// means the request was not applicable, namely that the polyline is open, that it has
        /// fewer than two vertexes, or that <paramref name="startIndex"/> is out of bounds.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="self"/> is null.</exception>
        public static Polyline<T>? RotateStart<T>(this IPlineSource<T> self, int startIndex, Vector2<T> point, T posEqualEps)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(self);

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

            return result;
        }

        /// <summary>Inverts the direction of the polyline in place.</summary>
        /// <remarks>
        /// <para>
        /// The vertex order is reversed, then all bulges are shifted by one position and negated,
        /// because a bulge belongs to the segment starting at its vertex and that segment now runs
        /// the other way. Concretely, after reversing, the bulge at index 0 becomes the negated
        /// bulge that was at index 1. For a closed polyline the bulge of the closing segment is
        /// handled as well, so the direction flips from clockwise to counter-clockwise or vice
        /// versa and <see cref="Area{T}(IPlineSource{T})"/> changes sign.
        /// </para>
        /// <para>
        /// A polyline with fewer than two vertexes is left untouched.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
        /// <param name="self">Polyline to invert.</param>
        /// <exception cref="ArgumentNullException"><paramref name="self"/> is null.</exception>
        public static void InvertDirection<T>(this IPlineSourceMut<T> self)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(self);

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
