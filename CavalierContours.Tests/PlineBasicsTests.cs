using System;
using Xunit;
using CavalierContours.Core;
using CavalierContours.Polyline;

namespace CavalierContours.Tests
{
    public class PlineBasicsTests
    {
        private const double Eps = 1e-5;

        [Fact]
        public void TestIterVertexes()
        {
            var polyline = new Polyline<double>(true);
            Assert.Equal(0, polyline.VertexCount);

            polyline.Add(1.0, 2.0, 0.3);
            Assert.Equal(1, polyline.VertexCount);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), polyline.Get(0));

            polyline.Add(4.0, 5.0, 0.6);
            Assert.Equal(2, polyline.VertexCount);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), polyline.Get(1));
        }

        [Fact]
        public void TestIterSegments()
        {
            var polyline = new Polyline<double>();
            int count = 0;
            foreach (var seg in polyline.IterSegments()) count++;
            Assert.Equal(0, count);

            polyline.Add(1.0, 2.0, 0.3);
            count = 0;
            foreach (var seg in polyline.IterSegments()) count++;
            Assert.Equal(0, count);

            polyline.Add(4.0, 5.0, 0.6);
            var segments = new System.Collections.Generic.List<(PlineVertex<double> V1, PlineVertex<double> V2)>(polyline.IterSegments());
            Assert.Single(segments);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), segments[0].V1);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), segments[0].V2);

            polyline.SetIsClosed(true);
            segments = new System.Collections.Generic.List<(PlineVertex<double> V1, PlineVertex<double> V2)>(polyline.IterSegments());
            Assert.Equal(2, segments.Count);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), segments[0].V1);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), segments[0].V2);
            Assert.Equal(new PlineVertex<double>(4.0, 5.0, 0.6), segments[1].V1);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.3), segments[1].V2);
        }

        [Fact]
        public void TestAreaAndPathLength()
        {
            var circle = new Polyline<double>(true);
            circle.Add(0.0, 0.0, 1.0);
            circle.Add(2.0, 0.0, 1.0);
            Assert.True(circle.Area().FuzzyEq(Math.PI));
            Assert.True(circle.PathLength().FuzzyEq(2.0 * Math.PI));

            var rectangle = new Polyline<double>(true);
            rectangle.Add(0.0, 0.0, 0.0);
            rectangle.Add(3.0, 0.0, 0.0);
            rectangle.Add(3.0, 2.0, 0.0);
            rectangle.Add(0.0, 2.0, 0.0);
            Assert.True(rectangle.Area().FuzzyEq(6.0));
            Assert.True(rectangle.PathLength().FuzzyEq(10.0));
        }

        [Fact]
        public void TestRemoveRepeatAndRedundant()
        {
            var polyline = new Polyline<double>(true);
            polyline.Add(2.0, 2.0, 0.5);
            polyline.Add(2.0, 2.0, 1.0);
            polyline.Add(3.0, 3.0, 1.0);
            polyline.Add(3.0, 3.0, 0.5);

            var result = polyline.RemoveRepeatPos(Eps);
            Assert.NotNull(result);
            Assert.Equal(2, result!.VertexCount);
            Assert.True(result.IsClosed);
            Assert.True(result[0].FuzzyEq(new PlineVertex<double>(2.0, 2.0, 1.0)));
            Assert.True(result[1].FuzzyEq(new PlineVertex<double>(3.0, 3.0, 0.5)));

            // Redundant collinear vertexes on a straight line
            var linePline = new Polyline<double>(true);
            linePline.Add(2.0, 2.0, 0.0);
            linePline.Add(3.0, 3.0, 0.0);
            linePline.Add(3.0, 3.0, 0.0);
            linePline.Add(4.0, 4.0, 0.0);
            linePline.Add(2.0, 4.0, 0.0);

            var lineResult = linePline.RemoveRedundant(Eps);
            Assert.NotNull(lineResult);
            Assert.Equal(3, lineResult!.VertexCount);
            Assert.True(lineResult[0].FuzzyEq(new PlineVertex<double>(2.0, 2.0, 0.0)));
            Assert.True(lineResult[1].FuzzyEq(new PlineVertex<double>(4.0, 4.0, 0.0)));
            Assert.True(lineResult[2].FuzzyEq(new PlineVertex<double>(2.0, 4.0, 0.0)));
        }

        [Fact]
        public void TestRotateStart()
        {
            var polyline = new Polyline<double>(true);
            polyline.Add(0.0, 0.0, 0.0);
            polyline.Add(1.0, 0.0, 0.0);
            polyline.Add(1.0, 1.0, 0.0);
            polyline.Add(0.0, 1.0, 0.0);

            var rot = polyline.RotateStart(0, new Vector2<double>(0.5, 0.0), Eps);
            Assert.NotNull(rot);
            Assert.Equal(5, rot!.VertexCount);
            Assert.True(rot[0].FuzzyEq(new PlineVertex<double>(0.5, 0.0, 0.0)));
            Assert.True(rot[1].FuzzyEq(new PlineVertex<double>(1.0, 0.0, 0.0)));
            Assert.True(rot[2].FuzzyEq(new PlineVertex<double>(1.0, 1.0, 0.0)));
            Assert.True(rot[3].FuzzyEq(new PlineVertex<double>(0.0, 1.0, 0.0)));
            Assert.True(rot[4].FuzzyEq(new PlineVertex<double>(0.0, 0.0, 0.0)));
        }

        [Fact]
        public void TestFindPointAtPathLength()
        {
            var pline = new Polyline<double>(true);
            pline.Add(0.0, 0.0, 1.0);
            pline.Add(1.0, 0.0, -1.0);
            pline.Add(1.0, 1.0, 0.0);
            pline.Add(1.0, 2.0, 0.0);

            double plinePathLength = pline.PathLength();

            // 0 path length (start)
            var (success, index, pt, acc) = pline.FindPointAtPathLength(0.0);
            Assert.True(success);
            Assert.Equal(0, index);
            Assert.True(pt.FuzzyEq(new Vector2<double>(0.0, 0.0)));

            // Half first segment length
            double targetLength = PlineSeg.SegLength(pline[0], pline[1]) / 2.0;
            (success, index, pt, acc) = pline.FindPointAtPathLength(targetLength);
            Assert.True(success);
            Assert.Equal(0, index);
            Assert.True(pt.FuzzyEq(new Vector2<double>(0.5, -0.5)));
        }
    }
}
