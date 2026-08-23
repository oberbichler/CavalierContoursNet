using System;
using System.Numerics;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    /// <summary>
    /// Containment test between two closed polylines.
    /// </summary>
    public static class PlineContains
    {
        /// <summary>
        /// Determines how two closed polylines are positioned relative to one another: whether one
        /// contains the other, whether they cross, or whether they are apart.
        /// </summary>
        /// <typeparam name="T">Floating point type used for the coordinates.</typeparam>
        /// <param name="pline1">
        /// The first polyline, the potential container. Must be closed and have at least two
        /// vertexes.
        /// </param>
        /// <param name="pline2">
        /// The second polyline, the potential containee. Must be closed and have at least two
        /// vertexes.
        /// </param>
        /// <param name="options">
        /// <see cref="PlineContainsOptions{T}.Pline1AabbIndex"/> supplies a prebuilt spatial index
        /// for <paramref name="pline1"/> and is computed internally when <see langword="null"/>;
        /// <see cref="PlineContainsOptions{T}.PosEqualEps"/> is the epsilon for position equality.
        /// </param>
        /// <returns>
        /// One of five outcomes:
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <see cref="PlineContainsResult.InvalidInput"/> when either polyline is open or has fewer
        /// than two vertexes. This is returned instead of throwing, so degenerate input must be
        /// checked for by inspecting the result.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <see cref="PlineContainsResult.Intersected"/> when the two polylines meet anywhere. This
        /// is tested first and wins over any containment: even if one polyline is almost entirely
        /// inside the other, a single touching or overlapping point makes the answer
        /// <c>Intersected</c>. Overlapping segments count as an intersection.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <see cref="PlineContainsResult.Pline2InsidePline1"/> when there are no intersects and
        /// <paramref name="pline2"/> lies inside <paramref name="pline1"/>.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <see cref="PlineContainsResult.Pline1InsidePline2"/> when there are no intersects and
        /// <paramref name="pline1"/> lies inside <paramref name="pline2"/>.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <see cref="PlineContainsResult.Disjoint"/> when there are no intersects and neither
        /// polyline is inside the other.
        /// </description>
        /// </item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Containment is decided by a winding number test on the first vertex of the candidate
        /// polyline, which is only meaningful because the intersect test already ruled out crossing
        /// boundaries. Self intersecting inputs are not rejected and may give unexpected results;
        /// screen them beforehand if that is a possibility.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="pline1"/> or <paramref name="pline2"/> or <paramref name="options"/> is null.</exception>
        public static PlineContainsResult PolylineContains<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            PlineContainsOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
            ArgumentNullException.ThrowIfNull(pline1);
            ArgumentNullException.ThrowIfNull(pline2);
            ArgumentNullException.ThrowIfNull(options);

            if (pline1.VertexCount < 2
                || !pline1.IsClosed
                || pline2.VertexCount < 2
                || !pline2.IsClosed)
            {
                return PlineContainsResult.InvalidInput;
            }

            T posEqualEps = options.PosEqualEps;
            StaticAABB2DIndex<T> pline1AabbIndex;
            if (options.Pline1AabbIndex != null)
            {
                pline1AabbIndex = options.Pline1AabbIndex;
            }
            else
            {
                pline1AabbIndex = pline1.CreateApproxAabbIndex();
            }

            bool PointInPline1(Vector2<T> point) => pline1.WindingNumber(point) != 0;
            bool PointInPline2(Vector2<T> point) => pline2.WindingNumber(point) != 0;

            bool IsPline1InPline2() => PointInPline2(pline1.Get(0).Pos());
            bool IsPline2InPline1() => PointInPline1(pline2.Get(0).Pos());

            var findOptions = new FindIntersectsOptions<T>
            {
                Pline1AabbIndex = pline1AabbIndex,
                PosEqualEps = posEqualEps
            };

            if (PlineIntersects.ScanForIntersect(pline1, pline2, findOptions))
            {
                return PlineContainsResult.Intersected;
            }
            else if (IsPline2InPline1())
            {
                return PlineContainsResult.Pline2InsidePline1;
            }
            else if (IsPline1InPline2())
            {
                return PlineContainsResult.Pline1InsidePline2;
            }
            else
            {
                return PlineContainsResult.Disjoint;
            }
        }
    }
}
