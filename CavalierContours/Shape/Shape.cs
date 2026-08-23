using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using CavalierContours.Core;
using CavalierContours.Polyline;
using CavalierContours.Spatial;

namespace CavalierContours.Shape
{
    /// <summary>
    /// One offset polyline produced from a single input loop of a <see cref="Shape{T}"/>, together
    /// with the index of the input loop it came from.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
    /// <remarks>
    /// This type is public so that intermediate results of the shape offset algorithm can be
    /// inspected for visualization and testing.
    /// </remarks>
    public class OffsetLoop<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Gets or sets the index of the parent loop in the original input shape, counting all
        /// counter-clockwise polylines first and then all clockwise polylines.
        /// </summary>
        public int ParentLoopIdx { get; set; }

        /// <summary>
        /// Gets or sets the offset polyline together with its spatial index.
        /// </summary>
        public IndexedPolyline<T> IndexedPline { get; set; }

        /// <summary>
        /// Creates an offset loop with parent index 0 and an empty polyline.
        /// </summary>
        public OffsetLoop() : this(0, new IndexedPolyline<T>(new Polyline<T>()))
        {
        }

        /// <summary>
        /// Creates an offset loop for the given parent index and offset polyline.
        /// </summary>
        /// <param name="parentLoopIdx">Index of the input loop this offset was derived from.</param>
        /// <param name="indexedPline">The offset polyline with its spatial index.</param>
        public OffsetLoop(int parentLoopIdx, IndexedPolyline<T> indexedPline)
        {
            ParentLoopIdx = parentLoopIdx;
            IndexedPline = indexedPline;
        }
    }

    /// <summary>
    /// A polyline paired with a spatial index built from the approximate bounding boxes of its
    /// segments, which makes intersection and proximity queries against it fast.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
    public class IndexedPolyline<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Gets or sets the polyline geometry.
        /// </summary>
        /// <remarks>
        /// Replacing the polyline or mutating it in place does not rebuild
        /// <see cref="SpatialIndex"/>, which then no longer matches the geometry.
        /// </remarks>
        public Polyline<T> Polyline { get; set; }

        /// <summary>
        /// Gets or sets the spatial index built from the polyline's segment bounding boxes.
        /// </summary>
        public StaticAABB2DIndex<T> SpatialIndex { get; set; }

        /// <summary>
        /// Wraps the given polyline and immediately builds its approximate segment bounding box
        /// index.
        /// </summary>
        /// <param name="polyline">
        /// The polyline to index. It is stored by reference, not copied.
        /// </param>
        public IndexedPolyline(Polyline<T> polyline)
        {
            Polyline = polyline;
            SpatialIndex = polyline.CreateApproxAabbIndex();
        }

        /// <summary>
        /// Offsets this single loop as part of a shape offset, reusing the existing spatial index
        /// and with self intersection handling turned off.
        /// </summary>
        /// <param name="offset">
        /// Offset distance. Positive offsets go to the left of the polyline direction, negative to
        /// the right.
        /// </param>
        /// <param name="options">Epsilons to use for the offset.</param>
        /// <returns>
        /// The resulting offset polylines. A single input loop may produce zero, one or several
        /// output loops.
        /// </returns>
        /// <remarks>
        /// Self intersects are deliberately not handled here because the surrounding shape offset
        /// resolves intersections globally across all loops in a later step.
        /// </remarks>
        public List<Polyline<T>> ParallelOffsetForShape(T offset, ShapeOffsetOptions<T> options)
        {
            var opts = new PlineOffsetOptions<T>
            {
                AabbIndex = SpatialIndex,
                HandleSelfIntersects = false,
                PosEqualEps = options.PosEqualEps,
                SliceJoinEps = options.SliceJoinEps,
                OffsetDistEps = options.OffsetDistEps
            };

            return PlineOffset.ParallelOffset<Polyline<T>, T>(Polyline, offset, opts);
        }
    }

    /// <summary>
    /// Options controlling the fuzzy comparisons performed by
    /// <see cref="Shape{T}.ParallelOffset(T, ShapeOffsetOptions{T})"/>.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the epsilons.</typeparam>
    public class ShapeOffsetOptions<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Gets or sets the epsilon used to decide whether two positions are equal. Defaults to
        /// 1e-5.
        /// </summary>
        public T PosEqualEps { get; set; }

        /// <summary>
        /// Gets or sets the epsilon used when testing the distance of a slice to the original
        /// polylines for validity. Defaults to 1e-4.
        /// </summary>
        public T OffsetDistEps { get; set; }

        /// <summary>
        /// Gets or sets the epsilon used to decide whether two positions are equal when stitching
        /// polyline slices together. Defaults to 1e-4.
        /// </summary>
        public T SliceJoinEps { get; set; }

        /// <summary>
        /// Creates options with the default epsilons (1e-5 for
        /// <see cref="PosEqualEps"/>, 1e-4 for <see cref="OffsetDistEps"/> and
        /// <see cref="SliceJoinEps"/>).
        /// </summary>
        public ShapeOffsetOptions()
        {
            PosEqualEps = T.CreateChecked(1e-5);
            OffsetDistEps = T.CreateChecked(1e-4);
            SliceJoinEps = T.CreateChecked(1e-4);
        }

        /// <summary>
        /// Creates options with explicit epsilons.
        /// </summary>
        /// <param name="posEqualEps">Value for <see cref="PosEqualEps"/>.</param>
        /// <param name="offsetDistEps">Value for <see cref="OffsetDistEps"/>.</param>
        /// <param name="sliceJoinEps">Value for <see cref="SliceJoinEps"/>.</param>
        public ShapeOffsetOptions(T posEqualEps, T offsetDistEps, T sliceJoinEps)
        {
            PosEqualEps = posEqualEps;
            OffsetDistEps = offsetDistEps;
            SliceJoinEps = sliceJoinEps;
        }
    }

    /// <summary>
    /// All intersection points found between one pair of offset loops, used to dissect those loops
    /// into slices.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
    /// <remarks>
    /// Overlapping intersects are flattened into two basic intersects, one for each end of the
    /// overlap, so this list holds only point intersects.
    /// </remarks>
    public class SlicePointSet<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Gets or sets the index of the first offset loop of the pair.
        /// </summary>
        public int LoopIdx1 { get; set; }

        /// <summary>
        /// Gets or sets the index of the second offset loop of the pair.
        /// </summary>
        public int LoopIdx2 { get; set; }

        /// <summary>
        /// Gets or sets the intersection points between the two loops. Each point carries the
        /// segment start index on the first loop and on the second loop.
        /// </summary>
        public List<PlineBasicIntersect<T>> SlicePoints { get; set; }

        /// <summary>
        /// Creates an intersection set for a pair of offset loops.
        /// </summary>
        /// <param name="loopIdx1">Index of the first offset loop.</param>
        /// <param name="loopIdx2">Index of the second offset loop.</param>
        /// <param name="slicePoints">Intersection points between the two loops.</param>
        public SlicePointSet(int loopIdx1, int loopIdx2, List<PlineBasicIntersect<T>> slicePoints)
        {
            LoopIdx1 = loopIdx1;
            LoopIdx2 = loopIdx2;
            SlicePoints = slicePoints;
        }
    }

    /// <summary>
    /// A portion of an offset loop that passed the distance validation and is ready to be stitched
    /// into the final result.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
    /// <remarks>
    /// The slice is kept as a view into the source offset polyline rather than as a copy, so the
    /// source loop must stay alive and unchanged until stitching is done.
    /// </remarks>
    public readonly struct DissectedSlice<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        /// <summary>
        /// Index of the offset loop this slice was cut from.
        /// </summary>
        public readonly int SourceIdx;

        /// <summary>
        /// View data describing where the slice starts and ends within the source polyline.
        /// </summary>
        public readonly PlineViewData<T> VData;

        /// <summary>
        /// Creates a dissected slice.
        /// </summary>
        /// <param name="sourceIdx">Index of the offset loop the slice was cut from.</param>
        /// <param name="vData">View data describing the slice within that loop.</param>
        public DissectedSlice(int sourceIdx, PlineViewData<T> vData)
        {
            SourceIdx = sourceIdx;
            VData = vData;
        }
    }

    /// <summary>
    /// A multi-polyline area: a set of closed loops partitioned by orientation into filled regions
    /// and holes.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
    /// <remarks>
    /// <para>
    /// Counter-clockwise loops (<see cref="CcwPlines"/>) carry positive area and describe islands,
    /// that is filled material. Clockwise loops (<see cref="CwPlines"/>) carry negative area and
    /// describe holes cut out of those islands. This is the only thing that distinguishes the two
    /// collections; orientation, not nesting order, decides which is which.
    /// </para>
    /// <para>
    /// Offsetting a shape via <see cref="ParallelOffset(T, ShapeOffsetOptions{T})"/> is a global
    /// operation over all loops at once, so islands and holes interact: loops may merge into one
    /// another or split apart, holes may vanish, and the result is again a valid
    /// <see cref="Shape{T}"/>.
    /// </para>
    /// </remarks>
    public class Shape<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly ReadOnlyCollection<IndexedPolyline<T>> _ccwPlines;
        private readonly ReadOnlyCollection<IndexedPolyline<T>> _cwPlines;

        /// <summary>
        /// Gets the counter-clockwise loops of the shape, that is the positive area islands.
        /// </summary>
        public IReadOnlyList<IndexedPolyline<T>> CcwPlines => _ccwPlines;

        /// <summary>
        /// Gets the clockwise loops of the shape, that is the negative area holes.
        /// </summary>
        public IReadOnlyList<IndexedPolyline<T>> CwPlines => _cwPlines;

        /// <summary>
        /// Gets the spatial index over the area bounding boxes of all loops.
        /// </summary>
        /// <remarks>
        /// Index positions run over all <see cref="CcwPlines"/> first and then over all
        /// <see cref="CwPlines"/>. With one ccw and two cw loops, position 0 is the ccw loop and
        /// positions 1 and 2 are the first and second cw loops.
        /// </remarks>
        public StaticAABB2DIndex<T> PlinesIndex { get; }

        /// <summary>
        /// Creates a shape from already partitioned loops and a matching spatial index.
        /// </summary>
        /// <param name="ccwPlines">Counter-clockwise loops (islands).</param>
        /// <param name="cwPlines">Clockwise loops (holes).</param>
        /// <param name="plinesIndex">
        /// Spatial index over the loop bounding boxes, ordered ccw loops first then cw loops. It is
        /// taken as given and not validated against the lists.
        /// </param>
        /// <remarks>
        /// No orientation check is performed; the caller is responsible for putting each loop in
        /// the right list. Use <see cref="FromPlines(IEnumerable{Polyline{T}})"/> to have that done
        /// automatically.
        /// </remarks>
        public Shape(List<IndexedPolyline<T>> ccwPlines, List<IndexedPolyline<T>> cwPlines, StaticAABB2DIndex<T> plinesIndex)
        {
            _ccwPlines = new ReadOnlyCollection<IndexedPolyline<T>>(ccwPlines);
            _cwPlines = new ReadOnlyCollection<IndexedPolyline<T>>(cwPlines);
            PlinesIndex = plinesIndex;
        }

        /// <summary>
        /// Builds a shape from a set of polylines, partitioning them by orientation.
        /// </summary>
        /// <param name="plines">
        /// The polylines to form the shape from. Polylines with fewer than two vertexes are
        /// filtered out. Every remaining polyline is classified by its orientation:
        /// counter-clockwise ones become islands, all others become holes.
        /// </param>
        /// <returns>The resulting shape with its spatial index already built.</returns>
        /// <exception cref="InvalidOperationException">
        /// A polyline that passed the vertex count filter has an empty spatial index and therefore
        /// no bounds.
        /// </exception>
        /// <remarks>
        /// The shape keeps references to the supplied polylines rather than copies of them.
        /// Mutating one of them afterwards invalidates both its own segment index and the shape's
        /// <see cref="PlinesIndex"/>, and any later offset will operate on stale bounds.
        /// </remarks>
        public static Shape<T> FromPlines(IEnumerable<Polyline<T>> plines)
        {
            var ccwPlines = new List<IndexedPolyline<T>>();
            var cwPlines = new List<IndexedPolyline<T>>();

            foreach (var pl in plines)
            {
                if (pl.VertexCount > 1)
                {
                    if (pl.Orientation() == PlineOrientation.CounterClockwise)
                    {
                        ccwPlines.Add(new IndexedPolyline<T>(pl));
                    }
                    else
                    {
                        cwPlines.Add(new IndexedPolyline<T>(pl));
                    }
                }
            }

            var builder = new StaticAABB2DIndexBuilder<T>(ccwPlines.Count + cwPlines.Count);

            void AddAllBounds(List<IndexedPolyline<T>> list)
            {
                foreach (var pline in list)
                {
                    var bounds = pline.SpatialIndex.Bounds;
                    if (bounds == null)
                    {
                        throw new InvalidOperationException("expect non-empty polyline");
                    }
                    builder.Add(bounds.Value.MinX, bounds.Value.MinY, bounds.Value.MaxX, bounds.Value.MaxY);
                }
            }

            AddAllBounds(ccwPlines);
            AddAllBounds(cwPlines);

            var plinesIndex = builder.Build();

            return new Shape<T>(ccwPlines, cwPlines, plinesIndex);
        }

        /// <summary>
        /// Returns an empty shape with no loops at all.
        /// </summary>
        /// <returns>A shape whose two loop collections and spatial index are empty.</returns>
        public static Shape<T> Empty()
        {
            return new Shape<T>(
                new List<IndexedPolyline<T>>(),
                new List<IndexedPolyline<T>>(),
                new StaticAABB2DIndexBuilder<T>(0).Build()
            );
        }

        /// <summary>
        /// Offsets the whole shape by <paramref name="offset"/>, treating all islands and holes as
        /// one connected area.
        /// </summary>
        /// <param name="offset">
        /// Offset distance. A positive value grows the filled area outward and shrinks the holes; a
        /// negative value does the opposite.
        /// </param>
        /// <param name="options">Epsilons controlling the fuzzy comparisons.</param>
        /// <returns>
        /// The offset shape, again partitioned into islands and holes. An empty shape is returned
        /// when the offset removes all material.
        /// </returns>
        /// <remarks>
        /// Merging and splitting of loops is handled automatically: loops that run into each other
        /// are joined, loops that pinch off are split, and loops that collapse are dropped. The
        /// work is done in four steps, each of which is also exposed individually:
        /// <see cref="CreateOffsetLoopsWithIndex(T, ShapeOffsetOptions{T})"/>,
        /// <see cref="FindIntersectsBetweenOffsetLoops(List{OffsetLoop{T}}, List{OffsetLoop{T}}, StaticAABB2DIndex{T}, T)"/>,
        /// <see cref="CreateValidSlicesFromIntersects(List{OffsetLoop{T}}, List{OffsetLoop{T}}, List{SlicePointSet{T}}, T, ShapeOffsetOptions{T})"/>
        /// and
        /// <see cref="StitchSlicesTogether(List{DissectedSlice{T}}, List{OffsetLoop{T}}, List{OffsetLoop{T}}, T, T)"/>.
        /// </remarks>
        public Shape<T> ParallelOffset(T offset, ShapeOffsetOptions<T> options)
        {
            var (ccwOffsetLoops, cwOffsetLoops, offsetLoopsIndex) = CreateOffsetLoopsWithIndex(offset, options);

            if (ccwOffsetLoops.Count == 0 && cwOffsetLoops.Count == 0)
            {
                return Empty();
            }

            var slicePointSets = FindIntersectsBetweenOffsetLoops(
                ccwOffsetLoops,
                cwOffsetLoops,
                offsetLoopsIndex,
                options.PosEqualEps
            );

            var slicesData = CreateValidSlicesFromIntersects(
                ccwOffsetLoops,
                cwOffsetLoops,
                slicePointSets,
                offset,
                options
            );

            return StitchSlicesTogether(
                slicesData,
                ccwOffsetLoops,
                cwOffsetLoops,
                options.PosEqualEps,
                options.SliceJoinEps
            );
        }

        /// <summary>
        /// Step 1 of the shape offset: offsets every input loop on its own and indexes the results.
        /// </summary>
        /// <param name="offset">Offset distance, see <see cref="ParallelOffset(T, ShapeOffsetOptions{T})"/>.</param>
        /// <param name="options">Epsilons controlling the fuzzy comparisons.</param>
        /// <returns>
        /// A tuple of the counter-clockwise offset loops, the clockwise offset loops, and a spatial
        /// index over the bounding boxes of all of them, ordered ccw loops first then cw loops.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// An offset loop has an empty spatial index and therefore no bounds.
        /// </exception>
        /// <remarks>
        /// Each result loop is classified by the sign of its area, not by the orientation of its
        /// parent. Loops that are obviously spurious are dropped right away: for a positive offset
        /// a negative area loop coming from an island, and for a negative offset a positive area
        /// loop coming from a hole. Exposed publicly so intermediate results can be visualized and
        /// tested.
        /// </remarks>
        public (List<OffsetLoop<T>> CcwOffsetLoops, List<OffsetLoop<T>> CwOffsetLoops, StaticAABB2DIndex<T> OffsetLoopsIndex) CreateOffsetLoopsWithIndex(
            T offset,
            ShapeOffsetOptions<T> options)
        {
            var ccwOffsetLoops = new List<OffsetLoop<T>>();
            var cwOffsetLoops = new List<OffsetLoop<T>>();
            int parentIdx = 0;

            foreach (var pline in CcwPlines)
            {
                foreach (var offsetPline in pline.ParallelOffsetForShape(offset, options))
                {
                    T area = offsetPline.Area();
                    if (offset > T.Zero && area < T.Zero)
                    {
                        continue;
                    }

                    var offsetLoop = new OffsetLoop<T>(parentIdx, new IndexedPolyline<T>(offsetPline));

                    if (area < T.Zero)
                    {
                        cwOffsetLoops.Add(offsetLoop);
                    }
                    else
                    {
                        ccwOffsetLoops.Add(offsetLoop);
                    }
                }
                parentIdx++;
            }

            foreach (var pline in CwPlines)
            {
                foreach (var offsetPline in pline.ParallelOffsetForShape(offset, options))
                {
                    T area = offsetPline.Area();
                    if (offset < T.Zero && area > T.Zero)
                    {
                        continue;
                    }

                    var offsetLoop = new OffsetLoop<T>(parentIdx, new IndexedPolyline<T>(offsetPline));

                    if (area < T.Zero)
                    {
                        cwOffsetLoops.Add(offsetLoop);
                    }
                    else
                    {
                        ccwOffsetLoops.Add(offsetLoop);
                    }
                }
                parentIdx++;
            }

            var builder = new StaticAABB2DIndexBuilder<T>(ccwOffsetLoops.Count + cwOffsetLoops.Count);

            void AddAllBounds(List<OffsetLoop<T>> list)
            {
                foreach (var l in list)
                {
                    var bounds = l.IndexedPline.SpatialIndex.Bounds;
                    if (bounds == null)
                    {
                        throw new InvalidOperationException("expect non-empty polyline");
                    }
                    builder.Add(bounds.Value.MinX, bounds.Value.MinY, bounds.Value.MaxX, bounds.Value.MaxY);
                }
            }

            AddAllBounds(ccwOffsetLoops);
            AddAllBounds(cwOffsetLoops);

            var offsetLoopsIndex = builder.Build();

            return (ccwOffsetLoops, cwOffsetLoops, offsetLoopsIndex);
        }

        private struct CollectVisitor : IQueryVisitor
        {
            public List<int> Results;
            public bool Visit(int indexPos)
            {
                Results.Add(indexPos);
                return true;
            }
        }

        /// <summary>
        /// Step 2 of the shape offset: finds all intersections between the offset loops produced by
        /// step 1.
        /// </summary>
        /// <param name="ccwOffsetLoops">Counter-clockwise offset loops from step 1.</param>
        /// <param name="cwOffsetLoops">Clockwise offset loops from step 1.</param>
        /// <param name="offsetLoopsIndex">Spatial index over the offset loop bounds from step 1.</param>
        /// <param name="posEqualEps">Epsilon for position equality comparisons.</param>
        /// <returns>
        /// One <see cref="SlicePointSet{T}"/> per intersecting pair of loops. Pairs without any
        /// intersection are omitted, and each unordered pair appears at most once.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// An offset loop has an empty spatial index and therefore no bounds.
        /// </exception>
        /// <remarks>
        /// The spatial index is used to restrict the pairwise intersection tests to loops whose
        /// bounding boxes overlap. Overlapping intersects are converted into two slice points, one
        /// for each end of the overlap. Exposed publicly so intersection points can be visualized
        /// and tested.
        /// </remarks>
        public List<SlicePointSet<T>> FindIntersectsBetweenOffsetLoops(
            List<OffsetLoop<T>> ccwOffsetLoops,
            List<OffsetLoop<T>> cwOffsetLoops,
            StaticAABB2DIndex<T> offsetLoopsIndex,
            T posEqualEps)
        {
            int offsetLoopCount = ccwOffsetLoops.Count + cwOffsetLoops.Count;
            var slicePointSets = new List<SlicePointSet<T>>();
            var visitedLoopPairs = new HashSet<ulong>();
            var queryStack = new List<int>();
            var queryResults = new List<int>();

            for (int i = 0; i < offsetLoopCount; i++)
            {
                var loop1 = GetLoop(i, ccwOffsetLoops, cwOffsetLoops);
                var spatialIdx1 = loop1.IndexedPline.SpatialIndex;
                var bounds = spatialIdx1.Bounds;
                if (bounds == null)
                {
                    throw new InvalidOperationException("expect non-empty polyline");
                }

                queryResults.Clear();
                var collectVisitor = new CollectVisitor { Results = queryResults };
                offsetLoopsIndex.VisitQueryWithStack(
                    bounds.Value.MinX,
                    bounds.Value.MinY,
                    bounds.Value.MaxX,
                    bounds.Value.MaxY,
                    ref collectVisitor,
                    queryStack
                );

                for (int r = 0; r < queryResults.Count; r++)
                {
                    int j = queryResults[r];
                    if (i == j)
                    {
                        continue;
                    }

                    ulong reverseKey = ((ulong)(uint)j << 32) | (uint)i;
                    if (visitedLoopPairs.Contains(reverseKey))
                    {
                        continue;
                    }

                    ulong key = ((ulong)(uint)i << 32) | (uint)j;
                    visitedLoopPairs.Add(key);

                    var loop2 = GetLoop(j, ccwOffsetLoops, cwOffsetLoops);

                    var intrsOpts = new FindIntersectsOptions<T>
                    {
                        Pline1AabbIndex = spatialIdx1,
                        PosEqualEps = posEqualEps
                    };

                    var intersects = PlineIntersects.FindIntersects(
                        loop1.IndexedPline.Polyline,
                        loop2.IndexedPline.Polyline,
                        intrsOpts
                    );

                    if (intersects.BasicIntersects.Count == 0 && intersects.OverlappingIntersects.Count == 0)
                    {
                        continue;
                    }

                    var slicePoints = new List<PlineBasicIntersect<T>>();

                    foreach (var intr in intersects.BasicIntersects)
                    {
                        slicePoints.Add(intr);
                    }

                    foreach (var overlapIntr in intersects.OverlappingIntersects)
                    {
                        int startIndex1 = overlapIntr.StartIndex1;
                        int startIndex2 = overlapIntr.StartIndex2;
                        slicePoints.Add(new PlineBasicIntersect<T>(startIndex1, startIndex2, overlapIntr.Point1));
                        slicePoints.Add(new PlineBasicIntersect<T>(startIndex1, startIndex2, overlapIntr.Point2));
                    }

                    slicePointSets.Add(new SlicePointSet<T>(i, j, slicePoints));
                }
            }

            return slicePointSets;
        }

        private readonly struct DissectionPoint
        {
            public readonly int SegIdx;
            public readonly Vector2<T> Pos;

            public DissectionPoint(int segIdx, Vector2<T> pos)
            {
                SegIdx = segIdx;
                Pos = pos;
            }
        }

        /// <summary>
        /// Step 3 of the shape offset: cuts the offset loops at the intersection points from step 2
        /// and keeps only the slices that are far enough from the input loops.
        /// </summary>
        /// <param name="ccwOffsetLoops">Counter-clockwise offset loops from step 1.</param>
        /// <param name="cwOffsetLoops">Clockwise offset loops from step 1.</param>
        /// <param name="slicePointSets">Intersection data from step 2.</param>
        /// <param name="offset">The offset distance the slices are validated against.</param>
        /// <param name="options">
        /// Epsilons; <see cref="ShapeOffsetOptions{T}.PosEqualEps"/> and
        /// <see cref="ShapeOffsetOptions{T}.OffsetDistEps"/> are used here.
        /// </param>
        /// <returns>
        /// The valid slices, ready to be stitched. Offset loops that had no intersection points at
        /// all are carried over whole, as a single slice covering the entire loop, provided they
        /// pass validation.
        /// </returns>
        /// <remarks>
        /// Validity is decided by sampling segment midpoints of the slice and checking that they
        /// keep the required distance from every input loop other than the slice's own parent.
        /// Where possible a midpoint of a segment not created by an intersection is used, because
        /// a segment ending at an intersection point is always exactly at the offset distance and
        /// would make an invalid slice look valid. Slices are returned as views into the source
        /// polylines, so nothing is copied. Exposed publicly so individual slices can be visualized
        /// and tested.
        /// </remarks>
        public List<DissectedSlice<T>> CreateValidSlicesFromIntersects(
            List<OffsetLoop<T>> ccwOffsetLoops,
            List<OffsetLoop<T>> cwOffsetLoops,
            List<SlicePointSet<T>> slicePointSets,
            T offset,
            ShapeOffsetOptions<T> options)
        {
            int offsetLoopCount = ccwOffsetLoops.Count + cwOffsetLoops.Count;
            T posEqualEps = options.PosEqualEps;
            T offsetDistEps = options.OffsetDistEps;

            var slicePointsLookup = new Dictionary<int, List<int>>();
            for (int setIdx = 0; setIdx < slicePointSets.Count; setIdx++)
            {
                var set = slicePointSets[setIdx];
                if (!slicePointsLookup.TryGetValue(set.LoopIdx1, out var list1))
                {
                    list1 = new List<int>();
                    slicePointsLookup[set.LoopIdx1] = list1;
                }
                list1.Add(setIdx);

                if (!slicePointsLookup.TryGetValue(set.LoopIdx2, out var list2))
                {
                    list2 = new List<int>();
                    slicePointsLookup[set.LoopIdx2] = list2;
                }
                list2.Add(setIdx);
            }

            PlineViewData<T>? CreateSlice(in DissectionPoint pt1, in DissectionPoint pt2, Polyline<T> offsetLoop)
            {
                return PlineViewData<T>.FromSlicePoints(
                    offsetLoop,
                    pt1.Pos,
                    pt1.SegIdx,
                    pt2.Pos,
                    pt2.SegIdx,
                    posEqualEps
                );
            }

            bool IsSliceValid(in PlineViewData<T> vData, Polyline<T> offsetLoop, int parentIdx, List<int> qStack)
            {
                var sliceView = vData.View(offsetLoop);
                int vertexCount = sliceView.VertexCount;

                Vector2<T> midpoint1;
                Vector2<T>? midpoint2 = null;

                if (vertexCount > 3)
                {
                    midpoint1 = PlineSeg.SegMidpoint(sliceView.Get(1), sliceView.Get(2));
                }
                else if (vertexCount == 3)
                {
                    midpoint1 = PlineSeg.SegMidpoint(sliceView.Get(0), sliceView.Get(1));
                    midpoint2 = PlineSeg.SegMidpoint(sliceView.Get(1), sliceView.Get(2));
                }
                else
                {
                    midpoint1 = PlineSeg.SegMidpoint(sliceView.Get(0), sliceView.Get(1));
                }

                int totalPlines = CcwPlines.Count + CwPlines.Count;
                for (int inputLoopIdx = 0; inputLoopIdx < totalPlines; inputLoopIdx++)
                {
                    if (inputLoopIdx == parentIdx)
                    {
                        continue;
                    }

                    IndexedPolyline<T> parentLoop;
                    if (inputLoopIdx < CcwPlines.Count)
                    {
                        parentLoop = CcwPlines[inputLoopIdx];
                    }
                    else
                    {
                        parentLoop = CwPlines[inputLoopIdx - CcwPlines.Count];
                    }

                    if (!PlineOffset.PointValidForOffset(
                        parentLoop.Polyline,
                        offset,
                        parentLoop.SpatialIndex,
                        midpoint1,
                        qStack,
                        posEqualEps,
                        offsetDistEps))
                    {
                        return false;
                    }

                    if (midpoint2.HasValue)
                    {
                        if (!PlineOffset.PointValidForOffset(
                            parentLoop.Polyline,
                            offset,
                            parentLoop.SpatialIndex,
                            midpoint2.Value,
                            qStack,
                            posEqualEps,
                            offsetDistEps))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            var sortedIntrs = new List<DissectionPoint>();
            var slicesData = new List<DissectedSlice<T>>();
            var queryStack = new List<int>();

            for (int loopIdx = 0; loopIdx < offsetLoopCount; loopIdx++)
            {
                sortedIntrs.Clear();
                var currLoop = GetLoop(loopIdx, ccwOffsetLoops, cwOffsetLoops);

                if (slicePointsLookup.TryGetValue(loopIdx, out var slicePointSetIdxs))
                {
                    foreach (int setIdx in slicePointSetIdxs)
                    {
                        var set = slicePointSets[setIdx];
                        bool loopIsFirstIndex = set.LoopIdx1 == loopIdx;

                        foreach (var intrPt in set.SlicePoints)
                        {
                            int segIdx = loopIsFirstIndex ? intrPt.StartIndex1 : intrPt.StartIndex2;
                            sortedIntrs.Add(new DissectionPoint(segIdx, intrPt.Point));
                        }
                    }

                    sortedIntrs.Sort((a, b) =>
                    {
                        int cmp = a.SegIdx.CompareTo(b.SegIdx);
                        if (cmp != 0)
                        {
                            return cmp;
                        }

                        var segStart = currLoop.IndexedPline.Polyline.Get(a.SegIdx).Pos();
                        T dist1 = BaseMath.DistSquared(a.Pos, segStart);
                        T dist2 = BaseMath.DistSquared(b.Pos, segStart);
                        return dist1.CompareTo(dist2);
                    });

                    if (sortedIntrs.Count == 1)
                    {
                        var vData = PlineViewData<T>.FromEntirePline(currLoop.IndexedPline.Polyline);
                        if (IsSliceValid(vData, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                        {
                            slicesData.Add(new DissectedSlice<T>(loopIdx, vData));
                        }
                    }
                    else
                    {
                        for (int w = 0; w < sortedIntrs.Count - 1; w++)
                        {
                            var pt1 = sortedIntrs[w];
                            var pt2 = sortedIntrs[w + 1];
                            var vData = CreateSlice(pt1, pt2, currLoop.IndexedPline.Polyline);
                            if (vData != null && IsSliceValid(vData.Value, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                            {
                                slicesData.Add(new DissectedSlice<T>(loopIdx, vData.Value));
                            }
                        }

                        var lastPt = sortedIntrs[^1];
                        var firstPt = sortedIntrs[0];
                        var lastToStartVData = CreateSlice(lastPt, firstPt, currLoop.IndexedPline.Polyline);
                        if (lastToStartVData != null && IsSliceValid(lastToStartVData.Value, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                        {
                            slicesData.Add(new DissectedSlice<T>(loopIdx, lastToStartVData.Value));
                        }
                    }
                }
                else
                {
                    var vData = PlineViewData<T>.FromEntirePline(currLoop.IndexedPline.Polyline);
                    if (IsSliceValid(vData, currLoop.IndexedPline.Polyline, currLoop.ParentLoopIdx, queryStack))
                    {
                        slicesData.Add(new DissectedSlice<T>(loopIdx, vData));
                    }
                }
            }

            return slicesData;
        }

        private struct StitchVisitor : IQueryVisitor
        {
            public List<int> QueryResults;
            public bool[] VisitedSlicesIdxs;

            public bool Visit(int indexPos)
            {
                if (!VisitedSlicesIdxs[indexPos])
                {
                    QueryResults.Add(indexPos);
                }
                return true;
            }
        }

        /// <summary>
        /// Step 4 of the shape offset: connects the valid slices from step 3 end to end into closed
        /// loops and assembles the final shape.
        /// </summary>
        /// <param name="slicesData">Valid slices from step 3.</param>
        /// <param name="ccwOffsetLoops">Counter-clockwise offset loops from step 1, used to resolve slice sources.</param>
        /// <param name="cwOffsetLoops">Clockwise offset loops from step 1, used to resolve slice sources.</param>
        /// <param name="posEqualEps">Epsilon for position equality when appending slice vertexes.</param>
        /// <param name="sliceJoinEps">
        /// Epsilon defining how close a slice start must be to the current slice end to be
        /// considered its continuation.
        /// </param>
        /// <returns>
        /// The finished shape, with each stitched loop classified as island or hole by its
        /// orientation. An empty shape is returned when there are no slices.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The stitching loop ran more iterations than there are slices, which means the slice
        /// connectivity is inconsistent; or a result loop has no bounds.
        /// </exception>
        /// <remarks>
        /// A spatial index over the slice start points is used to find the continuation of each
        /// slice. When several candidates are within <paramref name="sliceJoinEps"/>, a candidate
        /// from the same source offset loop is preferred. Exposed publicly so the stitching can be
        /// observed and tested.
        /// </remarks>
        public Shape<T> StitchSlicesTogether(
            List<DissectedSlice<T>> slicesData,
            List<OffsetLoop<T>> ccwOffsetLoops,
            List<OffsetLoop<T>> cwOffsetLoops,
            T posEqualEps,
            T sliceJoinEps)
        {
            if (slicesData.Count == 0)
            {
                return Empty();
            }

            var ccwPlinesResult = new List<IndexedPolyline<T>>();
            var cwPlinesResult = new List<IndexedPolyline<T>>();

            var builder = new StaticAABB2DIndexBuilder<T>(slicesData.Count);
            for (int i = 0; i < slicesData.Count; i++)
            {
                var slice = slicesData[i];
                var startPoint = slice.VData.UpdatedStart.Pos();
                builder.Add(
                    startPoint.X - sliceJoinEps,
                    startPoint.Y - sliceJoinEps,
                    startPoint.X + sliceJoinEps,
                    startPoint.Y + sliceJoinEps
                );
            }
            var sliceStartsAabbIndex = builder.Build();

            var visitedSlicesIdxs = new bool[slicesData.Count];
            var queryResults = new List<int>();
            var queryStack = new List<int>();

            for (int sliceIdx = 0; sliceIdx < slicesData.Count; sliceIdx++)
            {
                if (visitedSlicesIdxs[sliceIdx])
                {
                    continue;
                }
                visitedSlicesIdxs[sliceIdx] = true;

                int currentIndex = sliceIdx;
                int loopCount = 0;
                int maxLoopCount = slicesData.Count;
                var currentPline = new Polyline<T>();

                while (true)
                {
                    if (loopCount > maxLoopCount)
                    {
                        throw new InvalidOperationException("loopCount exceeded maxLoopCount while stitching slices together");
                    }
                    loopCount++;

                    var currSlice = slicesData[currentIndex];
                    var sourceLoop = GetLoop(currSlice.SourceIdx, ccwOffsetLoops, cwOffsetLoops);
                    var sliceView = currSlice.VData.View(sourceLoop.IndexedPline.Polyline);
                    var sliceUserdataValues = sliceView.UserDataValues;
                    currentPline.ExtendRemoveRepeat(sliceView, posEqualEps);
                    currentPline.AddUserDataValues(sliceUserdataValues);

                    queryResults.Clear();
                    var sliceEndPoint = currSlice.VData.EndPoint;
                    var stitchVisitor = new StitchVisitor
                    {
                        QueryResults = queryResults,
                        VisitedSlicesIdxs = visitedSlicesIdxs
                    };

                    sliceStartsAabbIndex.VisitQueryWithStack(
                        sliceEndPoint.X - sliceJoinEps,
                        sliceEndPoint.Y - sliceJoinEps,
                        sliceEndPoint.X + sliceJoinEps,
                        sliceEndPoint.Y + sliceJoinEps,
                        ref stitchVisitor,
                        queryStack
                    );

                    if (queryResults.Count == 0)
                    {
                        if (currentPline.VertexCount > 2)
                        {
                            currentPline.RemoveAt(currentPline.VertexCount - 1);
                            currentPline.SetIsClosed(true);
                        }
                        bool isCcw = currentPline.Orientation() == PlineOrientation.CounterClockwise;
                        if (isCcw)
                        {
                            ccwPlinesResult.Add(new IndexedPolyline<T>(currentPline));
                        }
                        else
                        {
                            cwPlinesResult.Add(new IndexedPolyline<T>(currentPline));
                        }
                        break;
                    }

                    int nextIndex = -1;
                    for (int r = 0; r < queryResults.Count; r++)
                    {
                        int idx = queryResults[r];
                        if (slicesData[idx].SourceIdx == currSlice.SourceIdx)
                        {
                            nextIndex = idx;
                            break;
                        }
                    }

                    if (nextIndex == -1)
                    {
                        nextIndex = queryResults[0];
                    }

                    currentIndex = nextIndex;
                    visitedSlicesIdxs[currentIndex] = true;
                }
            }

            var plinesIndexBuilder = new StaticAABB2DIndexBuilder<T>(ccwPlinesResult.Count + cwPlinesResult.Count);

            void AddAllBounds(List<IndexedPolyline<T>> plines)
            {
                foreach (var pline in plines)
                {
                    var bounds = pline.SpatialIndex.Bounds;
                    if (bounds == null)
                    {
                        throw new InvalidOperationException("expect non-empty polyline");
                    }
                    plinesIndexBuilder.Add(bounds.Value.MinX, bounds.Value.MinY, bounds.Value.MaxX, bounds.Value.MaxY);
                }
            }

            AddAllBounds(ccwPlinesResult);
            AddAllBounds(cwPlinesResult);

            var plinesIndex = plinesIndexBuilder.Build();

            return new Shape<T>(ccwPlinesResult, cwPlinesResult, plinesIndex);
        }

        private static OffsetLoop<T> GetLoop(int i, List<OffsetLoop<T>> s1, List<OffsetLoop<T>> s2)
        {
            if (i < s1.Count)
            {
                return s1[i];
            }
            else
            {
                return s2[i - s1.Count];
            }
        }
    }
}