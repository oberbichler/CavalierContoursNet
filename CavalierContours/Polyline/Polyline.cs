using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using CavalierContours.Core;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// Basic polyline data representation: a contiguous sequence of vertexes plus a flag telling
    /// whether the polyline is closed or open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A closed polyline has an implicit final segment from the last vertex back to the first one;
    /// an open polyline does not. The vertexes are never duplicated to express closure.
    /// </para>
    /// <para>
    /// This is the concrete type returned by the operations declared on
    /// <see cref="IPlineSource{T}"/> and <see cref="IPlineSourceMut{T}"/>; see
    /// <see cref="PlineSourceExtensions"/> for the operations available on it.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">Floating point type used for the vertex coordinates and bulges.</typeparam>
    public class Polyline<T> : IPlineSourceMut<T>
        where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
    {
        private readonly List<PlineVertex<T>> _vertexData;
        private bool _isClosed;
        private readonly List<ulong> _userdata = new();
        private readonly ReadOnlyCollection<ulong> _userdataView;

        /// <summary>Creates a new empty polyline with <see cref="IsClosed"/> set to false.</summary>
        public Polyline() : this(false) { }

        /// <summary>Creates a new empty polyline.</summary>
        /// <param name="isClosed">
        /// <see langword="true"/> to create a closed polyline, <see langword="false"/> for an open
        /// one.
        /// </param>
        public Polyline(bool isClosed)
        {
            _vertexData = new List<PlineVertex<T>>();
            _isClosed = isClosed;
            _userdataView = _userdata.AsReadOnly();
        }

        /// <summary>
        /// Creates a new empty polyline whose vertex storage is pre-allocated for
        /// <paramref name="capacity"/> vertexes.
        /// </summary>
        /// <param name="capacity">Number of vertexes to reserve storage for.</param>
        /// <param name="isClosed">
        /// <see langword="true"/> to create a closed polyline, <see langword="false"/> for an open
        /// one.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="capacity"/> is negative.
        /// </exception>
        public Polyline(int capacity, bool isClosed)
        {
            _vertexData = new List<PlineVertex<T>>(capacity);
            _isClosed = isClosed;
            _userdataView = _userdata.AsReadOnly();
        }

        /// <summary>Creates a new polyline from the vertexes given.</summary>
        /// <remarks>
        /// The vertexes are copied; no user data is carried over, because the source is only a
        /// vertex sequence.
        /// </remarks>
        /// <param name="vertexes">Vertexes to copy into the new polyline, in order.</param>
        /// <param name="isClosed">
        /// <see langword="true"/> to create a closed polyline, <see langword="false"/> for an open
        /// one.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="vertexes"/> is null.</exception>
        public Polyline(IEnumerable<PlineVertex<T>> vertexes, bool isClosed)
        {
            _vertexData = new List<PlineVertex<T>>(vertexes);
            _isClosed = isClosed;
            _userdataView = _userdata.AsReadOnly();
        }

        /// <summary>Total number of vertexes stored in this polyline.</summary>
        public int VertexCount => _vertexData.Count;

        /// <summary>
        /// Whether the polyline is closed (last vertex forms a segment with the first vertex) or
        /// open (no segment between last and first vertex).
        /// </summary>
        public bool IsClosed => _isClosed;

        /// <summary>Number of user data values stored with this polyline.</summary>
        public int UserDataCount => _userdata.Count;

        /// <summary>
        /// Read-only view over the user data. This is a live view over the backing list, not a
        /// snapshot: it reflects later mutations. Callers that need a stable copy must take one.
        /// </summary>
        public IReadOnlyList<ulong> UserDataValues => _userdataView;

        /// <summary>Gets the vertex at the given index position.</summary>
        /// <param name="index">Zero based vertex index.</param>
        /// <returns>The vertex at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of bounds.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> Get(int index) => _vertexData[index];

        /// <summary>Replaces the vertex at the given index position.</summary>
        /// <param name="index">Zero based vertex index.</param>
        /// <param name="vertex">New vertex data.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of bounds.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int index, PlineVertex<T> vertex) => _vertexData[index] = vertex;

        /// <summary>Inserts a new vertex at the given index position.</summary>
        /// <param name="index">
        /// Zero based index at which the vertex is inserted; may equal <see cref="VertexCount"/> to
        /// append.
        /// </param>
        /// <param name="vertex">Vertex to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is negative or greater than <see cref="VertexCount"/>.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertVertex(int index, PlineVertex<T> vertex) => _vertexData.Insert(index, vertex);

        /// <summary>Removes the vertex at the given index position and returns it.</summary>
        /// <param name="index">Zero based vertex index.</param>
        /// <returns>The vertex that was removed.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of bounds.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlineVertex<T> Remove(int index)
        {
            var vertex = _vertexData[index];
            _vertexData.RemoveAt(index);
            return vertex;
        }

        /// <summary>Appends a vertex to the end of the polyline.</summary>
        /// <param name="vertex">Vertex to append.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddVertex(PlineVertex<T> vertex) => _vertexData.Add(vertex);

        /// <summary>Sets whether the polyline is closed or open.</summary>
        /// <remarks>
        /// Changing this flag does not add or remove vertexes; it only changes whether the segment
        /// from the last vertex back to the first one exists.
        /// </remarks>
        /// <param name="isClosed">
        /// <see langword="true"/> for a closed polyline, <see langword="false"/> for an open one.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIsClosed(bool isClosed) => _isClosed = isClosed;

        /// <summary>Removes all vertexes from the polyline.</summary>
        /// <remarks>
        /// User data values and the <see cref="IsClosed"/> flag are left untouched, matching
        /// upstream.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _vertexData.Clear();

        /// <summary>Appends all vertexes from the sequence given to the end of the polyline.</summary>
        /// <param name="vertexes">Vertexes to append, in order.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vertexes"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExtendVertexes(IEnumerable<PlineVertex<T>> vertexes) => _vertexData.AddRange(vertexes);

        /// <summary>
        /// Clears all existing user data values and replaces them with the values provided.
        /// </summary>
        /// <remarks>
        /// The sequence is materialized before the existing values are cleared, so it is safe to
        /// pass <see cref="UserDataValues"/> of this same instance.
        /// </remarks>
        /// <param name="values">New user data values.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public void SetUserDataValues(IEnumerable<ulong> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            // Materialize first: 'values' may alias this instance's own backing list.
            var snapshot = new List<ulong>(values);
            _userdata.Clear();
            _userdata.AddRange(snapshot);
        }

        /// <summary>Appends user data values to the values already stored.</summary>
        /// <remarks>
        /// Unlike <see cref="SetUserDataValues"/> this does not snapshot the input first, so
        /// passing <see cref="UserDataValues"/> of this same instance is not supported.
        /// </remarks>
        /// <param name="values">User data values to append.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddUserDataValues(IEnumerable<ulong> values) => _userdata.AddRange(values);

        /// <summary>
        /// Removes the vertex at the given index position without returning it, see
        /// <see cref="Remove(int)"/>.
        /// </summary>
        /// <param name="index">Zero based vertex index.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of bounds.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index) => _vertexData.RemoveAt(index);

        /// <summary>Gets or sets the vertex at the given index position.</summary>
        /// <param name="index">Zero based vertex index.</param>
        /// <returns>The vertex at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> is out of bounds.
        /// </exception>
        public PlineVertex<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _vertexData[index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _vertexData[index] = value;
        }
    }
}
