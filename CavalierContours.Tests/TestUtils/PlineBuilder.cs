using System.Collections.Generic;
using CavalierContours.Polyline;

namespace CavalierContours.Tests.TestUtils
{
    /// <summary>
    /// C# equivalents of the Rust <c>pline_closed!</c> / <c>pline_open!</c> macros used
    /// throughout the upstream test suite.
    /// </summary>
    public static class PlineBuilder
    {
        public static Polyline<double> Closed(params (double X, double Y, double Bulge)[] vertexes)
            => Build(vertexes, isClosed: true);

        public static Polyline<double> Open(params (double X, double Y, double Bulge)[] vertexes)
            => Build(vertexes, isClosed: false);

        private static Polyline<double> Build(
            IReadOnlyList<(double X, double Y, double Bulge)> vertexes,
            bool isClosed)
        {
            var pline = new Polyline<double>(vertexes.Count, isClosed);
            foreach (var (x, y, bulge) in vertexes)
            {
                pline.Add(x, y, bulge);
            }
            return pline;
        }
    }
}
