using System;
using System.Numerics;
using CavalierContours.Core;
using CavalierContours.Spatial;

namespace CavalierContours.Polyline
{
    public static class PlineContains
    {
        public static PlineContainsResult PolylineContains<T>(
            IPlineSource<T> pline1,
            IPlineSource<T> pline2,
            PlineContainsOptions<T> options)
            where T : struct, IFloatingPointIeee754<T>, IMinMaxValue<T>
        {
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
