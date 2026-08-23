using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream <c>mod test_same</c> (test_pline_boolean.rs lines 273-314).
    /// </summary>
    public partial class PlineBooleanTests
    {
        [Fact]
        public void OriginCircle()
        {
            RunSameBooleanTests(PlineBuilder.Closed(
                (-1.0, 0.0, 1.0),
                (1.0, 0.0, 1.0)));
        }

        [Fact]
        public void OriginCircle2()
        {
            RunSameBooleanTests(PlineBuilder.Closed(
                (0.0, -1.0, 1.0),
                (0.0, 1.0, 1.0)));
        }

        [Fact]
        public void Rectangle()
        {
            RunSameBooleanTests(PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (20.0, 0.0, 0.0),
                (20.0, 10.0, 0.0),
                (0.0, 10.0, 0.0)));
        }

        [Fact]
        public void Diamond()
        {
            RunSameBooleanTests(PlineBuilder.Closed(
                (-10.0, 0.0, 0.0),
                (0.0, 10.0, 0.0),
                (10.0, 0.0, 0.0),
                (0.0, -10.0, 0.0)));
        }

        [Fact]
        public void Case1()
        {
            RunSameBooleanTests(PlineBuilder.Closed(
                (27.804688, 1.0, 0.0),
                (28.46842055794889, 0.3429054695163245, 0.0),
                (32.34577133994935, 0.9269762697003898, 0.0),
                (32.38116957207762, 1.451312562563487, 0.0),
                (31.5, 1.0, -0.31783751349740424),
                (30.79289310940682, 1.5, 0.0),
                (29.20710689059337, 1.5, -0.31783754777018053),
                (28.49999981323106, 1.00000000000007, 0.0)));
        }

        [Fact]
        public void Case2()
        {
            RunSameBooleanTests(PlineBuilder.Closed(
                (27.804688, 1.0, 0.0),
                (27.804688, 0.75, 0.0),
                (32.195313, 0.75, 0.0),
                (32.195313, 1.0, 0.0),
                (31.5, 1.0, -0.3178375134974),
                (30.792893109407, 1.5, 0.0),
                (29.207106890593, 1.5, -0.31783754777018),
                (28.499999813231, 1.0000000000001, 0.0)));
        }

        [Fact]
        public void Case3()
        {
            RunSameBooleanTests(PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (120.0, 0.0, 0.0),
                (120.0, 40.0, 0.0),
                (0.0, 40.0, 0.0)));
        }
    }
}
