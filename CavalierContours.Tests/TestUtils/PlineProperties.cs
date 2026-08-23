using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CavalierContours.Core;
using CavalierContours.Polyline;

namespace CavalierContours.Tests.TestUtils
{
    /// <summary>
    /// Fuzzy comparable fingerprint of a polyline. Port of the upstream
    /// <c>tests/test_utils/pline_test_properties.rs</c>.
    /// </summary>
    public sealed class PlineProperties
    {
        /// <summary>Positions equal epsilon.</summary>
        public const double PosEqEps = 1e-5;

        /// <summary>Property comparer epsilon.</summary>
        public const double PropCmpEps = 1e-4;

        /// <summary>Epsilon used with RemoveRedundant for consistent property comparison.</summary>
        public const double RemoveRedundantEps = 1e-4;

        public int VertexCount { get; }
        public double Area { get; }
        public double PathLength { get; }
        public AABB<double> Extents { get; }
        public IReadOnlyList<ulong> UserData { get; }

        public PlineProperties(
            int vertexCount,
            double area,
            double pathLength,
            double minX,
            double minY,
            double maxX,
            double maxY,
            params ulong[] userData)
        {
            VertexCount = vertexCount;
            Area = area;
            PathLength = pathLength;
            Extents = new AABB<double>(minX, minY, maxX, maxY);
            UserData = userData ?? Array.Empty<ulong>();
        }

        public static PlineProperties FromPline(Polyline<double> pline, bool invertArea)
        {
            ArgumentNullException.ThrowIfNull(pline);

            // Remove redundant vertexes so vertex counts are comparable.
            var reduced = pline.RemoveRedundant(RemoveRedundantEps) ?? pline;

            double area = reduced.Area();
            if (invertArea)
            {
                area = -area;
            }

            var extents = reduced.Extents()
                ?? throw new InvalidOperationException("polyline has no extents");

            return new PlineProperties(
                reduced.VertexCount,
                area,
                reduced.PathLength(),
                extents.MinX,
                extents.MinY,
                extents.MaxX,
                extents.MaxY,
                reduced.UserDataValues.ToArray());
        }

        public bool FuzzyEqEps(PlineProperties other, double eps)
        {
            ArgumentNullException.ThrowIfNull(other);
            return VertexCount == other.VertexCount
                && Area.FuzzyEq(other.Area, eps)
                && PathLength.FuzzyEq(other.PathLength, eps)
                && AabbFuzzyEqEps(Extents, other.Extents, eps)
                && UserDataSetsMatch(UserData, other.UserData);
        }

        /// <summary>
        /// Same as <see cref="FuzzyEqEps"/> but compares the absolute area, used where the
        /// expected orientation is not part of the assertion.
        /// </summary>
        public bool FuzzyEqEpsAbsArea(PlineProperties other, double eps)
        {
            ArgumentNullException.ThrowIfNull(other);
            return VertexCount == other.VertexCount
                && Math.Abs(Area).FuzzyEq(Math.Abs(other.Area), eps)
                && PathLength.FuzzyEq(other.PathLength, eps)
                && AabbFuzzyEqEps(Extents, other.Extents, eps)
                && UserDataSetsMatch(UserData, other.UserData);
        }

        public static bool AabbFuzzyEqEps(AABB<double> a, AABB<double> b, double eps)
        {
            return a.MinX.FuzzyEq(b.MinX, eps)
                && a.MinY.FuzzyEq(b.MinY, eps)
                && a.MaxX.FuzzyEq(b.MaxX, eps)
                && a.MaxY.FuzzyEq(b.MaxY, eps);
        }

