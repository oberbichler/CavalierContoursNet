using System;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Simple/basic test cases for parallel offset (e.g. circles and rectangles).
    /// Port of upstream <c>mod test_simple</c> (test_pline_parallel_offset.rs lines 109-179),
    /// declared via <c>declare_offset_tests!</c> so handle_self_intersects is false.
    /// </summary>
    public partial class PlineParallelOffsetTests
    {
        private static readonly ulong[] UserData4 = new ulong[] { 4UL };

        [Fact]
        public void EmptyReturnsEmpty()
        {
            RunPlineOffsetTests(
                new Polyline<double>(),
                5.0,
                Array.Empty<PlineProperties>(),
                false);
        }

        [Fact]
        public void CircleCollapsedIntoPoint()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(UserData4, (0.0, 0.0, 1.0), (2.0, 0.0, 1.0)),
                1.0,
                Array.Empty<PlineProperties>(),
                false);
        }

        [Fact]
        public void SquareCollapsedIntoPoint()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-1.0, -1.0, 0.0), (1.0, -1.0, 0.0), (1.0, 1.0, 0.0), (-1.0, 1.0, 0.0)),
                1.0,
                Array.Empty<PlineProperties>(),
                false);
        }

        [Fact]
        public void CircleCollapsed()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(UserData4, (0.0, 0.0, 1.0), (2.0, 0.0, 1.0)),
                2.0,
                Array.Empty<PlineProperties>(),
                false);
        }

        [Fact]
        public void SquareCollapsed()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-1.0, -1.0, 0.0), (1.0, -1.0, 0.0), (1.0, 1.0, 0.0), (-1.0, 1.0, 0.0)),
                2.0,
                Array.Empty<PlineProperties>(),
                false);
        }

        [Fact]
        public void ClosedRectangleInward()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0), (20.0, 0.0, 0.0), (20.0, 10.0, 0.0), (0.0, 10.0, 0.0)),
                2.0,
                new[]
                {
                    new PlineProperties(4, 96.0, 44.0, 2.0, 2.0, 18.0, 8.0, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedRectangleOutward()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0), (20.0, 0.0, 0.0), (20.0, 10.0, 0.0), (0.0, 10.0, 0.0)),
                -2.0,
                new[]
                {
                    new PlineProperties(8, 332.56637061436, 72.566370614359, -2.0, -2.0, 22.0, 12.0, 4UL),
                },
                false);
        }

        [Fact]
        public void OpenRectangleInward()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0), (20.0, 0.0, 0.0), (20.0, 10.0, 0.0), (0.0, 10.0, 0.0), (0.0, 0.0, 0.0)),
                2.0,
                new[]
                {
                    new PlineProperties(5, 0.0, 44.0, 2.0, 2.0, 18.0, 8.0, 4UL),
                },
                false);
        }

        [Fact]
        public void OpenRectangleOutward()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0), (20.0, 0.0, 0.0), (20.0, 10.0, 0.0), (0.0, 10.0, 0.0), (0.0, 0.0, 0.0)),
                -2.0,
                new[]
                {
                    new PlineProperties(8, 0.0, 69.424777960769, -2.0, -2.0, 22.0, 12.0, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedRectangleIntoOverlappingLine()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0), (20.0, 0.0, 0.0), (20.0, 10.0, 0.0), (0.0, 10.0, 0.0)),
                5.0,
                new[]
                {
                    new PlineProperties(2, 0.0, 20.0, 5.0, 5.0, 15.0, 5.0, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedDiamondOffsetInward()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-10.0, 0.0, 0.0), (0.0, 10.0, 0.0), (10.0, 0.0, 0.0), (0.0, -10.0, 0.0)),
                -5.0,
                new[]
                {
                    new PlineProperties(4, -17.157287525381, 16.568542494924, -2.9289321881345, -2.9289321881345, 2.9289321881345, 2.9289321881345, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedDiamondOffsetOutward()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-10.0, 0.0, 0.0), (0.0, 10.0, 0.0), (10.0, 0.0, 0.0), (0.0, -10.0, 0.0)),
                5.0,
                new[]
                {
                    new PlineProperties(8, -561.38252881436, 87.984469030822, -15.0, -15.0, 15.0, 15.0, 4UL),
                },
                false);
        }

        [Fact]
        public void OpenDiamondOffsetInward()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (-10.0, 0.0, 0.0), (0.0, 10.0, 0.0), (10.0, 0.0, 0.0), (0.0, -10.0, 0.0), (-10.0, 0.0, 0.0)),
                -5.0,
                new[]
                {
                    new PlineProperties(5, 0.0, 16.568542494924, -2.9289321881345, -2.9289321881345, 2.9289321881345, 2.9289321881345, 4UL),
                },
                false);
        }

        [Fact]
        public void OpenDiamondOffsetOutward()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (-10.0, 0.0, 0.0), (0.0, 10.0, 0.0), (10.0, 0.0, 0.0), (0.0, -10.0, 0.0), (-10.0, 0.0, 0.0)),
                5.0,
                new[]
                {
                    new PlineProperties(8, 0.0, 80.130487396847, -13.535533905933, -15.0, 15.0, 15.0, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedCircleOffsetInward()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(UserData4, (-5.0, 0.0, 1.0), (5.0, 0.0, 1.0)),
                3.0,
                new[]
                {
                    new PlineProperties(2, 12.566370614359, 12.566370614359, -2.0, -2.0, 2.0, 2.0, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedCircleOffsetOutward()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(UserData4, (-5.0, 0.0, 1.0), (5.0, 0.0, 1.0)),
                -3.0,
                new[]
                {
                    new PlineProperties(2, 201.06192982975, 50.265482457437, -8.0, -8.0, 8.0, 8.0, 4UL),
                },
                false);
        }
    }
}
