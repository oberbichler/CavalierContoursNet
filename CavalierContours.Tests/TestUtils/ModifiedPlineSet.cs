using System;
using System.Globalization;
using System.Linq;
using CavalierContours.Polyline;

namespace CavalierContours.Tests.TestUtils
{
    /// <summary>
    /// Describes which transformation produced a modified polyline, used in assertion messages.
    /// </summary>
    public readonly record struct ModifiedPlineState(bool InvertedDirection, int CyclePosition)
    {
        public override string ToString() => string.Format(
            CultureInfo.InvariantCulture,
            "ModifiedPlineState {{ inverted_direction: {0}, cycle_position: {1} }}",
            InvertedDirection ? "true" : "false",
            CyclePosition);
    }

    /// <summary>
    /// Produces the affine-equivalent variants of a polyline that upstream uses to shake out
    /// index-wrapping and orientation bugs. Port of
    /// <c>tests/test_utils/pline_modifiers.rs</c>.
    /// </summary>
    public sealed class ModifiedPlineSet
    {
        private readonly Polyline<double> _input;
        private readonly bool _invertDirection;
        private readonly bool _cycleIndexPositions;

        public ModifiedPlineSet(Polyline<double> input, bool invertDirection, bool cycleIndexPositions)
        {
            ArgumentNullException.ThrowIfNull(input);
            _input = input;
            _invertDirection = invertDirection;
            _cycleIndexPositions = cycleIndexPositions;
        }

        /// <summary>
        /// Cycles all vertex index positions forward by <paramref name="n"/>: index 0 becomes 1,
        /// the last index becomes 0, and so on. Closed polylines only.
        /// </summary>
        public static Polyline<double> CycleStartIndexForward(Polyline<double> input, int n)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (n <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(n), n, "cycling forward by 0 just returns the same polyline");
            }
            if (n >= input.VertexCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(n), n, "cycling forward by more than the polyline length is unnecessary");
            }
            if (!input.IsClosed)
            {
                throw new ArgumentException(
                    "cycling vertex index positions not possible with open polyline", nameof(input));
            }

            int vc = input.VertexCount;
            var cycled = new Polyline<double>(vc, isClosed: true);
            for (int i = 0; i < vc; i++)
            {
                cycled.AddVertex(input.Get((i + n) % vc));
            }

            // Userdata is deliberately NOT carried over. Upstream builds the cycled polyline
            // with Polyline::from_iter, which sets `userdata: Vec::new()` (0.7.0 pline.rs:220).
            // Copying it here would be stricter than upstream and could turn a ported
            // expectation that declares a userdata subset into a spurious failure.
            return cycled;
        }

        /// <summary>
        /// Deep copy including userdata. Upstream relies on Rust's <c>Clone</c>; the visitors
        /// must never receive a reference to the caller's polyline.
        /// </summary>
        public static Polyline<double> Clone(Polyline<double> input)
        {
            ArgumentNullException.ThrowIfNull(input);
            var copy = new Polyline<double>(input.IterVertexes().ToList(), input.IsClosed);
            copy.SetUserDataValues(input.UserDataValues);
            return copy;
        }

        public void Accept(Action<Polyline<double>, ModifiedPlineState> visitor)
        {
            ArgumentNullException.ThrowIfNull(visitor);

            visitor(Clone(_input), new ModifiedPlineState(false, 0));

            if (_invertDirection)
            {
                var inverted = Clone(_input);
                inverted.InvertDirection();
                visitor(inverted, new ModifiedPlineState(true, 0));
            }

            if (_cycleIndexPositions && _input.IsClosed)
            {
                for (int i = 1; i < _input.VertexCount; i++)
                {
                    visitor(CycleStartIndexForward(_input, i), new ModifiedPlineState(false, i));
                }

                if (_invertDirection)
                {
                    var inverted = Clone(_input);
                    inverted.InvertDirection();
                    for (int i = 1; i < _input.VertexCount; i++)
                    {
                        visitor(CycleStartIndexForward(inverted, i), new ModifiedPlineState(true, i));
                    }
                }
            }
        }
    }
}
