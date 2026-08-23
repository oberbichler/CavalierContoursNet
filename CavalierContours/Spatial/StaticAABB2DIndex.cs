using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Spatial
{
    /// <summary>
    /// Receives the index positions of the items found by a bounding box query on a
    /// <see cref="StaticAABB2DIndex{T}"/> and decides whether the query should carry on.
    /// </summary>
    /// <remarks>
    /// Implemented by value types so the query loop can call <see cref="Visit(int)"/> without a
    /// virtual dispatch; the visitor is passed by <c>ref</c> and may therefore accumulate state.
    /// </remarks>
    public interface IQueryVisitor
    {
        /// <summary>
        /// Called once for every item whose bounding box overlaps the query box.
        /// </summary>
        /// <param name="indexPos">
        /// Position of the item according to the order in which the boxes were handed to
        /// <see cref="StaticAABB2DIndexBuilder{T}.Add(T, T, T, T)"/>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> to continue the query, <see langword="false"/> to stop it
        /// immediately. No further items are visited after <see langword="false"/> is returned.
        /// </returns>
        bool Visit(int indexPos); // returns true to continue, false to break
    }

    /// <summary>
    /// Adapts a <see cref="Func{T, TResult}"/> to <see cref="IQueryVisitor"/> so a lambda can be
    /// used where a struct visitor is expected.
    /// </summary>
    public struct DelegateQueryVisitor : IQueryVisitor
    {
        private readonly Func<int, bool> _delegate;

        /// <summary>
        /// Creates a visitor that forwards every visited index position to <paramref name="del"/>.
        /// </summary>
        /// <param name="del">
        /// Callback invoked for each hit; it returns <see langword="true"/> to continue the query
        /// and <see langword="false"/> to stop it.
        /// </param>
        public DelegateQueryVisitor(Func<int, bool> del) => _delegate = del;

        /// <summary>
        /// Forwards <paramref name="indexPos"/> to the wrapped delegate.
        /// </summary>
        /// <param name="indexPos">Index position of the item that overlapped the query box.</param>
        /// <returns>Whatever the wrapped delegate returned: <see langword="true"/> to continue,
        /// <see langword="false"/> to stop.</returns>
        public bool Visit(int indexPos) => _delegate(indexPos);
    }

    /// <summary>
    /// Receives the items found by a nearest neighbor search on a
    /// <see cref="StaticAABB2DIndex{T}"/>, in order of increasing distance, and decides whether the
    /// search should carry on.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates and distances.</typeparam>
    /// <remarks>
    /// The search only stops when the visitor returns <see langword="false"/>; otherwise every item
    /// in the index is eventually visited.
    /// </remarks>
    public interface INeighborVisitor<T> where T : struct, IFloatingPointIeee754<T>
    {
        /// <summary>
        /// Called for each item in order of increasing distance from the query point.
        /// </summary>
        /// <param name="indexPos">
        /// Position of the item according to the order in which the boxes were handed to
        /// <see cref="StaticAABB2DIndexBuilder{T}.Add(T, T, T, T)"/>.
        /// </param>
        /// <param name="distSquared">
        /// Squared euclidean distance from the query point to the item's bounding box; zero if the
        /// query point lies inside that box.
        /// </param>
        /// <returns>
        /// <see langword="true"/> to continue visiting neighbors, <see langword="false"/> to stop
        /// the search immediately.
        /// </returns>
        bool Visit(int indexPos, T distSquared); // returns true to continue, false to break
    }

    /// <summary>
    /// Adapts a <see cref="Func{T1, T2, TResult}"/> to <see cref="INeighborVisitor{T}"/> so a lambda
    /// can be used where a struct visitor is expected.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the coordinates and distances.</typeparam>
    public struct DelegateNeighborVisitor<T> : INeighborVisitor<T> where T : struct, IFloatingPointIeee754<T>
    {
        private readonly Func<int, T, bool> _delegate;

        /// <summary>
        /// Creates a visitor that forwards every visited neighbor to <paramref name="del"/>.
        /// </summary>
        /// <param name="del">
        /// Callback invoked with the index position and the squared distance; it returns
        /// <see langword="true"/> to continue the search and <see langword="false"/> to stop it.
        /// </param>
        public DelegateNeighborVisitor(Func<int, T, bool> del) => _delegate = del;

        /// <summary>
        /// Forwards the visited neighbor to the wrapped delegate.
        /// </summary>
        /// <param name="indexPos">Index position of the visited item.</param>
        /// <param name="distSquared">Squared euclidean distance to that item's bounding box.</param>
        /// <returns>Whatever the wrapped delegate returned: <see langword="true"/> to continue,
        /// <see langword="false"/> to stop.</returns>
        public bool Visit(int indexPos, T distSquared) => _delegate(indexPos, distSquared);
    }

    /// <summary>
    /// Builds a <see cref="StaticAABB2DIndex{T}"/> from a fixed, known number of axis aligned
    /// bounding boxes.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the box coordinates.</typeparam>
    /// <remarks>
    /// <para>
    /// The builder has a strict contract: create it with the exact item <c>count</c>, call
    /// <see cref="Add(T, T, T, T)"/> exactly <c>count</c> times, then call <see cref="Build"/>
    /// once. The builder is spent afterwards and a second <see cref="Build"/> call throws.
    /// </para>
    /// <para>
    /// All storage is allocated up front in the constructor, so the number of items cannot change
    /// later. Adding more or fewer boxes than <c>count</c> is not reported by
    /// <see cref="Add(T, T, T, T)"/> itself but by <see cref="Build"/>.
    /// </para>
    /// </remarks>
    public class StaticAABB2DIndexBuilder<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly int _nodeSize;
        private readonly int _numItems;
        private readonly int[] _levelBounds;
        private readonly AABB<T>[] _boxes;
        private readonly int[] _indices;
        private int _pos;
        private bool _built;

        /// <summary>
        /// Creates a builder sized to fit exactly <paramref name="count"/> items, using the default
        /// node size of 16.
        /// </summary>
        /// <param name="count">
        /// Number of bounding boxes that will be added. Exactly this many
        /// <see cref="Add(T, T, T, T)"/> calls must follow before <see cref="Build"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is negative.
        /// </exception>
        public StaticAABB2DIndexBuilder(int count) : this(count, 16) { }

        /// <summary>
        /// Creates a builder sized to fit exactly <paramref name="count"/> items, using the given
        /// node size for the shape of the index tree.
        /// </summary>
        /// <param name="count">
        /// Number of bounding boxes that will be added. Exactly this many
        /// <see cref="Add(T, T, T, T)"/> calls must follow before <see cref="Build"/>.
        /// </param>
        /// <param name="nodeSize">
        /// Maximum number of boxes stored as children of a node in the index tree. Values below 2
        /// are raised to 2 and values above 65535 are lowered to 65535. The default of 16 used by
        /// <see cref="StaticAABB2DIndexBuilder{T}(int)"/> is optimal in most cases. When
        /// <paramref name="count"/> is zero the value is stored unclamped, matching upstream.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count"/> is negative. Upstream uses an unsigned count, so this case
        /// cannot arise there; without the guard the level bounds loop would not terminate.
        /// </exception>
        public StaticAABB2DIndexBuilder(int count, int nodeSize)
        {
            // Rust uses usize here, so a negative count is unrepresentable. Without this guard
            // Math.Ceiling(-1.0 / nodeSize) is 0, the level bounds loop never reaches 1 and the
            // constructor spins forever.
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            _numItems = count;
            if (_numItems == 0)
            {
                _nodeSize = nodeSize;
                _levelBounds = Array.Empty<int>();
                _boxes = Array.Empty<AABB<T>>();
                _indices = Array.Empty<int>();
                _pos = 0;
                return;
            }

            _nodeSize = Math.Clamp(nodeSize, 2, 65535);

            int n = _numItems;
            int levelBoundsLen = 1;
            while (true)
            {
                n = (int)Math.Ceiling((double)n / _nodeSize);
                levelBoundsLen++;
                if (n == 1) break;
            }

            n = _numItems;
            int numNodes = _numItems;
            var levelBoundsList = new List<int>(levelBoundsLen) { n };
            while (true)
            {
                n = (int)Math.Ceiling((double)n / _nodeSize);
                numNodes += n;
                levelBoundsList.Add(numNodes);
                if (n == 1) break;
            }

            _levelBounds = levelBoundsList.ToArray();
            _boxes = new AABB<T>[numNodes];
            _indices = new int[numNodes];
            for (int i = 0; i < numNodes; i++)
            {
                _indices[i] = i;
            }
            _pos = 0;
        }

        /// <summary>
        /// Adds one axis aligned bounding box with the extent points
        /// (<paramref name="minX"/>, <paramref name="minY"/>) and
        /// (<paramref name="maxX"/>, <paramref name="maxY"/>) to the index being built.
        /// </summary>
        /// <param name="minX">Lower x extent of the box.</param>
        /// <param name="minY">Lower y extent of the box.</param>
        /// <param name="maxX">Upper x extent of the box.</param>
        /// <param name="maxY">Upper y extent of the box.</param>
        /// <returns>This builder, so calls can be chained.</returns>
        /// <remarks>
        /// <para>
        /// For performance the sanity checks <c>minX &lt;= maxX</c> and <c>minY &lt;= maxY</c> are
        /// only debug asserted. Adding an invalid box leads to unspecified behaviour of the
        /// resulting index.
        /// </para>
        /// <para>
        /// Adding more boxes than the count given at construction does not throw here; the extra
        /// boxes are discarded and the mismatch is reported by <see cref="Build"/>.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StaticAABB2DIndexBuilder<T> Add(T minX, T minY, T maxX, T maxY)
        {
            if (_pos >= _numItems)
            {
                _pos++;
                return this;
            }
            Debug.Assert(minX <= maxX);
            Debug.Assert(minY <= maxY);

            _boxes[_pos] = new AABB<T>(minX, minY, maxX, maxY);
            _pos++;
            return this;
        }

        /// <summary>
        /// Builds the immutable <see cref="StaticAABB2DIndex{T}"/> from the boxes that were added.
        /// </summary>
        /// <returns>The packed Hilbert R-tree over the added boxes.</returns>
        /// <exception cref="InvalidOperationException">
        /// This builder has already been built, or the number of <see cref="Add(T, T, T, T)"/>
        /// calls does not equal the count given at construction.
        /// </exception>
        /// <remarks>
        /// Sorts the item boxes by their Hilbert value and then packs them bottom up into nodes of
        /// at most <c>nodeSize</c> children. The builder is consumed by this call, mirroring the
        /// Rust <c>build(mut self)</c> signature which takes the builder by value.
        /// </remarks>
        public StaticAABB2DIndex<T> Build()
        {
            // Rust's build(mut self) consumes the builder. Reporting the real reason beats the
            // misleading "added: 44, expected: 40" that a second call used to produce.
            if (_built)
            {
                throw new InvalidOperationException("this builder has already been built");
            }
            _built = true;

            if (_pos != _numItems)
            {
                throw new InvalidOperationException($"Added item count should equal static size given to builder (added: {_pos}, expected: {_numItems})");
            }

            if (_numItems == 0)
            {
                return new StaticAABB2DIndex<T>(_nodeSize, _numItems, _levelBounds, Array.Empty<AABB<T>>(), _indices);
            }

            // Calculate total bounds
            T minX = _boxes[0].MinX;
            T minY = _boxes[0].MinY;
            T maxX = _boxes[0].MaxX;
            T maxY = _boxes[0].MaxY;

            for (int i = 1; i < _numItems; i++)
            {
                minX = T.Min(minX, _boxes[i].MinX);
                minY = T.Min(minY, _boxes[i].MinY);
                maxX = T.Max(maxX, _boxes[i].MaxX);
                maxY = T.Max(maxY, _boxes[i].MaxY);
            }

            if (_numItems <= _nodeSize)
            {
                _indices[_pos] = 0;
                _boxes[_pos] = new AABB<T>(minX, minY, maxX, maxY);

                return new StaticAABB2DIndex<T>(_nodeSize, _numItems, _levelBounds, _boxes, _indices);
            }

            double width = double.CreateChecked(maxX - minX);
            double height = double.CreateChecked(maxY - minY);
            double extentMinX = double.CreateChecked(minX);
            double extentMinY = double.CreateChecked(minY);

            double hilbertMax = ushort.MaxValue;
            double scaledWidth = hilbertMax / width;
            double scaledHeight = hilbertMax / height;

            uint[] hilbertValues = new uint[_numItems];
            for (int i = 0; i < _numItems; i++)
            {
                double aabbMinX = double.CreateChecked(_boxes[i].MinX);
                double aabbMinY = double.CreateChecked(_boxes[i].MinY);
                double aabbMaxX = double.CreateChecked(_boxes[i].MaxX);
                double aabbMaxY = double.CreateChecked(_boxes[i].MaxY);

                ushort x = HilbertCoord(scaledWidth, aabbMinX, aabbMaxX, extentMinX);
                ushort y = HilbertCoord(scaledHeight, aabbMinY, aabbMaxY, extentMinY);
                hilbertValues[i] = HilbertXyToIndex(x, y);
            }

            // Sorting cannot improve node grouping when every box maps to the same Hilbert
            // value. Rare, but the check exits on the first compare in the common case.
            bool allSameHilbertValue = true;
            for (int i = 1; i < _numItems; i++)
            {
                if (hilbertValues[i] != hilbertValues[0])
                {
                    allSameHilbertValue = false;
                    break;
                }
            }

            if (!allSameHilbertValue)
            {
                RadixSort(hilbertValues, _boxes, _indices, 0, _numItems - 1, _nodeSize, 1u << 31);
            }

            int pos = 0;
            for (int levelIdx = 0; levelIdx < _levelBounds.Length - 1; levelIdx++)
            {
                int levelEnd = _levelBounds[levelIdx];
                while (pos < levelEnd)
                {
                    T nodeMinX = T.MaxValue;
                    T nodeMinY = T.MaxValue;
                    T nodeMaxX = T.MinValue;
                    T nodeMaxY = T.MinValue;
                    int nodeIndex = pos;

                    int j = 0;
                    while (j < _nodeSize && pos < levelEnd)
                    {
                        AABB<T> aabb = _boxes[pos];
                        pos++;
                        nodeMinX = T.Min(nodeMinX, aabb.MinX);
                        nodeMinY = T.Min(nodeMinY, aabb.MinY);
                        nodeMaxX = T.Max(nodeMaxX, aabb.MaxX);
                        nodeMaxY = T.Max(nodeMaxY, aabb.MaxY);
                        j++;
                    }

                    _indices[_pos] = nodeIndex;
                    _boxes[_pos] = new AABB<T>(nodeMinX, nodeMinY, nodeMaxX, nodeMaxY);
                    _pos++;
                }
            }

            return new StaticAABB2DIndex<T>(_nodeSize, _numItems, _levelBounds, _boxes, _indices);
        }

        private static ushort HilbertCoord(double scaledExtent, double aabbMin, double aabbMax, double extentMin)
        {
            double value = scaledExtent * (0.5 * (aabbMin + aabbMax) - extentMin);
            if (double.IsNaN(value)) return 0;
            if (value >= ushort.MaxValue) return ushort.MaxValue;
            if (value <= ushort.MinValue) return ushort.MinValue;
            return (ushort)value;
        }

        /// <summary>
        /// Maps a point in 2D grid space to its position along the Hilbert curve.
        /// </summary>
        /// <param name="x">Grid x coordinate in the range 0 to <see cref="ushort.MaxValue"/>.</param>
        /// <param name="y">Grid y coordinate in the range 0 to <see cref="ushort.MaxValue"/>.</param>
        /// <returns>
        /// The 1D Hilbert curve value <c>d</c> in the range 0 to <c>n^2 - 1</c> with
        /// <c>n = 2^16</c>.
        /// </returns>
        public static uint HilbertXyToIndex(ushort x, ushort y)
        {
            uint ux = x;
            uint uy = y;

            uint a1 = ux ^ uy;
            uint b1 = 0xFFFF ^ a1;
            uint c1 = 0xFFFF ^ (ux | uy);
            uint d1 = ux & (uy ^ 0xFFFF);

            uint a2 = a1 | (b1 >> 1);
            uint b2 = (a1 >> 1) ^ a1;
            uint c2 = ((c1 >> 1) ^ (b1 & (d1 >> 1))) ^ c1;
            uint d2 = ((a1 & (c1 >> 1)) ^ (d1 >> 1)) ^ d1;

            a1 = a2; b1 = b2; c1 = c2; d1 = d2;
            a2 = (a1 & (a1 >> 2)) ^ (b1 & (b1 >> 2));
            b2 = (a1 & (b1 >> 2)) ^ (b1 & ((a1 ^ b1) >> 2));
            c2 ^= (a1 & (c1 >> 2)) ^ (b1 & (d1 >> 2));
            d2 ^= (b1 & (c1 >> 2)) ^ ((a1 ^ b1) & (d1 >> 2));

            a1 = a2; b1 = b2; c1 = c2; d1 = d2;
            a2 = (a1 & (a1 >> 4)) ^ (b1 & (b1 >> 4));
            b2 = (a1 & (b1 >> 4)) ^ (b1 & ((a1 ^ b1) >> 4));
            c2 ^= (a1 & (c1 >> 4)) ^ (b1 & (d1 >> 4));
            d2 ^= (b1 & (c1 >> 4)) ^ ((a1 ^ b1) & (d1 >> 4));

            a1 = a2; b1 = b2; c1 = c2; d1 = d2;
            c2 ^= (a1 & (c1 >> 8)) ^ (b1 & (d1 >> 8));
            d2 ^= (b1 & (c1 >> 8)) ^ ((a1 ^ b1) & (d1 >> 8));

            a1 = c2 ^ (c2 >> 1);
            b1 = d2 ^ (d2 >> 1);

            uint i0 = ux ^ uy;
            uint i1 = b1 | (0xFFFF ^ (i0 | a1));

            i0 = (i0 | (i0 << 8)) & 0x00FF00FF;
            i0 = (i0 | (i0 << 4)) & 0x0F0F0F0F;
            i0 = (i0 | (i0 << 2)) & 0x33333333;
            i0 = (i0 | (i0 << 1)) & 0x55555555;

            i1 = (i1 | (i1 << 8)) & 0x00FF00FF;
            i1 = (i1 | (i1 << 4)) & 0x0F0F0F0F;
            i1 = (i1 | (i1 << 2)) & 0x33333333;
            i1 = (i1 | (i1 << 1)) & 0x55555555;

            return (i1 << 1) | i0;
        }

        /// <summary>
        /// MSB-first binary radix sort over the Hilbert values, carrying the boxes and indices
        /// along. Port of <c>radix_sort</c> in static_aabb2d_index 2.1.0.
        /// </summary>
        /// <remarks>
        /// A quicksort would produce a valid but different permutation of the items within a
        /// node, which propagates into the order in which segment pairs reach the intersection
        /// routines and from there into the last bits of the results.
        /// </remarks>
        private static void RadixSort(
            uint[] values,
            AABB<T>[] boxes,
            int[] indices,
            int left,
            int right,
            int nodeSize,
            uint bit)
        {
            Debug.Assert(left <= right);

            int emptyPartitions = 0;
            int split;

            while (true)
            {
                // A same-node range needs no ordering; at bit zero all remaining values are equal.
                if (left / nodeSize >= right / nodeSize || bit == 0)
                {
                    return;
                }

                int end = right + 1;
                int i = left;
                int j = end;
                while (i < j)
                {
                    while (i < j && (values[i] & bit) == 0)
                    {
                        i++;
                    }
                    while (i < j && (values[j - 1] & bit) != 0)
                    {
                        j--;
                    }
                    if (i == j)
                    {
                        break;
                    }
                    Swap(values, boxes, indices, i, j - 1);
                    i++;
                    j--;
                }

                bit >>= 1;
                if (i == left || i == end)
                {
                    emptyPartitions++;
                    // After two bits fail to split, one scan is cheaper than testing each
                    // shared bit.
                    if (emptyPartitions == 2)
                    {
                        uint first = values[left];
                        uint differingBits = 0;
                        for (int k = left + 1; k <= right; k++)
                        {
                            differingBits |= first ^ values[k];
                        }
                        if (differingBits == 0)
                        {
                            return;
                        }
                        bit = 1u << BitOperations.Log2(differingBits);
                        emptyPartitions = 0;
                    }
                    continue;
                }

                split = i;
                break;
            }

            RadixSort(values, boxes, indices, left, split - 1, nodeSize, bit);
            RadixSort(values, boxes, indices, split, right, nodeSize, bit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(uint[] values, AABB<T>[] boxes, int[] indices, int i, int j)
        {
            (values[i], values[j]) = (values[j], values[i]);
            (boxes[i], boxes[j]) = (boxes[j], boxes[i]);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
    }

    /// <summary>
    /// Static, fixed size spatial index over two dimensional axis aligned bounding boxes.
    /// </summary>
    /// <typeparam name="T">Floating point type used for the box coordinates.</typeparam>
    /// <remarks>
    /// <para>
    /// The index is a packed Hilbert R-tree: it is built once from a known item count via
    /// <see cref="StaticAABB2DIndexBuilder{T}"/> and is immutable afterwards. Boxes cannot be
    /// added, removed or moved; to change the contents a new index must be built. In exchange
    /// construction and querying are both fast and the whole tree lives in flat arrays.
    /// </para>
    /// <para>
    /// A bounding box is represented by its two extent points
    /// <c>(minX, minY)</c> and <c>(maxX, maxY)</c>. Index positions reported by the queries refer
    /// to the order in which boxes were handed to
    /// <see cref="StaticAABB2DIndexBuilder{T}.Add(T, T, T, T)"/>.
    /// </para>
    /// <para>
    /// The order of the items within a node comes from an MSB-first binary radix sort over the
    /// Hilbert values, matching static_aabb2d_index 2.1.0 exactly. A different but equally valid
    /// sort would permute the items inside a node, which changes the order in which segment pairs
    /// reach the intersection routines and therefore the last bits of the geometric results.
    /// </para>
    /// </remarks>
    public class StaticAABB2DIndex<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly int _nodeSize;
        private readonly int _numItems;
        private readonly int[] _levelBounds;
        private readonly AABB<T>[] _boxes;
        private readonly int[] _indices;

        internal StaticAABB2DIndex(int nodeSize, int numItems, int[] levelBounds, AABB<T>[] boxes, int[] indices)
        {
            _nodeSize = nodeSize;
            _numItems = numItems;
            _levelBounds = levelBounds;
            _boxes = boxes;
            _indices = indices;
        }

        /// <summary>
        /// Gets the total bounds of all items that were added to the index, or <see langword="null"/>
        /// if no items were added (<see cref="Count"/> is zero).
        /// </summary>
        public AABB<T>? Bounds => _boxes.Length == 0 ? null : _boxes[^1];

        /// <summary>
        /// Gets the number of items that were added to the index during construction.
        /// </summary>
        public int Count => _numItems;

        /// <summary>
        /// Gets the node size of the index, that is the maximum number of boxes stored as children
        /// of each node in the index tree.
        /// </summary>
        public int NodeSize => _nodeSize;

        /// <summary>
        /// Gets the level bounds of the index: the positions in <see cref="AllBoxes"/> at which the
        /// level of the index tree changes.
        /// </summary>
        public ReadOnlySpan<int> LevelBounds => _levelBounds;

        /// <summary>
        /// Gets every bounding box in the index, item boxes and node boxes alike.
        /// </summary>
        /// <remarks>
        /// The boxes are ordered from the bottom of the tree up, so positions 0 to
        /// <see cref="Count"/> hold the item boxes and everything after that holds the node boxes.
        /// Use <see cref="AllBoxIndices"/> to map a box back to the position it was added at or to
        /// find the start position of a node's children.
        /// </remarks>
        public ReadOnlySpan<AABB<T>> AllBoxes => _boxes;

        /// <summary>
        /// Maps a position in <see cref="AllBoxes"/> back to the index position the item was added
        /// at; for positions past <see cref="Count"/> it yields the <see cref="AllBoxes"/> start
        /// position of that node's children instead.
        /// </summary>
        public ReadOnlySpan<int> AllBoxIndices => _indices;

        /// <summary>
        /// Gets only the item bounding boxes that were added via
        /// <see cref="StaticAABB2DIndexBuilder{T}.Add(T, T, T, T)"/>, in index order.
        /// </summary>
        /// <remarks>
        /// The order is the internal Hilbert order, not the order the boxes were added in. Use
        /// <see cref="ItemIndices"/> to map a position back to the original add position.
        /// </remarks>
        public ReadOnlySpan<AABB<T>> ItemBoxes => _boxes.AsSpan(0, _numItems);

        /// <summary>
        /// Maps a position in <see cref="ItemBoxes"/> back to the index position the item was added
        /// at.
        /// </summary>
        public ReadOnlySpan<int> ItemIndices => _indices.AsSpan(0, _numItems);

        /// <summary>
        /// Queries the index and returns the index positions of all items whose bounding box
        /// overlaps the given query box.
        /// </summary>
        /// <param name="minX">Lower x extent of the query box.</param>
        /// <param name="minY">Lower y extent of the query box.</param>
        /// <param name="maxX">Upper x extent of the query box.</param>
        /// <param name="maxY">Upper y extent of the query box.</param>
        /// <returns>
        /// The index positions of the overlapping items, according to the order the boxes were
        /// handed to <see cref="StaticAABB2DIndexBuilder{T}.Add(T, T, T, T)"/>.
        /// </returns>
        public List<int> Query(T minX, T minY, T maxX, T maxY)
        {
            var results = new List<int>();
            var visitor = new DelegateQueryVisitor(i => { results.Add(i); return true; });
            VisitQuery(minX, minY, maxX, maxY, ref visitor);
            return results;
        }

        /// <summary>
        /// Same as <see cref="Query(T, T, T, T)"/> but yields the results lazily instead of
        /// collecting them into a list.
        /// </summary>
        /// <param name="minX">Lower x extent of the query box.</param>
        /// <param name="minY">Lower y extent of the query box.</param>
        /// <param name="maxX">Upper x extent of the query box.</param>
        /// <param name="maxY">Upper y extent of the query box.</param>
        /// <returns>
        /// A lazily evaluated sequence of the index positions of the overlapping items.
        /// </returns>
        public IEnumerable<int> QueryIter(T minX, T minY, T maxX, T maxY)
        {
            if (_numItems == 0) yield break;

            var stack = new List<int>(16);
            int nodeIndex = _boxes.Length - 1;
            int pos = nodeIndex;
            int level = _levelBounds.Length - 1;
            int end = Math.Min(nodeIndex + _nodeSize, _levelBounds[level]);

            while (true)
            {
                while (pos < end)
                {
                    int currentPos = pos;
                    pos++;

                    AABB<T> aabb = _boxes[currentPos];
                    if (!aabb.Overlaps(minX, minY, maxX, maxY)) continue;

                    int index = _indices[currentPos];
                    if (nodeIndex < _numItems)
                    {
                        yield return index;
                    }
                    else
                    {
                        stack.Add(index);
                        stack.Add(level - 1);
                    }
                }

                if (stack.Count > 1)
                {
                    level = stack[^1]; stack.RemoveAt(stack.Count - 1);
                    nodeIndex = stack[^1]; stack.RemoveAt(stack.Count - 1);
                    pos = nodeIndex;
                    end = Math.Min(nodeIndex + _nodeSize, _levelBounds[level]);
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Same as <see cref="Query(T, T, T, T)"/> but instead of collecting the results it calls
        /// <paramref name="visitor"/> for each overlapping item.
        /// </summary>
        /// <typeparam name="V">Concrete visitor type; a value type so the call can be inlined.</typeparam>
        /// <param name="minX">Lower x extent of the query box.</param>
        /// <param name="minY">Lower y extent of the query box.</param>
        /// <param name="maxX">Upper x extent of the query box.</param>
        /// <param name="maxY">Upper y extent of the query box.</param>
        /// <param name="visitor">
        /// Visitor invoked once per overlapping item. Returning <see langword="true"/> continues
        /// the query, returning <see langword="false"/> stops it. Passed by reference so state
        /// accumulated in it is visible to the caller afterwards.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the query ran to completion, <see langword="false"/> if the
        /// visitor stopped it early.
        /// </returns>
        public bool VisitQuery<V>(T minX, T minY, T maxX, T maxY, ref V visitor) where V : struct, IQueryVisitor
        {
            if (_numItems == 0) return true;
            var stack = new List<int>(16);
            return VisitQueryWithStackImpl(minX, minY, maxX, maxY, ref visitor, stack);
        }

        /// <summary>
        /// Same as <see cref="VisitQuery{V}(T, T, T, T, ref V)"/> but reuses an existing buffer for
        /// the traversal stack.
        /// </summary>
        /// <typeparam name="V">Concrete visitor type; a value type so the call can be inlined.</typeparam>
        /// <param name="minX">Lower x extent of the query box.</param>
        /// <param name="minY">Lower y extent of the query box.</param>
        /// <param name="maxX">Upper x extent of the query box.</param>
        /// <param name="maxY">Upper y extent of the query box.</param>
        /// <param name="visitor">
        /// Visitor invoked once per overlapping item. Returning <see langword="true"/> continues
        /// the query, returning <see langword="false"/> stops it.
        /// </param>
        /// <param name="stack">
        /// Scratch buffer for the tree traversal. Its contents are cleared before use, so any
        /// list may be passed; supplying the same list across many queries avoids repeated
        /// allocations and has no effect other than on performance.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the query ran to completion, <see langword="false"/> if the
        /// visitor stopped it early.
        /// </returns>
        public bool VisitQueryWithStack<V>(T minX, T minY, T maxX, T maxY, ref V visitor, List<int> stack) where V : struct, IQueryVisitor
        {
            if (_numItems == 0) return true;
            return VisitQueryWithStackImpl(minX, minY, maxX, maxY, ref visitor, stack);
        }

        /// <summary>
        /// Convenience overload of <see cref="VisitQuery{V}(T, T, T, T, ref V)"/> that takes a
        /// delegate instead of a struct visitor.
        /// </summary>
        /// <param name="minX">Lower x extent of the query box.</param>
        /// <param name="minY">Lower y extent of the query box.</param>
        /// <param name="maxX">Upper x extent of the query box.</param>
        /// <param name="maxY">Upper y extent of the query box.</param>
        /// <param name="visitor">
        /// Called with the index position of each overlapping item. Return <see langword="true"/>
        /// to continue the query, <see langword="false"/> to stop it.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the query ran to completion, <see langword="false"/> if the
        /// visitor stopped it early.
        /// </returns>
        public bool VisitQuery(T minX, T minY, T maxX, T maxY, Func<int, bool> visitor)
        {
            var v = new DelegateQueryVisitor(visitor);
            return VisitQuery(minX, minY, maxX, maxY, ref v);
        }

        /// <summary>
        /// Convenience overload of
        /// <see cref="VisitQueryWithStack{V}(T, T, T, T, ref V, List{int})"/> that takes a delegate
        /// instead of a struct visitor.
        /// </summary>
        /// <param name="minX">Lower x extent of the query box.</param>
        /// <param name="minY">Lower y extent of the query box.</param>
        /// <param name="maxX">Upper x extent of the query box.</param>
        /// <param name="maxY">Upper y extent of the query box.</param>
        /// <param name="visitor">
        /// Called with the index position of each overlapping item. Return <see langword="true"/>
        /// to continue the query, <see langword="false"/> to stop it.
        /// </param>
        /// <param name="stack">
        /// Scratch buffer for the tree traversal, cleared before use. Reusing one list across many
        /// queries avoids repeated allocations.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the query ran to completion, <see langword="false"/> if the
        /// visitor stopped it early.
        /// </returns>
        public bool VisitQueryWithStack(T minX, T minY, T maxX, T maxY, Func<int, bool> visitor, List<int> stack)
        {
            var v = new DelegateQueryVisitor(visitor);
            return VisitQueryWithStack(minX, minY, maxX, maxY, ref v, stack);
        }

        private bool VisitQueryWithStackImpl<V>(T minX, T minY, T maxX, T maxY, ref V visitor, List<int> stack) where V : struct, IQueryVisitor
        {
            int nodeIndex = _boxes.Length - 1;
            int level = _levelBounds.Length - 1;
            stack.Clear();

            while (true)
            {
                int end = Math.Min(nodeIndex + _nodeSize, _levelBounds[level]);

                for (int pos = nodeIndex; pos < end; pos++)
                {
                    AABB<T> aabb = _boxes[pos];
                    if (!aabb.Overlaps(minX, minY, maxX, maxY)) continue;

                    int index = _indices[pos];
                    if (nodeIndex < _numItems)
                    {
                        if (!visitor.Visit(index)) return false;
                    }
                    else
                    {
                        stack.Add(index);
                        stack.Add(level - 1);
                    }
                }

                if (stack.Count > 1)
                {
                    level = stack[^1]; stack.RemoveAt(stack.Count - 1);
                    nodeIndex = stack[^1]; stack.RemoveAt(stack.Count - 1);
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Visits all items in order of increasing euclidean distance to the point
        /// (<paramref name="x"/>, <paramref name="y"/>) until the visitor stops the search or every
        /// item has been visited.
        /// </summary>
        /// <typeparam name="V">Concrete visitor type; a value type so the call can be inlined.</typeparam>
        /// <param name="x">X coordinate of the query point.</param>
        /// <param name="y">Y coordinate of the query point.</param>
        /// <param name="visitor">
        /// Receives the index position and the squared euclidean distance of each item. Returning
        /// <see langword="true"/> continues the search, <see langword="false"/> stops it. Since the
        /// search only ends early on <see langword="false"/>, a visitor that always continues will
        /// see every item in the index.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if all items were visited, <see langword="false"/> if the visitor
        /// stopped the search early.
        /// </returns>
        /// <remarks>
        /// Distances are squared (<c>dx * dx + dy * dy</c>) and are zero when the query point lies
        /// inside an item's bounding box. Use
        /// <see cref="VisitNeighborsWithQueue{V}(T, T, ref V, PriorityQueue{NeighborsState, T})"/>
        /// to avoid reallocating the internal priority queue on repeated calls.
        /// </remarks>
        public bool VisitNeighbors<V>(T x, T y, ref V visitor) where V : struct, INeighborVisitor<T>
        {
            if (_numItems == 0) return true;
            var queue = new PriorityQueue<NeighborsState, T>(8);
            return VisitNeighborsWithQueueImpl(x, y, ref visitor, queue);
        }

        /// <summary>
        /// Same as <see cref="VisitNeighbors{V}(T, T, ref V)"/> but reuses an existing priority
        /// queue instead of allocating one.
        /// </summary>
        /// <typeparam name="V">Concrete visitor type; a value type so the call can be inlined.</typeparam>
        /// <param name="x">X coordinate of the query point.</param>
        /// <param name="y">Y coordinate of the query point.</param>
        /// <param name="visitor">
        /// Receives the index position and the squared euclidean distance of each item. Returning
        /// <see langword="true"/> continues the search, <see langword="false"/> stops it.
        /// </param>
        /// <param name="queue">
        /// Priority queue used for the traversal. Its contents are cleared before use, so any queue
        /// may be passed; reusing one across many searches avoids repeated allocations.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if all items were visited, <see langword="false"/> if the visitor
        /// stopped the search early.
        /// </returns>
        public bool VisitNeighborsWithQueue<V>(T x, T y, ref V visitor, PriorityQueue<NeighborsState, T> queue) where V : struct, INeighborVisitor<T>
        {
            if (_numItems == 0) return true;
            return VisitNeighborsWithQueueImpl(x, y, ref visitor, queue);
        }

        /// <summary>
        /// Entry of the priority queue used by the nearest neighbor search.
        /// </summary>
        /// <remarks>
        /// This type is public only so that a caller can create and reuse a queue for
        /// <see cref="VisitNeighborsWithQueue{V}(T, T, ref V, PriorityQueue{NeighborsState, T})"/>.
        /// Its contents are an implementation detail of the traversal.
        /// </remarks>
        public readonly struct NeighborsState
        {
            /// <summary>
            /// Index position of the item when <see cref="IsLeafNode"/> is <see langword="true"/>,
            /// otherwise the <see cref="AllBoxes"/> start position of the node's children.
            /// </summary>
            public readonly int Index;

            /// <summary>
            /// <see langword="true"/> if this entry refers to an item rather than to an inner node
            /// of the tree.
            /// </summary>
            public readonly bool IsLeafNode;

            /// <summary>
            /// Creates a queue entry.
            /// </summary>
            /// <param name="index">Item index position or child start position, see <see cref="Index"/>.</param>
            /// <param name="isLeafNode"><see langword="true"/> if the entry refers to an item.</param>
            public NeighborsState(int index, bool isLeafNode)
            {
                Index = index;
                IsLeafNode = isLeafNode;
            }
        }

        private bool VisitNeighborsWithQueueImpl<V>(T x, T y, ref V visitor, PriorityQueue<NeighborsState, T> queue) where V : struct, INeighborVisitor<T>
        {
            T AxisDist(T k, T min, T max)
            {
                if (k < min) return min - k;
                if (k > max) return k - max;
                return T.Zero;
            }

            int nodeIndex = _boxes.Length - 1;
            queue.Clear();

            while (true)
            {
                int upperIdx = Array.BinarySearch(_levelBounds, nodeIndex);
                if (upperIdx >= 0) upperIdx += 1;
                else upperIdx = ~upperIdx;

                int end = Math.Min(nodeIndex + _nodeSize, _levelBounds[upperIdx]);

                for (int pos = nodeIndex; pos < end; pos++)
                {
                    AABB<T> aabb = _boxes[pos];
                    T dx = AxisDist(x, aabb.MinX, aabb.MaxX);
                    T dy = AxisDist(y, aabb.MinY, aabb.MaxY);
                    T dist = dx * dx + dy * dy;
                    int index = _indices[pos];
                    bool isLeafNode = nodeIndex < _numItems;
                    queue.Enqueue(new NeighborsState(index, isLeafNode), dist);
                }

                bool continueSearch = false;
                while (queue.TryDequeue(out NeighborsState state, out T dist))
                {
                    if (state.IsLeafNode)
                    {
                        if (!visitor.Visit(state.Index, dist)) return false;
                    }
                    else
                    {
                        nodeIndex = state.Index;
                        continueSearch = true;
                        break;
                    }
                }

                if (!continueSearch)
                {
                    return true;
                }
            }
        }
    }
}
