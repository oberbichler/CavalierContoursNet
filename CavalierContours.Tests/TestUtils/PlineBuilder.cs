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
            => Build(vertexes, isClosed: true, userData: null);

        public static Polyline<double> Open(params (double X, double Y, double Bulge)[] vertexes)
            => Build(vertexes, isClosed: false, userData: null);

        /// <summary>Equivalent of the Rust <c>pline_closed_userdata!</c> macro.</summary>
        public static Polyline<double> ClosedWithUserData(
            ulong[] userData,
            params (double X, double Y, double Bulge)[] vertexes)
            => Build(vertexes, isClosed: true, userData);

        /// <summary>Equivalent of the Rust <c>pline_open_userdata!</c> macro.</summary>
        public static Polyline<double> OpenWithUserData(
            ulong[] userData,
            params (double X, double Y, double Bulge)[] vertexes)
            => Build(vertexes, isClosed: false, userData);

        private static Polyline<double> Build(
            IReadOnlyList<(double X, double Y, double Bulge)> vertexes,
            bool isClosed,
            ulong[]? userData)
        {
            var pline = new Polyline<double>(vertexes.Count, isClosed);
            foreach (var (x, y, bulge) in vertexes)
            {
                pline.Add(x, y, bulge);
            }
            if (userData is not null)
            {
                pline.SetUserDataValues(userData);
            }
            return pline;
        }
    }
}
