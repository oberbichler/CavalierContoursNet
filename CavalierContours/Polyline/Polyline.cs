using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    public class Polyline<T> : IPlineSourceMut<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly List<PlineVertex<T>> _vertexData;
        private bool _isClosed;
        private readonly List<ulong> _userdata;

        public Polyline() : this(false) { }

        public Polyline(bool isClosed)
        {
            _vertexData = new List<PlineVertex<T>>();
            _isClosed = isClosed;
            _userdata = new List<ulong>();
        }

        public Polyline(int capacity, bool isClosed)
        {
            _vertexData = new List<PlineVertex<T>>(capacity);
            _isClosed = isClosed;
            _userdata = new List<ulong>();
        }

        public Polyline(IEnumerable<PlineVertex<T>> vertexes, bool isClosed)
        {
            _vertexData = new List<PlineVertex<T>>(vertexes);
            _isClosed = isClosed;
            _userdata = new List<ulong>();
        }

        public int VertexCount => _vertexData.Count;
        public bool IsClosed => _isClosed;
        public int UserDataCount => _userdata.Count;
        public IEnumerable<ulong> UserDataValues => _userdata;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> Get(int index) => _vertexData[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, PlineVertex<T> vertex) => _vertexData[index] = vertex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertVertex(int index, PlineVertex<T> vertex) => _vertexData.Insert(index, vertex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> Remove(int index)
        {
            var vertex = _vertexData[index];
            _vertexData.RemoveAt(index);
            return vertex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(PlineVertex<T> vertex) => _vertexData.Add(vertex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIsClosed(bool isClosed) => _isClosed = isClosed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _vertexData.Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExtendVertexes(IEnumerable<PlineVertex<T>> vertexes) => _vertexData.AddRange(vertexes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetUserDataValues(IEnumerable<ulong> values)
        {
            _userdata.Clear();
            _userdata.AddRange(values);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUserDataValues(IEnumerable<ulong> values) => _userdata.AddRange(values);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index) => _vertexData.RemoveAt(index);

        public PlineVertex<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _vertexData[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _vertexData[index] = value;
        }
    }
}
