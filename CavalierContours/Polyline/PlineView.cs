using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// A partial selection, or subpart, of a source polyline that does not copy the source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A view pairs a <see cref="PlineViewData{T}"/> with the source polyline it indexes into and
    /// implements <see cref="IPlineSource{T}"/>, so all read-only polyline operations can be run on
    /// the selection directly. Vertexes are synthesized on demand from the source: only the first
    /// and last vertex of the selection are modified copies, everything in between is passed
    /// through unchanged.
    /// </para>
    /// <para>
    /// A view is always an open polyline, even when the source is closed. If the selection covers
    /// the whole closed source it still follows the same geometric path, it merely repeats the
    /// start position as its end position instead of closing.
    /// </para>
    /// <para>
    /// The view holds a live reference to the source. Mutating the source while a view exists
    /// invalidates the view.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
    public readonly struct PlineView<T> : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>The source polyline this view reads its vertex data from.</summary>
        public readonly IPlineSource<T> Source;

        /// <summary>The view data used for indexing into <see cref="Source"/>.</summary>
        public readonly PlineViewData<T> Data;

        /// <summary>Creates a view binding view data to a source polyline.</summary>
        /// <remarks>
        /// No validation is performed here; use
        /// <see cref="PlineViewData{T}.View(IPlineSource{T})"/> to get a debug-time validity check.
        /// </remarks>
        /// <param name="source">Source polyline to read vertex data from.</param>
        /// <param name="data">View data describing the selection over <paramref name="source"/>.</param>
        public PlineView(IPlineSource<T> source, PlineViewData<T> data)
        {
            Source = source;
            Data = data;
        }

        /// <summary>
        /// Number of vertexes in the view, which is <c>Data.EndIndexOffset + 2</c> and generally
        /// differs from the source's vertex count.
        /// </summary>
        public int VertexCount => Data.VertexCount;

        /// <summary>
        /// Always <see langword="false"/>: a view is treated as an open polyline even when the
        /// source is closed.
        /// </summary>
        public bool IsClosed => false;

        /// <summary>
        /// Number of user data values, passed through unchanged from <see cref="Source"/>.
        /// </summary>
        public int UserDataCount => Source.UserDataCount;

        /// <summary>
        /// User data values, passed through unchanged from <see cref="Source"/>. The values are not
        /// filtered by the selection.
        /// </summary>
        public IReadOnlyList<ulong> UserDataValues => Source.UserDataValues;

        /// <summary>Gets the vertex of the view at the given index position.</summary>
        /// <remarks>
        /// The vertex is computed on demand from <see cref="Data"/> and <see cref="Source"/>, see
        /// <see cref="PlineViewData{T}.GetVertex(IPlineSource{T}, int)"/>.
        /// </remarks>
        /// <param name="index">Zero based vertex index within the view.</param>
        /// <returns>The vertex of the view at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is negative or not less than <see cref="VertexCount"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> Get(int index)
        {
            var v = Data.GetVertex(Source, index);
            if (v == null)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of view range");
            }
            return v.Value;
        }
    }

    /// <summary>
    /// The minimum data required to describe a partial selection over a source polyline, detached
    /// from that source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="PlineView{T}"/> this structure holds no reference to a polyline. It only
    /// stores indexes and the two trimmed endpoints, and it therefore has to be re-attached to a
    /// source before it can be read: call <see cref="View(IPlineSource{T})"/> to obtain an active
    /// <see cref="PlineView{T}"/>. The same view data can be applied to any source with a matching
    /// vertex layout, and it can be stored and passed around cheaply because it is a small value
    /// type.
    /// </para>
    /// <para>
    /// Together the fields describe a contiguous walk over the source, which may wrap past the end
    /// of a closed source, and which may optionally be traversed backwards:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="StartIndex"/> is the source segment the selection starts on.
    /// </description></item>
    /// <item><description>
    /// <see cref="EndIndexOffset"/> is the wrapping offset from <see cref="StartIndex"/> to the
    /// last source segment of the selection, so the number of view vertexes is
    /// <c>EndIndexOffset + 2</c>.
    /// </description></item>
    /// <item><description>
    /// <see cref="UpdatedStart"/> is the first vertex of the selection: a point somewhere along the
    /// start segment, with its bulge trimmed to the remaining part of that segment.
    /// </description></item>
    /// <item><description>
    /// <see cref="UpdatedEndBulge"/> is the bulge to be used on the last source segment, trimmed so
    /// that the segment ends exactly at <see cref="EndPoint"/>.
    /// </description></item>
    /// <item><description>
    /// <see cref="EndPoint"/> is the final position of the selection.
    /// </description></item>
    /// <item><description>
    /// <see cref="InvertedDirection"/> reverses the order in which vertexes are produced. It only
    /// affects vertex construction; all other fields keep their forward-oriented meaning.
    /// </description></item>
    /// </list>
    /// <para>
    /// A view data value describes exactly the same geometry as the corresponding slice of the
    /// source polyline; the trimming is expressed purely through <see cref="UpdatedStart"/>,
    /// <see cref="UpdatedEndBulge"/> and <see cref="EndPoint"/> rather than by copying vertexes.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Floating point type used for the vertexes.</typeparam>
    public readonly struct PlineViewData<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>Index of the source segment the selection starts on.</summary>
        public readonly int StartIndex;

        /// <summary>
        /// Wrapping offset from <see cref="StartIndex"/> to the last source segment of the
        /// selection. May be as large as the source vertex count, which selects the entire closed
        /// source starting from a point in the middle of a segment.
        /// </summary>
        public readonly int EndIndexOffset;

        /// <summary>
        /// First vertex of the selection: a point on the <see cref="StartIndex"/> segment with both
        /// its position and its bulge updated for the trim.
        /// </summary>
        public readonly PlineVertex<T> UpdatedStart;

        /// <summary>
        /// Bulge to be used on the last segment of the selection, trimmed so that the segment ends
        /// at <see cref="EndPoint"/>. When <see cref="EndIndexOffset"/> is zero the whole selection
        /// lies on a single segment and this value equals <c>UpdatedStart.Bulge</c>.
        /// </summary>
        public readonly T UpdatedEndBulge;

        /// <summary>Final end position of the selection.</summary>
        public readonly Vector2<T> EndPoint;

        /// <summary>
        /// Whether the view is traversed in reverse. When set, vertex index 0 of the view is
        /// <see cref="EndPoint"/> and the last vertex is <see cref="UpdatedStart"/>, with all bulge
        /// signs flipped and shifted by one position. All fields keep their forward-oriented
        /// meaning regardless of this flag.
        /// </summary>
        public readonly bool InvertedDirection;

        /// <summary>Constructs view data from its individual fields.</summary>
        /// <remarks>
        /// This performs no validation. Prefer the factory methods
        /// <see cref="FromSlicePoints(IPlineSource{T}, Vector2{T}, int, Vector2{T}, int, T)"/>,
        /// <see cref="FromNewStart(IPlineSource{T}, Vector2{T}, int, T)"/>,
        /// <see cref="FromEntirePline(IPlineSource{T})"/>,
        /// <see cref="Create(IPlineSource{T}, int, Vector2{T}, int, PlineVertex{T}, int, T)"/> and
        /// <see cref="CreateOnSingleSegment(IPlineSource{T}, int, PlineVertex{T}, Vector2{T}, T)"/>,
        /// which derive consistent values from a source polyline.
        /// </remarks>
        /// <param name="startIndex">Index of the source segment the selection starts on.</param>
        /// <param name="endIndexOffset">
        /// Wrapping offset to the last source segment, see <see cref="EndIndexOffset"/>.
        /// </param>
        /// <param name="updatedStart">First vertex of the selection, see <see cref="UpdatedStart"/>.</param>
        /// <param name="updatedEndBulge">
        /// Trimmed bulge for the last segment, see <see cref="UpdatedEndBulge"/>.
        /// </param>
        /// <param name="endPoint">Final end position of the selection.</param>
        /// <param name="invertedDirection">
        /// Whether the view is traversed in reverse, see <see cref="InvertedDirection"/>.
        /// </param>
        public PlineViewData(int startIndex, int endIndexOffset, PlineVertex<T> updatedStart, T updatedEndBulge, Vector2<T> endPoint, bool invertedDirection)
        {
            StartIndex = startIndex;
            EndIndexOffset = endIndexOffset;
            UpdatedStart = updatedStart;
            UpdatedEndBulge = updatedEndBulge;
            EndPoint = endPoint;
            InvertedDirection = invertedDirection;
        }

        /// <summary>
        /// Number of vertexes the view produces, which is <see cref="EndIndexOffset"/> plus two:
        /// the updated start, all pass-through source vertexes in between, and the end point.
        /// </summary>
        public int VertexCount => EndIndexOffset + 2;

        /// <summary>
        /// Binds this view data to a source polyline, producing an active
        /// <see cref="PlineView{T}"/> that can be read and operated on.
        /// </summary>
        /// <remarks>
        /// In debug builds the view data is checked against the source with
        /// <see cref="ValidateForSource(IPlineSource{T})"/>. In release builds no check is
        /// performed and binding to an incompatible source silently yields nonsense vertexes.
        /// </remarks>
        /// <param name="source">Source polyline the view will read vertex data from.</param>
        /// <returns>A view over <paramref name="source"/> described by this view data.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineView<T> View(IPlineSource<T> source)
        {
            Debug.Assert(ValidateForSource(source) == ViewDataValidation.IsValid);
            return new PlineView<T>(source, this);
        }

        /// <summary>
        /// Gets the vertex of the view at <paramref name="index"/>, computed from this view data
        /// and the source polyline given.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In forward direction index 0 is <see cref="UpdatedStart"/>, indexes strictly between 0
        /// and <see cref="EndIndexOffset"/> are source vertexes passed through unchanged, index
        /// <see cref="EndIndexOffset"/> is the source vertex at the end of the walk with its bulge
        /// replaced by <see cref="UpdatedEndBulge"/>, and the last index is
        /// <see cref="EndPoint"/> with a zero bulge.
        /// </para>
        /// <para>
        /// With <see cref="InvertedDirection"/> set the same vertexes are produced in reverse: each
        /// vertex takes the negated bulge of its predecessor in the source, index 0 is
        /// <see cref="EndPoint"/> with the negated <see cref="UpdatedEndBulge"/>, and the last
        /// index is <see cref="UpdatedStart"/> with a zero bulge.
        /// </para>
        /// </remarks>
        /// <param name="source">Source polyline to read vertex data from.</param>
        /// <param name="index">Zero based vertex index within the view.</param>
        /// <returns>
        /// The vertex at <paramref name="index"/>, or <see langword="null"/> if
        /// <paramref name="index"/> is negative or not less than <see cref="VertexCount"/>.
        /// </returns>
        public PlineVertex<T>? GetVertex(IPlineSource<T> source, int index)
        {
            // Mirrors Rust's Option-returning get_vertex, whose index is a usize. The unsigned
            // compare catches negatives too, which would otherwise index the source array.
            if ((uint)index >= (uint)VertexCount) return null;

            if (InvertedDirection)
            {
                if (index == 0)
                {
                    return PlineVertex<T>.FromVector2(EndPoint, -UpdatedEndBulge);
                }

                if (index < EndIndexOffset)
                {
                    int bulgeI = source.FwdWrappingIndex(StartIndex, EndIndexOffset - index);
                    int i = source.NextWrappingIndex(bulgeI);
                    return source.Get(i).WithBulge(-source.Get(bulgeI).Bulge);
                }

                if (index == EndIndexOffset)
                {
                    int i = source.FwdWrappingIndex(StartIndex, EndIndexOffset - index + 1);
                    return source.Get(i).WithBulge(-UpdatedStart.Bulge);
                }

                if (index == EndIndexOffset + 1)
                {
                    return UpdatedStart.WithBulge(T.Zero);
                }
            }
            else
            {
                if (index == 0)
                {
                    return UpdatedStart;
                }

                if (index < EndIndexOffset)
                {
                    int i = source.FwdWrappingIndex(StartIndex, index);
                    return source.Get(i);
                }

                if (index == EndIndexOffset)
                {
                    int i = source.FwdWrappingIndex(StartIndex, EndIndexOffset);
                    return source.Get(i).WithBulge(UpdatedEndBulge);
                }

                if (index == EndIndexOffset + 1)
                {
                    return PlineVertex<T>.FromVector2(EndPoint, T.Zero);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates view data for a selection that lies entirely on a single source segment.
        /// </summary>
        /// <remarks>
        /// <see cref="EndIndexOffset"/> is set to zero and <see cref="UpdatedEndBulge"/> is taken
        /// from <paramref name="updatedStart"/>, since start and end lie on the same segment.
        /// </remarks>
        /// <param name="source">
        /// Source polyline the selection refers to. Not read by this method; it is accepted so the
        /// signature matches the other factory methods, and it is used upstream for a debug
        /// validation that this port does not perform.
        /// </param>
        /// <param name="startIndex">Index of the source segment the selection lies on.</param>
        /// <param name="updatedStart">
        /// First vertex of the selection, already trimmed to the start point.
        /// </param>
        /// <param name="endIntersect">End position of the selection on the same segment.</param>
        /// <param name="posEqualEps">Epsilon used for the positional comparison.</param>
        /// <returns>
        /// The view data, or <see langword="null"/> if the selection collapsed, that is if
        /// <paramref name="updatedStart"/> is fuzzy equal to <paramref name="endIntersect"/>.
        /// </returns>
        public static PlineViewData<T>? CreateOnSingleSegment(
            IPlineSource<T> source,
            int startIndex,
            PlineVertex<T> updatedStart,
            Vector2<T> endIntersect,
            T posEqualEps)
        {
            if (updatedStart.Pos().FuzzyEqEps(endIntersect, posEqualEps))
            {
                return null;
            }
            return new PlineViewData<T>(startIndex, 0, updatedStart, updatedStart.Bulge, endIntersect, false);
        }

        /// <summary>
        /// Creates view data for a selection that spans at least one full source segment boundary.
        /// </summary>
        /// <remarks>
        /// If <paramref name="endIntersect"/> lies on top of the vertex at
        /// <paramref name="intersectIndex"/> the walk is shortened by one segment and the end bulge
        /// is taken from the preceding source segment, otherwise the segment at
        /// <paramref name="intersectIndex"/> is split at the end point and its trimmed bulge is
        /// used.
        /// </remarks>
        /// <param name="source">Source polyline the selection refers to.</param>
        /// <param name="startIndex">Index of the source segment the selection starts on.</param>
        /// <param name="endIntersect">End position of the selection.</param>
        /// <param name="intersectIndex">
        /// Index of the source segment <paramref name="endIntersect"/> lies on.
        /// </param>
        /// <param name="updatedStart">
        /// First vertex of the selection, already trimmed to the start point.
        /// </param>
        /// <param name="traverseCount">
        /// Number of source segment boundaries crossed on the way from
        /// <paramref name="startIndex"/> to <paramref name="intersectIndex"/>. Must be greater
        /// than zero.
        /// </param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons.</param>
        /// <returns>The view data describing the selection.</returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="traverseCount"/> is zero. This mirrors the upstream <c>assert!</c>,
        /// which is active in release builds as well; use
        /// <see cref="CreateOnSingleSegment(IPlineSource{T}, int, PlineVertex{T}, Vector2{T}, T)"/>
        /// when the selection lies on a single segment.
        /// </exception>
        public static PlineViewData<T> Create(
            IPlineSource<T> source,
            int startIndex,
            Vector2<T> endIntersect,
            int intersectIndex,
            PlineVertex<T> updatedStart,
            int traverseCount,
            T posEqualEps)
        {
            // Upstream uses assert!, which is active in release builds. With Debug.Assert a
            // release build would silently produce endIndexOffset = -1 and a one vertex view.
            if (traverseCount == 0)
            {
                throw new InvalidOperationException(
                    "traverseCount must be greater than 0, use CreateOnSingleSegment if the view is all on one segment");
            }

            PlineVertex<T> currentVertex = source.Get(intersectIndex);
            int endIndexOffset;
            T updatedEndBulge;

            if (endIntersect.FuzzyEqEps(currentVertex.Pos(), posEqualEps))
            {
                endIndexOffset = traverseCount - 1;
                updatedEndBulge = endIndexOffset != 0
                    ? source.Get(source.PrevWrappingIndex(intersectIndex)).Bulge
                    : updatedStart.Bulge;
            }
            else
            {
                int nextIndex = source.NextWrappingIndex(intersectIndex);
                var split = PlineSeg.SegSplitAtPoint(currentVertex, source.Get(nextIndex), endIntersect, posEqualEps);
                endIndexOffset = traverseCount;
                updatedEndBulge = split.UpdatedStart.Bulge;
            }

            return new PlineViewData<T>(startIndex, endIndexOffset, updatedStart, updatedEndBulge, endIntersect, false);
        }

        /// <summary>
        /// Creates view data that selects an entire polyline.
        /// </summary>
        /// <remarks>
        /// The resulting view is always an open polyline even when <paramref name="source"/> is
        /// closed, but it follows the same geometric path: for a closed source the walk covers all
        /// segments and the end point is the start position again.
        /// </remarks>
        /// <param name="source">Source polyline to select in full.</param>
        /// <returns>View data covering all of <paramref name="source"/>.</returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="source"/> has fewer than two vertexes and therefore no segments. This
        /// mirrors the upstream <c>assert!</c>, which is active in release builds as well.
        /// </exception>
        public static PlineViewData<T> FromEntirePline(IPlineSource<T> source)
        {
            int vc = source.VertexCount;
            // Upstream uses assert!, active in release builds.
            if (vc < 2)
            {
                throw new InvalidOperationException("source must have at least 2 vertexes to form view data");
            }

            if (source.IsClosed)
            {
                return new PlineViewData<T>(0, vc - 1, source.Get(0), source.Get(vc - 1).Bulge, source.Get(0).Pos(), false);
            }
            else
            {
                return new PlineViewData<T>(0, vc - 2, source.Get(0), source.Get(vc - 2).Bulge, source.Get(vc - 1).Pos(), false);
            }
        }

        /// <summary>
        /// Creates view data that changes the start point of a polyline.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For an open source the polyline is trimmed away up to <paramref name="startPoint"/>: the
        /// call is delegated to
        /// <see cref="FromSlicePoints(IPlineSource{T}, Vector2{T}, int, Vector2{T}, int, T)"/> with
        /// the last vertex as end point.
        /// </para>
        /// <para>
        /// For a closed source the entire path is retained and only the start point moves; the walk
        /// wraps all the way around back to <paramref name="startPoint"/>.
        /// </para>
        /// </remarks>
        /// <param name="source">Source polyline the new start point is placed on.</param>
        /// <param name="startPoint">New start position.</param>
        /// <param name="startIndex">
        /// Index of the source segment <paramref name="startPoint"/> lies on. If the point sits on
        /// the end vertex of that segment the index is advanced by one.
        /// </param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons.</param>
        /// <returns>
        /// The view data, or <see langword="null"/> if there is nothing to select. That is the case
        /// for an empty source, and for an open source whose selection collapses, in particular
        /// when <paramref name="startPoint"/> is the final vertex position.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="source"/> is closed and has fewer than two vertexes. This mirrors the
        /// upstream <c>assert!</c>, which is active in release builds as well.
        /// </exception>
        public static PlineViewData<T>? FromNewStart(
            IPlineSource<T> source,
            Vector2<T> startPoint,
            int startIndex,
            T posEqualEps)
        {
            if (!source.IsClosed)
            {
                // Upstream is `source.last()?.pos()`, so an empty source yields None rather
                // than panicking.
                var last = source.Last();
                if (last is null)
                {
                    return null;
                }

                return FromSlicePoints(source, startPoint, startIndex, last.Value.Pos(), source.VertexCount - 1, posEqualEps);
            }

            int vc = source.VertexCount;
            // Upstream uses assert!, active in release builds.
            if (vc < 2)
            {
                throw new InvalidOperationException("source must have at least 2 vertexes to form view data");
            }

            int nextIdx = source.NextWrappingIndex(startIndex);
            if (source.Get(nextIdx).Pos().FuzzyEqEps(startPoint, posEqualEps))
            {
                startIndex = nextIdx;
            }

            PlineVertex<T> startV1 = source.Get(startIndex);
            PlineVertex<T> startV2 = source.Get(source.NextWrappingIndex(startIndex));
            var split = PlineSeg.SegSplitAtPoint(startV1, startV2, startPoint, posEqualEps);

            int endIndexOffset = startV1.Pos().FuzzyEqEps(startPoint, posEqualEps) ? vc - 1 : vc;
            T updatedEndBulge = startV1.Pos().FuzzyEqEps(startPoint, posEqualEps)
                ? source.Get(source.PrevWrappingIndex(startIndex)).Bulge
                : split.UpdatedStart.Bulge;

            return new PlineViewData<T>(startIndex, endIndexOffset, split.SplitVertex, updatedEndBulge, startPoint, false);
        }

        /// <summary>
        /// Creates view data for the contiguous selection between two points on a source polyline,
        /// trimming away everything before the start point and after the end point.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the general entry point for building a slice. It resolves how far the walk has
        /// to travel, splits the start segment at <paramref name="startPoint"/> and then delegates
        /// to
        /// <see cref="CreateOnSingleSegment(IPlineSource{T}, int, PlineVertex{T}, Vector2{T}, T)"/>
        /// or
        /// <see cref="Create(IPlineSource{T}, int, Vector2{T}, int, PlineVertex{T}, int, T)"/>.
        /// </para>
        /// <para>
        /// For a closed source the walk may wrap past the end of the vertex list. The ambiguous
        /// case where both points lie on the same segment is resolved by comparing their distances
        /// to the segment start: if the start point is nearer the selection stays on that one
        /// segment, otherwise it wraps all the way around the polyline.
        /// </para>
        /// <para>
        /// For an open source <paramref name="startIndex"/> is expected to be less than or equal to
        /// <paramref name="endIndex"/>; this is only asserted in debug builds.
        /// </para>
        /// </remarks>
        /// <param name="source">Source polyline to slice.</param>
        /// <param name="startPoint">Start position of the slice.</param>
        /// <param name="startIndex">
        /// Index of the source segment <paramref name="startPoint"/> lies on.
        /// </param>
        /// <param name="endPoint">End position of the slice.</param>
        /// <param name="endIndex">
        /// Index of the source segment <paramref name="endPoint"/> lies on.
        /// </param>
        /// <param name="posEqualEps">Epsilon used for the positional comparisons.</param>
        /// <returns>
        /// The view data, or <see langword="null"/> when the slice collapses to a point and there
        /// is therefore nothing to select. This happens when start and end coincide on a single
        /// segment, and when the slice degenerates across a near-coincident vertex, which would
        /// otherwise yield a two vertex view of zero path length.
        /// </returns>
        public static PlineViewData<T>? FromSlicePoints(
            IPlineSource<T> source,
            Vector2<T> startPoint,
            int startIndex,
            Vector2<T> endPoint,
            int endIndex,
            T posEqualEps)
        {
            Debug.Assert(startIndex <= endIndex || source.IsClosed, "startIndex must be <= endIndex if open");

            int nextIdx = source.NextWrappingIndex(startIndex);
            bool startPointAtSegEnd = false;
            if (source.IsClosed || startIndex < endIndex)
            {
                if (source.Get(nextIdx).Pos().FuzzyEqEps(startPoint, posEqualEps))
                {
                    startIndex = nextIdx;
                    startPointAtSegEnd = true;
                }
            }

            int traverseCount;
            int indexDist = source.FwdWrappingDist(startIndex, endIndex);
            if (indexDist == 0 && source.IsClosed && !startPoint.FuzzyEqEps(endPoint, posEqualEps))
            {
                Vector2<T> segStart = source.Get(startIndex).Pos();
                T dist1 = BaseMath.DistSquared(segStart, startPoint);
                T dist2 = BaseMath.DistSquared(segStart, endPoint);
                traverseCount = dist1 < dist2 ? 0 : source.VertexCount;
            }
            else
            {
                traverseCount = indexDist;
            }

            PlineVertex<T> startV1 = source.Get(startIndex);
            PlineVertex<T> startV2 = source.Get(source.NextWrappingIndex(startIndex));
            PlineVertex<T> updatedStart;

            if (startPointAtSegEnd)
            {
                if (traverseCount == 0)
                {
                    var split = PlineSeg.SegSplitAtPoint(startV1, startV2, endPoint, posEqualEps);
                    updatedStart = split.UpdatedStart;
                }
                else
                {
                    updatedStart = startV1;
                }
            }
            else
            {
                var startSplit = PlineSeg.SegSplitAtPoint(startV1, startV2, startPoint, posEqualEps);
                var updatedForStart = startSplit.SplitVertex;
                if (traverseCount == 0)
                {
                    var split = PlineSeg.SegSplitAtPoint(updatedForStart, startV2, endPoint, posEqualEps);
                    updatedStart = split.UpdatedStart;
                }
                else
                {
                    updatedStart = updatedForStart;
                }
            }

            if (traverseCount == 0)
            {
                return CreateOnSingleSegment(source, startIndex, updatedStart, endPoint, posEqualEps);
            }
            else if (traverseCount == 1
                && endPoint.FuzzyEqEps(source.Get(endIndex).Pos(), posEqualEps)
                && updatedStart.Pos().FuzzyEqEps(endPoint, posEqualEps))
            {
                // The slice collapsed onto a single point across a near-coincident vertex.
                // Without this the result is a two vertex view with zero path length, which then
                // travels into the boolean and offset stitching stages.
                return null;
            }
            else
            {
                return Create(source, startIndex, endPoint, endIndex, updatedStart, traverseCount, posEqualEps);
            }
        }

        /// <summary>
        /// Checks that this view data's properties are valid for the source polyline given.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Intended for debugging and assertions. The checks are, in order: the source has at least
        /// one segment; <see cref="EndIndexOffset"/> is within range; <see cref="UpdatedStart"/>
        /// lies on the <see cref="StartIndex"/> segment; <see cref="EndPoint"/> lies on the segment
        /// reached by the walk; <see cref="EndPoint"/> does not sit exactly on that segment's start
        /// vertex; and, for a single segment selection, <see cref="UpdatedEndBulge"/> matches
        /// <c>UpdatedStart.Bulge</c>.
        /// </para>
        /// <para>
        /// Fixed epsilons are used: <c>1e-5</c> for the bulge and coincidence tests and <c>1e-3</c>
        /// for the point-on-segment tests.
        /// </para>
        /// </remarks>
        /// <param name="source">Source polyline to validate this view data against.</param>
        /// <returns>
        /// The first violated condition, or <c>ViewDataValidation.IsValid</c> if all checks pass.
        /// </returns>
        public ViewDataValidation ValidateForSource(IPlineSource<T> source)
        {
            if (source.VertexCount < 2) return ViewDataValidation.SourceHasNoSegments;
            if (EndIndexOffset > source.VertexCount) return ViewDataValidation.OffsetOutOfRange;

            T validationEps = T.CreateChecked(1e-5);
            T onSegEps = T.CreateChecked(1e-3);

            bool PointIsOnSegment(int segIdx, Vector2<T> pt)
            {
                var sv1 = source.Get(segIdx);
                var sv2 = source.Get(source.NextWrappingIndex(segIdx));
                if (pt.FuzzyEqEps(sv1.Pos(), onSegEps) || pt.FuzzyEqEps(sv2.Pos(), onSegEps)) return true;
                var closest = PlineSeg.SegClosestPoint(sv1, sv2, pt, validationEps);
                return closest.FuzzyEqEps(pt, onSegEps);
            }

            if (!PointIsOnSegment(StartIndex, UpdatedStart.Pos()))
            {
                return ViewDataValidation.UpdatedStartNotOnSegment;
            }

            int endIdx = source.FwdWrappingIndex(StartIndex, EndIndexOffset);
            if (!PointIsOnSegment(endIdx, EndPoint))
            {
                return ViewDataValidation.EndPointNotOnSegment;
            }

            if (EndPoint.FuzzyEqEps(source.Get(endIdx).Pos(), validationEps))
            {
                return ViewDataValidation.EndPointOnFinalOffsetVertex;
            }

            if (EndIndexOffset == 0)
            {
                if (!UpdatedEndBulge.FuzzyEq(UpdatedStart.Bulge, validationEps))
                {
                    return ViewDataValidation.UpdatedBulgeDoesNotMatch;
                }
            }

            return ViewDataValidation.IsValid;
        }
    }
}
