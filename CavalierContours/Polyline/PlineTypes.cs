using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// Represents the orientation of a polyline.
    /// </summary>
    public enum PlineOrientation : byte
    {
        /// <summary>
        /// Polyline is open.
        /// </summary>
        Open,

        /// <summary>
        /// Polyline is closed and directionally clockwise (signed area is negative).
        /// </summary>
        Clockwise,

        /// <summary>
        /// Polyline is closed and directionally counter clockwise (signed area is positive).
        /// </summary>
        CounterClockwise
    }

    /// <summary>
    /// Result from finding the closest point on a polyline to some query point
    /// (<c>PlineSourceExtensions.ClosestPoint</c>).
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public readonly struct ClosestPointResult<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// The start vertex index of the closest segment.
        /// </summary>
        public readonly int SegStartIndex;

        /// <summary>
        /// The closest point on the closest segment.
        /// </summary>
        public readonly Vector2<T> SegPoint;

        /// <summary>
        /// The distance between the query point and <see cref="SegPoint"/>.
        /// </summary>
        public readonly T Distance;

        /// <summary>
        /// Creates a new closest point result.
        /// </summary>
        /// <param name="segStartIndex">The start vertex index of the closest segment.</param>
        /// <param name="segPoint">The closest point on the closest segment.</param>
        /// <param name="distance">The distance between the query point and <paramref name="segPoint"/>.</param>
        public ClosestPointResult(int segStartIndex, Vector2<T> segPoint, T distance)
        {
            SegStartIndex = segStartIndex;
            SegPoint = segPoint;
            Distance = distance;
        }
    }

    /// <summary>
    /// Holds the optional parameters used when performing a parallel polyline offset
    /// (<see cref="PlineOffset"/>).
    /// </summary>
    /// <remarks>
    /// Upstream <c>cavalier_contours</c> removed <see cref="SliceJoinEps"/> in version 0.9.0 and
    /// replaced it with the <c>TouchingLoopBehavior</c> and <c>CoincidentSegmentBehavior</c>
    /// options. This port tracks upstream 0.8.0 and therefore has neither of those options; slice
    /// joining is still controlled purely by <see cref="SliceJoinEps"/>.
    /// </remarks>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class PlineOffsetOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Spatial index of all the polyline segment bounding boxes (or boxes no smaller, e.g. one
        /// built by <c>PlineSourceExtensions.CreateApproxAabbIndex</c> is valid). If <c>null</c> is
        /// given then the index is computed internally. Either
        /// <c>PlineSourceExtensions.CreateApproxAabbIndex</c> or
        /// <c>PlineSourceExtensions.CreateAabbIndex</c> may be used to create the spatial index; the
        /// only restriction is that the spatial index bounding boxes must be at least big enough to
        /// contain the segments.
        /// </summary>
        /// <value>An existing spatial index to reuse, or <c>null</c>. Default is <c>null</c>.</value>
        public StaticAABB2DIndex<T>? AabbIndex { get; set; }

        /// <summary>
        /// If <c>true</c> then self intersects will be properly handled by the offset algorithm; if
        /// <c>false</c> then self intersecting polylines may not offset correctly.
        /// </summary>
        /// <remarks>
        /// This flag selects a different code path rather than merely tuning a tolerance: with
        /// <c>false</c> a closed polyline is offset from a single raw offset polyline, with
        /// <c>true</c> a dual raw offset (both <c>+offset</c> and <c>-offset</c>) is constructed and
        /// sliced. The flag only has an effect on closed polylines; open polylines always take the
        /// dual raw offset path. Handling self intersects of closed polylines requires more memory
        /// and computation.
        /// </remarks>
        /// <value><c>true</c> to handle self intersects. Default is <c>false</c>.</value>
        public bool HandleSelfIntersects { get; set; }

        /// <summary>
        /// Fuzzy comparison epsilon used for determining if two positions are equal.
        /// </summary>
        /// <value>Positional comparison epsilon. Default is <c>1e-5</c>.</value>
        public T PosEqualEps { get; set; }

        /// <summary>
        /// Fuzzy comparison epsilon used for determining if two positions are equal when stitching
        /// polyline slices together.
        /// </summary>
        /// <value>Slice stitching epsilon. Default is <c>1e-4</c>.</value>
        public T SliceJoinEps { get; set; }

        /// <summary>
        /// Fuzzy comparison epsilon used when testing the distance of slices to the original
        /// polyline for validity (slices closer to the source than the offset distance are
        /// discarded).
        /// </summary>
        /// <value>Offset distance validity epsilon. Default is <c>1e-4</c>.</value>
        public T OffsetDistEps { get; set; }

        /// <summary>
        /// Creates a new set of offset options with all values set to their defaults:
        /// <see cref="AabbIndex"/> = <c>null</c>, <see cref="HandleSelfIntersects"/> =
        /// <c>false</c>, <see cref="PosEqualEps"/> = <c>1e-5</c>, <see cref="SliceJoinEps"/> =
        /// <c>1e-4</c> and <see cref="OffsetDistEps"/> = <c>1e-4</c>.
        /// </summary>
        public PlineOffsetOptions()
        {
            AabbIndex = null;
            HandleSelfIntersects = false;
            PosEqualEps = T.CreateChecked(1e-5);
            SliceJoinEps = T.CreateChecked(1e-4);
            OffsetDistEps = T.CreateChecked(1e-4);
        }
    }

    /// <summary>
    /// Information about the outcome of a polyline containment test (<see cref="PlineContains"/>).
    /// </summary>
    public enum PlineContainsResult : byte
    {
        /// <summary>
        /// Input was not valid to perform the containment test operation. This is returned (rather
        /// than an exception being thrown) whenever either polyline is open or degenerate, i.e. has
        /// fewer than two vertexes.
        /// </summary>
        InvalidInput,

        /// <summary>
        /// Pline1 entirely inside of pline2 with no intersects.
        /// </summary>
        Pline1InsidePline2,

        /// <summary>
        /// Pline2 entirely inside of pline1 with no intersects.
        /// </summary>
        Pline2InsidePline1,

        /// <summary>
        /// Pline1 is disjoint from pline2 (no intersects and neither polyline is inside of the
        /// other).
        /// </summary>
        Disjoint,

        /// <summary>
        /// Pline1 intersects with pline2 in at least one place.
        /// </summary>
        Intersected
    }

    /// <summary>
    /// Holds the optional parameters used when performing a polyline containment test
    /// (<see cref="PlineContains"/>).
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class PlineContainsOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Spatial index for the first polyline argument of the containment test. If <c>null</c> is
        /// given then an approximate index is computed internally. The bounding boxes must be at
        /// least big enough to contain the segments.
        /// </summary>
        /// <value>An existing spatial index to reuse, or <c>null</c>. Default is <c>null</c>.</value>
        public StaticAABB2DIndex<T>? Pline1AabbIndex { get; set; }

        /// <summary>
        /// Fuzzy comparison epsilon used for determining if two positions are equal.
        /// </summary>
        /// <value>Positional comparison epsilon. Default is <c>1e-5</c>.</value>
        public T PosEqualEps { get; set; }

        /// <summary>
        /// Creates a new set of containment options with all values set to their defaults:
        /// <see cref="Pline1AabbIndex"/> = <c>null</c> and <see cref="PosEqualEps"/> =
        /// <c>1e-5</c>.
        /// </summary>
        public PlineContainsOptions()
        {
            Pline1AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
        }
    }

    /// <summary>
    /// Boolean operation to apply to polylines.
    /// </summary>
    public enum BooleanOp : byte
    {
        /// <summary>
        /// Return the union of the polylines (all area covered by either polyline).
        /// </summary>
        Or,

        /// <summary>
        /// Return the intersection of the polylines (only area covered by both polylines).
        /// </summary>
        And,

        /// <summary>
        /// Return the exclusion of the second polyline from the first (area of pline1 that is not
        /// covered by pline2).
        /// </summary>
        Not,

        /// <summary>
        /// Exclusive OR between the polylines (area covered by exactly one of the two polylines).
        /// </summary>
        Xor
    }

    /// <summary>
    /// Represents one of the polyline results from a boolean operation between two polylines.
    /// </summary>
    /// <typeparam name="P">Polyline source type of the resultant polyline.</typeparam>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class BooleanResultPline<P, T>
        where P : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly ReadOnlyCollection<BooleanPlineSlice<T>> _subslices;

        /// <summary>
        /// Resultant polyline.
        /// </summary>
        public P Pline { get; set; }

        /// <summary>
        /// Slices of the two input polylines that were stitched together to form the
        /// <see cref="Pline"/> result. Each slice records which input polyline it came from and the
        /// view data needed to replay the vertexes of that portion, which lets a caller map result
        /// geometry back onto the inputs. If the boolean result info is not
        /// <see cref="BooleanResultInfo.Intersected"/> this collection may be empty (results that
        /// are whole input polylines carry no subslices).
        /// </summary>
        /// <value>
        /// A read-only snapshot of the subslices taken when this result was constructed. It is not a
        /// live view and callers must not expect to be able to mutate it.
        /// </value>
        public IReadOnlyList<BooleanPlineSlice<T>> Subslices => _subslices;

        /// <summary>
        /// Creates a new boolean result polyline.
        /// </summary>
        /// <param name="pline">Resultant polyline.</param>
        /// <param name="subslices">
        /// Slices that were stitched together to form <paramref name="pline"/>; the list is wrapped
        /// in a read-only collection and exposed through <see cref="Subslices"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="subslices"/> is null.</exception>
        public BooleanResultPline(P pline, List<BooleanPlineSlice<T>> subslices)
        {
            ArgumentNullException.ThrowIfNull(subslices);

            Pline = pline;
            _subslices = new ReadOnlyCollection<BooleanPlineSlice<T>>(subslices);
        }
    }

    /// <summary>
    /// Information about what happened during a boolean operation between two polylines.
    /// </summary>
    public enum BooleanResultInfo : byte
    {
        /// <summary>
        /// Input was not valid to perform the boolean operation, i.e. one of the polylines was open
        /// or had fewer than two vertexes.
        /// </summary>
        InvalidInput,

        /// <summary>
        /// Pline1 entirely inside of pline2 with no intersects.
        /// </summary>
        Pline1InsidePline2,

        /// <summary>
        /// Pline2 entirely inside of pline1 with no intersects.
        /// </summary>
        Pline2InsidePline1,

        /// <summary>
        /// Pline1 is disjoint from pline2 (no intersects and neither polyline is inside of the
        /// other).
        /// </summary>
        Disjoint,

        /// <summary>
        /// Pline1 exactly overlaps pline2 (same geometric path).
        /// </summary>
        Overlapping,

        /// <summary>
        /// Pline1 intersects with pline2 but is not exactly overlapping with the same geometric
        /// path; this is the only case in which the result is assembled from stitched slices.
        /// </summary>
        Intersected
    }

    /// <summary>
    /// Result of performing a boolean operation between two polylines.
    /// </summary>
    /// <typeparam name="P">Polyline source type of the resultant polylines.</typeparam>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class BooleanResult<P, T>
        where P : IPlineSource<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly ReadOnlyCollection<BooleanResultPline<P, T>> _posPlines;
        private readonly ReadOnlyCollection<BooleanResultPline<P, T>> _negPlines;

        /// <summary>
        /// Positive remaining space polylines, i.e. the outer loops that bound the area kept by the
        /// operation. For the conventional counter clockwise input these come back counter
        /// clockwise. Internally a stitched result is classified as positive when its orientation
        /// matches the orientation of pline2, so the sense follows the inputs rather than being
        /// forced to counter clockwise.
        /// </summary>
        /// <value>
        /// A read-only snapshot taken when this result was constructed. It is not a live view and
        /// callers must not expect to be able to mutate it.
        /// </value>
        public IReadOnlyList<BooleanResultPline<P, T>> PosPlines => _posPlines;

        /// <summary>
        /// Negative subtracted space polylines, i.e. holes cut out of the polylines in
        /// <see cref="PosPlines"/>. These run opposite to the positive loops, so for the
        /// conventional counter clockwise input they come back clockwise. Only the union
        /// (<see cref="BooleanOp.Or"/>) and the non-intersecting <see cref="BooleanOp.Not"/> and
        /// <see cref="BooleanOp.Xor"/> cases can produce negative polylines.
        /// </summary>
        /// <value>
        /// A read-only snapshot taken when this result was constructed. It is not a live view and
        /// callers must not expect to be able to mutate it.
        /// </value>
        public IReadOnlyList<BooleanResultPline<P, T>> NegPlines => _negPlines;

        /// <summary>
        /// Information about what happened during the boolean operation.
        /// </summary>
        public BooleanResultInfo ResultInfo { get; }

        /// <summary>
        /// Creates a new boolean result.
        /// </summary>
        /// <param name="posPlines">Positive remaining space polylines.</param>
        /// <param name="negPlines">Negative subtracted space polylines.</param>
        /// <param name="resultInfo">Information about what happened during the boolean operation.</param>
        public BooleanResult(List<BooleanResultPline<P, T>> posPlines, List<BooleanResultPline<P, T>> negPlines, BooleanResultInfo resultInfo)
        {
            _posPlines = new ReadOnlyCollection<BooleanResultPline<P, T>>(posPlines);
            _negPlines = new ReadOnlyCollection<BooleanResultPline<P, T>>(negPlines);
            ResultInfo = resultInfo;
        }

        /// <summary>
        /// Creates a boolean result with no positive and no negative polylines, used when the
        /// operation leaves no area behind or the input was rejected.
        /// </summary>
        /// <param name="resultInfo">Information about what happened during the boolean operation.</param>
        /// <returns>An empty boolean result carrying <paramref name="resultInfo"/>.</returns>
        public static BooleanResult<P, T> Empty(BooleanResultInfo resultInfo)
        {
            return new BooleanResult<P, T>(new List<BooleanResultPline<P, T>>(), new List<BooleanResultPline<P, T>>(), resultInfo);
        }

        /// <summary>
        /// Creates a boolean result directly from whole (unsliced) polylines. Used for the cases in
        /// which no stitching was required, e.g. when the inputs are disjoint or one is entirely
        /// contained in the other; every result polyline gets an empty
        /// <see cref="BooleanResultPline{P, T}.Subslices"/> collection.
        /// </summary>
        /// <param name="posPlines">Whole polylines to record as positive remaining space.</param>
        /// <param name="negPlines">Whole polylines to record as negative subtracted space.</param>
        /// <param name="resultInfo">Information about what happened during the boolean operation.</param>
        /// <returns>A boolean result wrapping the given polylines.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="posPlines"/> or <paramref name="negPlines"/> is null.</exception>
        public static BooleanResult<P, T> FromWholePlines(IEnumerable<P> posPlines, IEnumerable<P> negPlines, BooleanResultInfo resultInfo)
        {
            ArgumentNullException.ThrowIfNull(posPlines);
            ArgumentNullException.ThrowIfNull(negPlines);

            var pos = new List<BooleanResultPline<P, T>>();
            foreach (var p in posPlines)
            {
                pos.Add(new BooleanResultPline<P, T>(p, new List<BooleanPlineSlice<T>>()));
            }

            var neg = new List<BooleanResultPline<P, T>>();
            foreach (var p in negPlines)
            {
                neg.Add(new BooleanResultPline<P, T>(p, new List<BooleanPlineSlice<T>>()));
            }

            return new BooleanResult<P, T>(pos, neg, resultInfo);
        }
    }

    /// <summary>
    /// Holds the optional parameters used when performing a boolean operation between two polylines
    /// (<see cref="PlineBoolean"/>).
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class PlineBooleanOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Spatial index for the first polyline argument of the boolean operation. If <c>null</c> is
        /// given then an approximate index is computed internally. The bounding boxes must be at
        /// least big enough to contain the segments.
        /// </summary>
        /// <value>An existing spatial index to reuse, or <c>null</c>. Default is <c>null</c>.</value>
        public StaticAABB2DIndex<T>? Pline1AabbIndex { get; set; }

        /// <summary>
        /// Fuzzy comparison epsilon used for determining if two positions are equal.
        /// </summary>
        /// <value>Positional comparison epsilon. Default is <c>1e-5</c>.</value>
        public T PosEqualEps { get; set; }

        /// <summary>
        /// If not <c>null</c> then this epsilon value is used to determine if a result polyline is
        /// collapsed, that is has no area according to <c>abs(area) &lt; eps</c>. Polylines that are
        /// collapsed are not included in the result. If <c>null</c> then no such filtering takes
        /// place and sliver polylines with a near-zero area are returned as-is.
        /// </summary>
        /// <remarks>
        /// Setting a value is useful to avoid inconsistent results due to floating point
        /// thresholding, or simply if collapsed polylines are never wanted in the result. Upstream's
        /// own boolean test harness sets <c>1e-5</c> for exactly this reason: it runs every case
        /// with the input direction inverted and with the vertex indexes cycled, and without pruning
        /// the collapsed areas those variations do not agree with each other.
        /// </remarks>
        /// <value>Collapsed area epsilon, or <c>null</c> for no filtering. Default is <c>null</c>.</value>
        public T? CollapsedAreaEps { get; set; }

        /// <summary>
        /// Creates a new set of boolean options with all values set to their defaults:
        /// <see cref="Pline1AabbIndex"/> = <c>null</c>, <see cref="PosEqualEps"/> = <c>1e-5</c> and
        /// <see cref="CollapsedAreaEps"/> = <c>null</c>.
        /// </summary>
        public PlineBooleanOptions()
        {
            Pline1AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
            CollapsedAreaEps = null;
        }
    }

    /// <summary>
    /// Controls which self intersects to include when scanning a polyline for self intersects.
    /// </summary>
    /// <remarks>
    /// This type is currently unused by this port: upstream's <c>scan_for_self_intersect</c> was
    /// never ported, so nothing consumes the value. It is kept so that the public surface matches
    /// upstream 0.8.0.
    /// </remarks>
    public enum SelfIntersectsInclude : byte
    {
        /// <summary>
        /// Include all (local and global) self intersects.
        /// </summary>
        All,

        /// <summary>
        /// Include only local self intersects, defined as being between two adjacent polyline
        /// segments.
        /// </summary>
        Local,

        /// <summary>
        /// Include only global self intersects, defined as being between two non-adjacent polyline
        /// segments.
        /// </summary>
        Global
    }

    /// <summary>
    /// Holds the optional parameters used when scanning a polyline for self intersects.
    /// </summary>
    /// <remarks>
    /// This type is currently unused by this port: upstream's <c>scan_for_self_intersect</c> was
    /// never ported, so no method in this library accepts these options. It is kept so that the
    /// public surface matches upstream 0.8.0.
    /// </remarks>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class PlineSelfIntersectOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Spatial index for the polyline being scanned. If <c>null</c> is given then an approximate
        /// index would be computed internally.
        /// </summary>
        /// <value>An existing spatial index to reuse, or <c>null</c>. Default is <c>null</c>.</value>
        public StaticAABB2DIndex<T>? AabbIndex { get; set; }

        /// <summary>
        /// Fuzzy comparison epsilon used for determining if two positions are equal.
        /// </summary>
        /// <value>Positional comparison epsilon. Default is <c>1e-5</c>.</value>
        public T PosEqualEps { get; set; }

        /// <summary>
        /// Controls whether to include all (local and global), only local, or only global self
        /// intersects.
        /// </summary>
        /// <value>
        /// Which self intersects to include. Default is <see cref="SelfIntersectsInclude.All"/>.
        /// </value>
        public SelfIntersectsInclude Include { get; set; }

        /// <summary>
        /// Creates a new set of self intersect options with all values set to their defaults:
        /// <see cref="AabbIndex"/> = <c>null</c>, <see cref="PosEqualEps"/> = <c>1e-5</c> and
        /// <see cref="Include"/> = <see cref="SelfIntersectsInclude.All"/>.
        /// </summary>
        public PlineSelfIntersectOptions()
        {
            AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
            Include = SelfIntersectsInclude.All;
        }
    }

    /// <summary>
    /// Holds the optional parameters used when finding all intersects between two polylines
    /// (<see cref="PlineIntersects"/>).
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class FindIntersectsOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Spatial index for the first polyline argument used to find intersects. If <c>null</c> is
        /// given then an approximate index is computed internally. The bounding boxes must be at
        /// least big enough to contain the segments.
        /// </summary>
        /// <value>An existing spatial index to reuse, or <c>null</c>. Default is <c>null</c>.</value>
        public StaticAABB2DIndex<T>? Pline1AabbIndex { get; set; }

        /// <summary>
        /// Fuzzy comparison epsilon used for determining if two positions are equal.
        /// </summary>
        /// <value>Positional comparison epsilon. Default is <c>1e-5</c>.</value>
        public T PosEqualEps { get; set; }

        /// <summary>
        /// Creates a new set of find intersects options with all values set to their defaults:
        /// <see cref="Pline1AabbIndex"/> = <c>null</c> and <see cref="PosEqualEps"/> =
        /// <c>1e-5</c>.
        /// </summary>
        public FindIntersectsOptions()
        {
            Pline1AabbIndex = null;
            PosEqualEps = T.CreateChecked(1e-5);
        }
    }

    /// <summary>
    /// Represents a polyline intersect at a single point.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public readonly struct PlineBasicIntersect<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Starting vertex index of the first polyline segment involved in the intersect.
        /// </summary>
        public readonly int StartIndex1;

        /// <summary>
        /// Starting vertex index of the second polyline segment involved in the intersect.
        /// </summary>
        public readonly int StartIndex2;

        /// <summary>
        /// Point at which the intersect occurs.
        /// </summary>
        public readonly Vector2<T> Point;

        /// <summary>
        /// Creates a new basic (single point) polyline intersect.
        /// </summary>
        /// <param name="startIndex1">Starting vertex index of the first polyline segment involved.</param>
        /// <param name="startIndex2">Starting vertex index of the second polyline segment involved.</param>
        /// <param name="point">Point at which the intersect occurs.</param>
        public PlineBasicIntersect(int startIndex1, int startIndex2, Vector2<T> point)
        {
            StartIndex1 = startIndex1;
            StartIndex2 = startIndex2;
            Point = point;
        }
    }

    /// <summary>
    /// Represents an overlapping polyline intersect segment, that is a stretch over which the two
    /// segments run along the same geometric path rather than crossing at a single point.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public readonly struct PlineOverlappingIntersect<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Starting vertex index of the first polyline segment involved in the overlapping
        /// intersect.
        /// </summary>
        public readonly int StartIndex1;

        /// <summary>
        /// Starting vertex index of the second polyline segment involved in the intersect.
        /// </summary>
        public readonly int StartIndex2;

        /// <summary>
        /// First end point of the overlapping intersect (closest to the second segment start).
        /// </summary>
        public readonly Vector2<T> Point1;

        /// <summary>
        /// Second end point of the overlapping intersect (furthest from the second segment start).
        /// </summary>
        public readonly Vector2<T> Point2;

        /// <summary>
        /// Creates a new overlapping polyline intersect.
        /// </summary>
        /// <param name="startIndex1">Starting vertex index of the first polyline segment involved.</param>
        /// <param name="startIndex2">Starting vertex index of the second polyline segment involved.</param>
        /// <param name="point1">
        /// First end point of the overlap (closest to the second segment start).
        /// </param>
        /// <param name="point2">
        /// Second end point of the overlap (furthest from the second segment start).
        /// </param>
        public PlineOverlappingIntersect(int startIndex1, int startIndex2, Vector2<T> point1, Vector2<T> point2)
        {
            StartIndex1 = startIndex1;
            StartIndex2 = startIndex2;
            Point1 = point1;
            Point2 = point2;
        }
    }

    /// <summary>
    /// Discriminator telling which kind of intersect a <see cref="PlineIntersect{T}"/> holds.
    /// </summary>
    public enum PlineIntersectKind : byte
    {
        /// <summary>
        /// The intersect is a <see cref="PlineBasicIntersect{T}"/> occurring at a single point.
        /// </summary>
        Basic,

        /// <summary>
        /// The intersect is a <see cref="PlineOverlappingIntersect{T}"/> spanning a stretch of
        /// coincident geometry.
        /// </summary>
        Overlapping
    }

    /// <summary>
    /// Represents a polyline intersect that may be either a <see cref="PlineBasicIntersect{T}"/> or
    /// a <see cref="PlineOverlappingIntersect{T}"/>.
    /// </summary>
    /// <remarks>
    /// This is the C# stand-in for upstream's Rust enum: <see cref="Kind"/> acts as the tag and only
    /// the matching one of <see cref="Basic"/> and <see cref="Overlapping"/> carries meaningful
    /// data, the other is left at its default value.
    /// </remarks>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public readonly struct PlineIntersect<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Which of the two payloads is populated.
        /// </summary>
        public readonly PlineIntersectKind Kind;

        /// <summary>
        /// The single point intersect. Only meaningful when <see cref="Kind"/> is
        /// <see cref="PlineIntersectKind.Basic"/>.
        /// </summary>
        public readonly PlineBasicIntersect<T> Basic;

        /// <summary>
        /// The overlapping intersect. Only meaningful when <see cref="Kind"/> is
        /// <see cref="PlineIntersectKind.Overlapping"/>.
        /// </summary>
        public readonly PlineOverlappingIntersect<T> Overlapping;

        private PlineIntersect(PlineBasicIntersect<T> basic)
        {
            Kind = PlineIntersectKind.Basic;
            Basic = basic;
            Overlapping = default;
        }

        private PlineIntersect(PlineOverlappingIntersect<T> overlapping)
        {
            Kind = PlineIntersectKind.Overlapping;
            Basic = default;
            Overlapping = overlapping;
        }

        /// <summary>
        /// Creates an intersect holding a <see cref="PlineBasicIntersect{T}"/>.
        /// </summary>
        /// <param name="startIndex1">Starting vertex index of the first polyline segment involved.</param>
        /// <param name="startIndex2">Starting vertex index of the second polyline segment involved.</param>
        /// <param name="point">Point at which the intersect occurs.</param>
        /// <returns>
        /// An intersect whose <see cref="Kind"/> is <see cref="PlineIntersectKind.Basic"/>.
        /// </returns>
        public static PlineIntersect<T> NewBasic(int startIndex1, int startIndex2, Vector2<T> point)
        {
            return new PlineIntersect<T>(new PlineBasicIntersect<T>(startIndex1, startIndex2, point));
        }

        /// <summary>
        /// Creates an intersect holding a <see cref="PlineOverlappingIntersect{T}"/>.
        /// </summary>
        /// <param name="startIndex1">Starting vertex index of the first polyline segment involved.</param>
        /// <param name="startIndex2">Starting vertex index of the second polyline segment involved.</param>
        /// <param name="point1">
        /// First end point of the overlap (closest to the second segment start).
        /// </param>
        /// <param name="point2">
        /// Second end point of the overlap (furthest from the second segment start).
        /// </param>
        /// <returns>
        /// An intersect whose <see cref="Kind"/> is <see cref="PlineIntersectKind.Overlapping"/>.
        /// </returns>
        public static PlineIntersect<T> NewOverlapping(int startIndex1, int startIndex2, Vector2<T> point1, Vector2<T> point2)
        {
            return new PlineIntersect<T>(new PlineOverlappingIntersect<T>(startIndex1, startIndex2, point1, point2));
        }
    }

    /// <summary>
    /// Callback interface for visiting polyline intersects as they are discovered.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public interface IPlineIntersectVisitor<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Visits a single point intersect.
        /// </summary>
        /// <param name="intr">The basic intersect that was found.</param>
        /// <returns><c>true</c> to continue visiting, <c>false</c> to stop the traversal early.</returns>
        bool VisitBasicIntr(PlineBasicIntersect<T> intr);

        /// <summary>
        /// Visits an overlapping intersect.
        /// </summary>
        /// <param name="intr">The overlapping intersect that was found.</param>
        /// <returns><c>true</c> to continue visiting, <c>false</c> to stop the traversal early.</returns>
        bool VisitOverlappingIntr(PlineOverlappingIntersect<T> intr);
    }

    /// <summary>
    /// Bundles up the context information for visiting intersections of two polylines: which
    /// segment of one polyline is currently being tested. Two of these are supplied on every visit,
    /// one for each polyline.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public readonly struct PlineIntersectVisitContext<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Index of the start vertex of the segment being visited.
        /// </summary>
        public readonly int VertexIndex;

        /// <summary>
        /// Start vertex of the segment being visited (the vertex at <see cref="VertexIndex"/>).
        /// </summary>
        public readonly PlineVertex<T> V1;

        /// <summary>
        /// End vertex of the segment being visited (the vertex following <see cref="VertexIndex"/>,
        /// wrapping for closed polylines).
        /// </summary>
        public readonly PlineVertex<T> V2;

        /// <summary>
        /// Creates a new intersect visit context.
        /// </summary>
        /// <param name="vertexIndex">Index of the start vertex of the segment being visited.</param>
        /// <param name="v1">Start vertex of the segment being visited.</param>
        /// <param name="v2">End vertex of the segment being visited.</param>
        public PlineIntersectVisitContext(int vertexIndex, PlineVertex<T> v1, PlineVertex<T> v2)
        {
            VertexIndex = vertexIndex;
            V1 = v1;
            V2 = v2;
        }
    }

    /// <summary>
    /// Visitor interface used to visit intersections between two polylines, with the raw segment
    /// intersect result and the context of both segments involved.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public interface ITwoPlinesIntersectVisitor<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Visits the intersection.
        /// </summary>
        /// <param name="intersect">The segment-to-segment intersect result.</param>
        /// <param name="pline1Context">Context of the segment of the first polyline involved.</param>
        /// <param name="pline2Context">Context of the segment of the second polyline involved.</param>
        /// <returns><c>true</c> to continue visiting, <c>false</c> to stop the traversal early.</returns>
        bool Visit(PlineSegIntr<T> intersect, in PlineIntersectVisitContext<T> pline1Context, in PlineIntersectVisitContext<T> pline2Context);
    }

    /// <summary>
    /// Callback interface for visiting polyline vertexes.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public interface IPlineVertexVisitor<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Visits a vertex.
        /// </summary>
        /// <param name="vertex">The vertex being visited.</param>
        /// <returns><c>true</c> to continue visiting, <c>false</c> to stop the traversal early.</returns>
        bool VisitVertex(PlineVertex<T> vertex);
    }

    /// <summary>
    /// Callback interface for visiting polyline segments (two consecutive vertexes).
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public interface IPlineSegVisitor<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Visits a segment.
        /// </summary>
        /// <param name="v1">Start vertex of the segment.</param>
        /// <param name="v2">End vertex of the segment.</param>
        /// <returns><c>true</c> to continue visiting, <c>false</c> to stop the traversal early.</returns>
        bool VisitSeg(PlineVertex<T> v1, PlineVertex<T> v2);
    }

    /// <summary>
    /// Represents a collection of basic and overlapping polyline intersects, as returned when
    /// finding all intersects between two polylines.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public class PlineIntersectsCollection<T>
        where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// All the single point intersects that were found.
        /// </summary>
        public List<PlineBasicIntersect<T>> BasicIntersects { get; set; }

        /// <summary>
        /// All the overlapping intersects that were found, each spanning a stretch of coincident
        /// geometry.
        /// </summary>
        public List<PlineOverlappingIntersect<T>> OverlappingIntersects { get; set; }

        /// <summary>
        /// Creates a new intersects collection from the given lists.
        /// </summary>
        /// <param name="basicIntersects">The single point intersects.</param>
        /// <param name="overlappingIntersects">The overlapping intersects.</param>
        /// <exception cref="ArgumentNullException"><paramref name="basicIntersects"/> or <paramref name="overlappingIntersects"/> is null.</exception>
        public PlineIntersectsCollection(List<PlineBasicIntersect<T>> basicIntersects, List<PlineOverlappingIntersect<T>> overlappingIntersects)
        {
            ArgumentNullException.ThrowIfNull(basicIntersects);
            ArgumentNullException.ThrowIfNull(overlappingIntersects);

            BasicIntersects = basicIntersects;
            OverlappingIntersects = overlappingIntersects;
        }

        /// <summary>
        /// Creates a new intersects collection with both lists empty.
        /// </summary>
        /// <returns>An empty intersects collection.</returns>
        public static PlineIntersectsCollection<T> NewEmpty()
        {
            return new PlineIntersectsCollection<T>(new List<PlineBasicIntersect<T>>(), new List<PlineOverlappingIntersect<T>>());
        }
    }

    /// <summary>
    /// Represents an open polyline slice where there was overlap between two polylines across one or
    /// more segments. Such a slice is built by joining consecutive overlapping intersects end to
    /// start.
    /// </summary>
    /// <remarks>
    /// The source polyline for <see cref="ViewData"/> is always the second polyline.
    /// </remarks>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public readonly struct OverlappingSlice<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Start vertex indexes of the slice according to the original polylines that overlapped;
        /// <c>First</c> indexes into the first polyline and <c>Second</c> into the second.
        /// </summary>
        public readonly (int First, int Second) StartIndexes;

        /// <summary>
        /// End vertex indexes of the slice according to the original polylines that overlapped;
        /// <c>First</c> indexes into the first polyline and <c>Second</c> into the second.
        /// </summary>
        public readonly (int First, int Second) EndIndexes;

        /// <summary>
        /// View data for the slice; the source polyline is always the second polyline. Combine it
        /// with that source to obtain a <see cref="PlineView{T}"/> over the slice vertexes.
        /// </summary>
        public readonly PlineViewData<T> ViewData;

        /// <summary>
        /// If <c>true</c> then the overlapping slice forms a closed loop back on itself, otherwise
        /// it does not.
        /// </summary>
        public readonly bool IsLoop;

        /// <summary>
        /// If <c>true</c> then the overlapping slice was formed by segments that have opposing
        /// directions, determined by the sign of the dot product of the two segment tangent vectors
        /// at the start of the overlap.
        /// </summary>
        public readonly bool OpposingDirections;

        /// <summary>
        /// Creates a new overlapping slice from already computed parts.
        /// </summary>
        /// <param name="startIndexes">Start vertex indexes in the first and second polyline.</param>
        /// <param name="endIndexes">End vertex indexes in the first and second polyline.</param>
        /// <param name="viewData">View data for the slice, relative to the second polyline.</param>
        /// <param name="isLoop">Whether the slice forms a closed loop back on itself.</param>
        /// <param name="opposingDirections">
        /// Whether the overlapping segments run in opposing directions.
        /// </param>
        public OverlappingSlice(
            (int First, int Second) startIndexes,
            (int First, int Second) endIndexes,
            PlineViewData<T> viewData,
            bool isLoop,
            bool opposingDirections)
        {
            StartIndexes = startIndexes;
            EndIndexes = endIndexes;
            ViewData = viewData;
            IsLoop = isLoop;
            OpposingDirections = opposingDirections;
        }

        /// <summary>
        /// Builds an overlapping slice from the overlapping intersect it starts at and, optionally,
        /// the overlapping intersect it ends at.
        /// </summary>
        /// <param name="pline1">
        /// First polyline of the overlap, used only to compute the segment tangent that decides
        /// <see cref="OpposingDirections"/>.
        /// </param>
        /// <param name="pline2">
        /// Second polyline of the overlap; this is the source polyline the resulting
        /// <see cref="ViewData"/> refers to.
        /// </param>
        /// <param name="startIntr">Overlapping intersect at which the slice starts.</param>
        /// <param name="endIntr">
        /// Overlapping intersect at which the slice ends, or <c>null</c> when the slice is formed by
        /// the single <paramref name="startIntr"/> alone. If the end intersect closes back onto the
        /// start point then the resulting slice has <see cref="IsLoop"/> set.
        /// </param>
        /// <param name="posEqualEps">
        /// Fuzzy comparison epsilon used for determining if two positions are equal while splitting
        /// segments at the intersect points.
        /// </param>
        /// <returns>The overlapping slice spanning from the start to the end intersect.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pline1"/> or <paramref name="pline2"/> is null.</exception>
        public static OverlappingSlice<T> New(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            PlineOverlappingIntersect<T> startIntr,
            PlineOverlappingIntersect<T>? endIntr,
            T posEqualEps)
        {
            ArgumentNullException.ThrowIfNull(pline1);
            ArgumentNullException.ThrowIfNull(pline2);

            var startV1 = pline1.Get(startIntr.StartIndex1);
            var startV2 = pline1.Get(pline1.NextWrappingIndex(startIntr.StartIndex1));
            var startU1 = pline2.Get(startIntr.StartIndex2);
            var startU2 = pline2.Get(pline2.NextWrappingIndex(startIntr.StartIndex2));

            bool opposingDirections;
            {
                var t1 = PlineSeg.SegTangentVector(startV1, startV2, startIntr.Point1);
                var t2 = PlineSeg.SegTangentVector(startU1, startU2, startIntr.Point1);
                opposingDirections = t1.Dot(t2) < T.Zero;
            }

            var startIndexes = (startIntr.StartIndex1, startIntr.StartIndex2);

            PlineVertex<T> CreateUpdatedStart()
            {
                var split1 = PlineSeg.SegSplitAtPoint(startU1, startU2, startIntr.Point1, posEqualEps);
                var split2 = PlineSeg.SegSplitAtPoint(split1.SplitVertex, startU2, startIntr.Point2, posEqualEps);
                return split2.UpdatedStart;
            }

            if (endIntr == null)
            {
                var updatedStart = CreateUpdatedStart();
                var updatedEndBulge = updatedStart.Bulge;
                var endPoint = startIntr.Point2;
                var endIndexOffset = 0;

                var viewData = new PlineViewData<T>(
                    startIndexes.Item2,
                    endIndexOffset,
                    updatedStart,
                    updatedEndBulge,
                    endPoint,
                    false
                );

                return new OverlappingSlice<T>(startIndexes, startIndexes, viewData, false, opposingDirections);
            }
            else
            {
                var endIntrVal = endIntr.Value;
                if (endIntrVal.Point2.FuzzyEqEps(startIntr.Point1, posEqualEps))
                {
                    var viewData = new PlineViewData<T>(
                        startIndexes.Item2,
                        pline2.VertexCount - 1,
                        startU1,
                        pline2.Get(pline2.VertexCount - 1).Bulge,
                        endIntrVal.Point2,
                        false
                    );

                    return new OverlappingSlice<T>(startIndexes, startIndexes, viewData, true, opposingDirections);
                }
                else
                {
                    var endPoint = endIntrVal.Point2;
                    var endIndexes = (endIntrVal.StartIndex1, endIntrVal.StartIndex2);
                    var endIndexOffset = pline2.FwdWrappingDist(startIndexes.Item2, endIntrVal.StartIndex2);

                    if (startIntr.StartIndex2 == endIntrVal.StartIndex2)
                    {
                        var updatedStart = CreateUpdatedStart();
                        var updatedEndBulge = updatedStart.Bulge;

                        var viewData = new PlineViewData<T>(
                            startIndexes.Item2,
                            endIndexOffset,
                            updatedStart,
                            updatedEndBulge,
                            endPoint,
                            false
                        );

                        return new OverlappingSlice<T>(startIndexes, endIndexes, viewData, false, opposingDirections);
                    }
                    else
                    {
                        var updatedStart = PlineSeg.SegSplitAtPoint(startU1, startU2, startIntr.Point1, posEqualEps).SplitVertex;

                        var endU1 = pline2.Get(endIntrVal.StartIndex2);
                        var endU2 = pline2.Get(pline2.NextWrappingIndex(endIntrVal.StartIndex2));

                        var split1 = PlineSeg.SegSplitAtPoint(endU1, endU2, endIntrVal.Point1, posEqualEps);
                        var split2 = PlineSeg.SegSplitAtPoint(split1.SplitVertex, endU2, endIntrVal.Point2, posEqualEps);
                        var updatedEnd = split2.UpdatedStart;

                        var viewData = new PlineViewData<T>(
                            startIndexes.Item2,
                            endIndexOffset,
                            updatedStart,
                            updatedEnd.Bulge,
                            endPoint,
                            false
                        );

                        return new OverlappingSlice<T>(startIndexes, endIndexes, viewData, false, opposingDirections);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Open polyline slice created in the process of performing a polyline boolean operation. These
    /// are the pieces that get pruned and then stitched back together into the closed result
    /// polylines, and they are what <see cref="BooleanResultPline{P, T}.Subslices"/> reports.
    /// </summary>
    /// <typeparam name="T">Floating point scalar type used for the polyline coordinates.</typeparam>
    public readonly struct BooleanPlineSlice<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// View data for the slice; can be used with the source polyline to form a view of the
        /// vertexes for the slice, see <see cref="View(IPlineSource{T})"/>.
        /// </summary>
        public readonly PlineViewData<T> ViewData;

        /// <summary>
        /// If <c>true</c> then the source polyline for this slice is pline1 from the boolean
        /// operation, otherwise it is pline2.
        /// </summary>
        public readonly bool SourceIsPline1;

        /// <summary>
        /// Whether the slice is an overlapping slice or not, that is whether both polylines in the
        /// boolean operation overlapped along this slice.
        /// </summary>
        public readonly bool Overlapping;

        /// <summary>
        /// Creates a new boolean slice from already computed parts.
        /// </summary>
        /// <param name="viewData">View data for the slice.</param>
        /// <param name="sourceIsPline1">
        /// <c>true</c> if the source polyline for the slice is pline1, <c>false</c> if it is pline2.
        /// </param>
        /// <param name="overlapping">Whether the slice is an overlapping slice.</param>
        public BooleanPlineSlice(PlineViewData<T> viewData, bool sourceIsPline1, bool overlapping)
        {
            ViewData = viewData;
            SourceIsPline1 = sourceIsPline1;
            Overlapping = overlapping;
        }

        /// <summary>
        /// Creates a non-overlapping boolean slice from open polyline slice view data, optionally
        /// reversing the direction in which the slice is traversed.
        /// </summary>
        /// <param name="data">View data of the open polyline slice to copy.</param>
        /// <param name="sourceIsPline1">
        /// <c>true</c> if the source polyline for the slice is pline1, <c>false</c> if it is pline2.
        /// </param>
        /// <param name="inverted">
        /// Whether the resulting slice traverses the source segments in the inverted direction.
        /// </param>
        /// <returns>
        /// A boolean slice with <see cref="Overlapping"/> set to <c>false</c>.
        /// </returns>
        public static BooleanPlineSlice<T> FromOpenPlineSlice(in PlineViewData<T> data, bool sourceIsPline1, bool inverted)
        {
            var viewData = new PlineViewData<T>(
                data.StartIndex,
                data.EndIndexOffset,
                data.UpdatedStart,
                data.UpdatedEndBulge,
                data.EndPoint,
                inverted
            );
            return new BooleanPlineSlice<T>(viewData, sourceIsPline1, false);
        }

        /// <summary>
        /// Creates an overlapping boolean slice from an <see cref="OverlappingSlice{T}"/>. Because
        /// the view data of an overlapping slice always refers to the second polyline, the resulting
        /// slice always has <see cref="SourceIsPline1"/> set to <c>false</c>.
        /// </summary>
        /// <param name="source">
        /// Source polyline the slice refers to. Upstream uses it only for a debug assertion that the
        /// view data is valid for the source; this port does not perform that check here.
        /// </param>
        /// <param name="overlappingSlice">The overlapping slice to convert.</param>
        /// <param name="inverted">
        /// Whether the resulting slice traverses the source segments in the inverted direction.
        /// </param>
        /// <returns>
        /// A boolean slice with <see cref="Overlapping"/> set to <c>true</c>.
        /// </returns>
        public static BooleanPlineSlice<T> FromOverlapping(IPlineSource<T> source, in OverlappingSlice<T> overlappingSlice, bool inverted)
        {
            var viewData = new PlineViewData<T>(
                overlappingSlice.StartIndexes.Second,
                overlappingSlice.ViewData.EndIndexOffset,
                overlappingSlice.ViewData.UpdatedStart,
                overlappingSlice.ViewData.UpdatedEndBulge,
                overlappingSlice.ViewData.EndPoint,
                inverted
            );
            return new BooleanPlineSlice<T>(viewData, false, true);
        }

        /// <summary>
        /// Forms a view over the source polyline that exposes just the vertexes of this slice.
        /// </summary>
        /// <param name="source">
        /// The polyline this slice was cut from; pass pline1 when <see cref="SourceIsPline1"/> is
        /// <c>true</c>, otherwise pline2.
        /// </param>
        /// <returns>An open polyline view of the slice over <paramref name="source"/>.</returns>
        public PlineView<T> View(IPlineSource<T> source)
        {
            return ViewData.View(source);
        }
    }

    /// <summary>
    /// Outcome of validating <see cref="PlineViewData{T}"/> against a source polyline, used for
    /// debugging and assertions.
    /// </summary>
    /// <remarks>
    /// Upstream carries diagnostic payload data on each failing variant (the offending offset,
    /// point, bulge and so on); this port models it as a plain enum, so only the failure category is
    /// reported.
    /// </remarks>
    public enum ViewDataValidation : byte
    {
        /// <summary>
        /// The source polyline has fewer than two vertexes and therefore no segments to view.
        /// </summary>
        SourceHasNoSegments,

        /// <summary>
        /// The end index offset of the view is larger than the vertex count of the source polyline.
        /// </summary>
        OffsetOutOfRange,

        /// <summary>
        /// The updated start position does not lie on the segment identified by the start index.
        /// </summary>
        UpdatedStartNotOnSegment,

        /// <summary>
        /// The end point does not lie on the segment identified by the end index.
        /// </summary>
        EndPointNotOnSegment,

        /// <summary>
        /// The end point lies directly on top of the start vertex of the final segment, which is
        /// never valid because the view would end with a zero length segment.
        /// </summary>
        EndPointOnFinalOffsetVertex,

        /// <summary>
        /// The view spans a single segment (end index offset of zero) but the updated end bulge does
        /// not match the updated start bulge.
        /// </summary>
        UpdatedBulgeDoesNotMatch,

        /// <summary>
        /// The view data is valid for the source polyline.
        /// </summary>
        IsValid
    }
}
