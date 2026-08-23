using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    public class PlineBuilderSelfTests
    {
        [Fact]
        public void ClosedBuildsAClosedPolylineWithTheGivenVertexes()
        {
            var circle = PlineBuilder.Closed((-1.0, 0.0, 1.0), (1.0, 0.0, 1.0));

            Assert.True(circle.IsClosed);
            Assert.Equal(2, circle.VertexCount);
            Assert.Equal(new PlineVertex<double>(-1.0, 0.0, 1.0), circle.Get(0));
            Assert.Equal(new PlineVertex<double>(1.0, 0.0, 1.0), circle.Get(1));
            Assert.Equal(System.Math.PI, circle.Area(), 12);
            Assert.Equal(0, circle.UserDataCount);
        }

        [Fact]
        public void OpenBuildsAnOpenPolyline()
        {
            var pline = PlineBuilder.Open((0.0, 0.0, 0.0), (1.0, 2.0, 0.5));

            Assert.False(pline.IsClosed);
            Assert.Equal(2, pline.VertexCount);
            Assert.Equal(new PlineVertex<double>(1.0, 2.0, 0.5), pline.Get(1));
        }

        [Fact]
        public void UserDataVariantsAttachTheValues()
        {
            var closed = PlineBuilder.ClosedWithUserData(
                new ulong[] { 4, 117 },
                (0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0));
            var open = PlineBuilder.OpenWithUserData(
                new ulong[] { 9 },
                (0.0, 0.0, 0.0), (10.0, 0.0, 0.0));

            Assert.True(closed.IsClosed);
            Assert.Equal(new ulong[] { 4, 117 }, closed.UserDataValues);

            Assert.False(open.IsClosed);
            Assert.Equal(new ulong[] { 9 }, open.UserDataValues);
        }

        [Fact]
        public void EmptyVertexListIsAllowed()
        {
            var pline = PlineBuilder.Closed();

            Assert.True(pline.IsClosed);
            Assert.Equal(0, pline.VertexCount);
        }
    }
}
