using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Spatial
{
    public interface IQueryVisitor
    {
        bool Visit(int indexPos); // returns true to continue, false to break
    }

    public struct DelegateQueryVisitor : IQueryVisitor
    {
        private readonly Func<int, bool> _delegate;
        public DelegateQueryVisitor(Func<int, bool> del) => _delegate = del;
        public bool Visit(int indexPos) => _delegate(indexPos);
    }

    public interface INeighborVisitor<T> where T : struct, IFloatingPointIeee754<T>
    {
        bool Visit(int indexPos, T distSquared); // returns true to continue, false to break
    }

    public struct DelegateNeighborVisitor<T> : INeighborVisitor<T> where T : struct, IFloatingPointIeee754<T>
    {
        private readonly Func<int, T, bool> _delegate;
        public DelegateNeighborVisitor(Func<int, T, bool> del) => _delegate = del;
        public bool Visit(int indexPos, T distSquared) => _delegate(indexPos, distSquared);
    }

    public class StaticAABB2DIndexBuilder<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly int _nodeSize;
        private readonly int _numItems;
        private readonly int[] _levelBounds;
        private readonly AABB<T>[] _boxes;
        private readonly int[] _indices;
        private int _pos;

        public StaticAABB2DIndexBuilder(int count) : this(count, 16) { }

        public StaticAABB2DIndexBuilder(int count, int nodeSize)
        {
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

        public StaticAABB2DIndex<T> Build()
        {
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

            Sort(hilbertValues, _boxes, _indices, 0, _numItems - 1, _nodeSize);

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

        private static void Sort(
            uint[] values,
            AABB<T>[] boxes,
            int[] indices,
            int left,
            int right,
            int nodeSize)
        {
            Debug.Assert(left <= right);

            if (left / nodeSize >= right / nodeSize)
            {
                return;
            }

            int mid = (left + right) / 2;
            uint pivot = values[mid];
            int i = left - 1;
            int j = right + 1;

            while (true)
            {
                do
                {
                    i++;
                } while (values[i] < pivot);

                do
                {
                    j--;
                } while (values[j] > pivot);

                if (i >= j)
                {
                    break;
                }

                Swap(values, boxes, indices, i, j);
            }

            Sort(values, boxes, indices, left, j, nodeSize);
            Sort(values, boxes, indices, j + 1, right, nodeSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Swap(uint[] values, AABB<T>[] boxes, int[] indices, int i, int j)
        {
            (values[i], values[j]) = (values[j], values[i]);
            (boxes[i], boxes[j]) = (boxes[j], boxes[i]);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
    }

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

        public AABB<T>? Bounds => _boxes.Length == 0 ? null : _boxes[^1];
        public int Count => _numItems;
        public int NodeSize => _nodeSize;
        public ReadOnlySpan<int> LevelBounds => _levelBounds;
        public ReadOnlySpan<AABB<T>> AllBoxes => _boxes;
        public ReadOnlySpan<int> AllBoxIndices => _indices;
        public ReadOnlySpan<AABB<T>> ItemBoxes => _boxes.AsSpan(0, _numItems);
        public ReadOnlySpan<int> ItemIndices => _indices.AsSpan(0, _numItems);

        public List<int> Query(T minX, T minY, T maxX, T maxY)
        {
            var results = new List<int>();
            var visitor = new DelegateQueryVisitor(i => { results.Add(i); return true; });
            VisitQuery(minX, minY, maxX, maxY, ref visitor);
            return results;
        }

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

        public bool VisitQuery<V>(T minX, T minY, T maxX, T maxY, ref V visitor) where V : struct, IQueryVisitor
        {
            if (_numItems == 0) return true;
            var stack = new List<int>(16);
            return VisitQueryWithStackImpl(minX, minY, maxX, maxY, ref visitor, stack);
        }

        public bool VisitQueryWithStack<V>(T minX, T minY, T maxX, T maxY, ref V visitor, List<int> stack) where V : struct, IQueryVisitor
        {
            if (_numItems == 0) return true;
            return VisitQueryWithStackImpl(minX, minY, maxX, maxY, ref visitor, stack);
        }

        public bool VisitQuery(T minX, T minY, T maxX, T maxY, Func<int, bool> visitor)
        {
            var v = new DelegateQueryVisitor(visitor);
            return VisitQuery(minX, minY, maxX, maxY, ref v);
        }

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

        public bool VisitNeighbors<V>(T x, T y, ref V visitor) where V : struct, INeighborVisitor<T>
        {
            if (_numItems == 0) return true;
            var queue = new PriorityQueue<NeighborsState, T>(8);
            return VisitNeighborsWithQueueImpl(x, y, ref visitor, queue);
        }

        public bool VisitNeighborsWithQueue<V>(T x, T y, ref V visitor, PriorityQueue<NeighborsState, T> queue) where V : struct, INeighborVisitor<T>
        {
            if (_numItems == 0) return true;
            return VisitNeighborsWithQueueImpl(x, y, ref visitor, queue);
        }

        public readonly struct NeighborsState
        {
            public readonly int Index;
            public readonly bool IsLeafNode;

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