        /// <summary>
        /// Upstream semantics: every expected datum must be present in the actual set.
        /// Extra values in <paramref name="actual"/> are allowed.
        /// </summary>
        public static bool UserDataSetsMatch(IReadOnlyList<ulong> actual, IReadOnlyList<ulong> expected)
        {
            foreach (var datum in expected)
            {
                if (!actual.Contains(datum))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString()
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.Append(c, $"{{ vc: {VertexCount}, area: {Area:G17}, len: {PathLength:G17}, ");
            sb.Append(c, $"extents: [{Extents.MinX:G17}, {Extents.MinY:G17}, {Extents.MaxX:G17}, {Extents.MaxY:G17}]");
            if (UserData.Count > 0)
            {
                sb.Append(c, $", userdata: [{string.Join(", ", UserData)}]");
            }
            sb.Append(" }");
            return sb.ToString();
        }

        public static List<PlineProperties> CreatePropertySet(
            IEnumerable<Polyline<double>> polylines,
            bool invertArea)
        {
            return polylines.Select(p => FromPline(p, invertArea)).ToList();
        }

        /// <summary>
        /// Multiset comparison: each expected entry must consume exactly one distinct result
        /// entry.
        /// </summary>
        /// <remarks>
        /// This uses the corrected upstream 0.9.0 semantics rather than the 0.7.0 version. The
        /// old version required <c>match_count == 1</c> per expected entry and therefore failed
        /// whenever two result polylines had identical properties.
        /// </remarks>
        public static bool PropertySetsMatch(
            IReadOnlyList<PlineProperties> resultSet,
            IReadOnlyList<PlineProperties> expectedSet)
            => SetsMatch(resultSet, expectedSet, (e, r) => e.FuzzyEqEps(r, PropCmpEps));

        public static bool PropertySetsMatchAbsArea(
            IReadOnlyList<PlineProperties> resultSet,
            IReadOnlyList<PlineProperties> expectedSet)
            => SetsMatch(resultSet, expectedSet, (e, r) => e.FuzzyEqEpsAbsArea(r, PropCmpEps));

        private static bool SetsMatch(
            IReadOnlyList<PlineProperties> resultSet,
            IReadOnlyList<PlineProperties> expectedSet,
            Func<PlineProperties, PlineProperties, bool> comparer)
        {
            if (resultSet.Count != expectedSet.Count)
            {
                return false;
            }

            var consumed = new bool[resultSet.Count];
            foreach (var expected in expectedSet)
            {
                int index = -1;
                for (int i = 0; i < resultSet.Count; i++)
                {
                    if (!consumed[i] && comparer(expected, resultSet[i]))
                    {
                        index = i;
                        break;
                    }
                }
                if (index < 0)
                {
                    return false;
                }
                consumed[index] = true;
            }
            return true;
        }

        public static string Render(IReadOnlyList<PlineProperties> set)
            => "[" + string.Join(",\n  ", set.Select(p => p.ToString())) + "]";

        /// <summary>
        /// Asserts <see cref="PropertySetsMatch"/> and renders both sets on failure. Upstream
        /// prints them via <c>eprintln!</c>; use this instead of a bare Assert.True so the
        /// diagnostic can never be forgotten.
        /// </summary>
        public static void AssertSetsMatch(
            IReadOnlyList<PlineProperties> resultSet,
            IReadOnlyList<PlineProperties> expectedSet,
            string context)
        {
            if (!PropertySetsMatch(resultSet, expectedSet))
            {
                throw new Xunit.Sdk.XunitException(Describe("property sets do not match", resultSet, expectedSet, context));
            }
        }

        /// <summary>
        /// Asserts <see cref="PropertySetsMatchAbsArea"/> and renders both sets on failure.
        /// </summary>
        public static void AssertSetsMatchAbsArea(
            IReadOnlyList<PlineProperties> resultSet,
            IReadOnlyList<PlineProperties> expectedSet,
            string context)
        {
            if (!PropertySetsMatchAbsArea(resultSet, expectedSet))
            {
                throw new Xunit.Sdk.XunitException(Describe("property sets do not match (abs area)", resultSet, expectedSet, context));
            }
        }

        private static string Describe(
            string headline,
            IReadOnlyList<PlineProperties> resultSet,
            IReadOnlyList<PlineProperties> expectedSet,
            string context)
        {
            return $"{headline}\n  context:  {context}\n  result:   {Render(resultSet)}\n  expected: {Render(expectedSet)}";
        }
    }
}
