using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Port of upstream <c>mod test_simple</c> (test_pline_boolean.rs lines 421-504).
    /// </summary>
    public partial class PlineBooleanTests
    {
        [Fact]
        public void RectangleSlicingCircle()
        {
            RunPlineBooleanTests(
                PlineBuilder.ClosedWithUserData(
                    [4],
                    (0.0, 1.0, 1.0),
                    (10.0, 1.0, 1.0)),
                PlineBuilder.ClosedWithUserData(
                    [117],
                    (3.0, -10.0, 0.0),
                    (6.0, -10.0, 0.0),
                    (6.0, 10.0, 0.0),
                    (3.0, 10.0, 0.0)),
                Case(BooleanOp.Or,
                    Set(Props(8, 109.15381629282, 52.324068506275, 0.0, -10.0, 10.0, 10.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Not,
                    Set(
                        Props(2, 29.336980664548, 23.492343031178, 6.0, -3.8989794855664, 10.0, 5.8989794855664, 4, 117),
                        Props(2, 19.816835628274, 20.757946197186, 0.0, -3.5825756949558, 3.0, 5.5825756949558, 4, 117)),
                    NoPlines),
                Case(BooleanOp.And,
                    Set(Props(4, 29.386000046923, 25.091858029623, 3.0, -4.0, 6.0, 6.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Xor,
                    Set(
                        Props(2, 29.336980664548, 23.492343031178, 6.0, -3.8989794855664, 10.0, 5.8989794855664, 4, 117),
                        Props(2, 19.816835628274, 20.757946197186, -8.8817841970013e-16, -3.5825756949558, 3.0, 5.5825756949558, 4, 117),
                        Props(4, -18.306999976538, 18.582818653767, 3.0, -10.0, 6.0, -3.5825756949558, 4, 117),
                        Props(4, -12.306999976538, 14.582818653767, 3.0, 5.5825756949558, 6.0, 10.0, 4, 117)),
                    NoPlines));
        }

        [Fact]
        public void RectangleOverHalfOfCircle()
        {
            RunPlineBooleanTests(
                PlineBuilder.ClosedWithUserData(
                    [4],
                    (-50.0, 0.0, 1.0),
                    (50.0, 0.0, 1.0)),
                PlineBuilder.ClosedWithUserData(
                    [117],
                    (-50.0, 0.0, 0.0),
                    (50.0, 0.0, 0.0),
                    (50.0, 50.0, 0.0),
                    (-50.0, 50.0, 0.0)),
                Case(BooleanOp.Or,
                    Set(Props(4, 8926.990816987241, 357.0796326794897, -50.0, -50.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.And,
                    Set(Props(2, 3926.9908169872415, 257.0796326794897, -50.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Not,
                    Set(Props(2, -3926.9908169872415, 257.0796326794897, -50.0, -50.0, 50.0, 0.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Xor,
                    Set(
                        Props(2, -3926.9908169872415, 257.0796326794897, -50.0, -50.0, 50.0, 0.0, 4, 117),
                        Props(3, 536.504591506379, 178.53981633974485, 0.0, 0.0, 50.0, 50.0, 4, 117),
                        Props(3, 536.504591506379, 178.53981633974485, -50.0, 0.0, 0.0, 50.0, 4, 117)),
                    NoPlines));
        }

        [Fact]
        public void RectangleInRectangleOneEdgeOverlap()
        {
            RunPlineBooleanTests(
                PlineBuilder.ClosedWithUserData(
                    [4],
                    (0.0, 0.0, 0.0),
                    (50.0, 0.0, 0.0),
                    (50.0, 50.0, 0.0),
                    (0.0, 50.0, 0.0)),
                PlineBuilder.ClosedWithUserData(
                    [117],
                    (10.0, 10.0, 0.0),
                    (50.0, 10.0, 0.0),
                    (50.0, 40.0, 0.0),
                    (10.0, 40.0, 0.0)),
                Case(BooleanOp.Or,
                    Set(Props(4, 2500.0, 200.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.And,
                    Set(Props(4, 1200.0, 140.0, 10.0, 10.0, 50.0, 40.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Not,
                    Set(Props(8, -1300.0, 280.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Xor,
                    Set(Props(8, -1300.0, 280.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines));
        }

        [Fact]
        public void RectangleInRectangleOneEdgeOverlapFlippedOrder()
        {
            RunPlineBooleanTests(
                PlineBuilder.ClosedWithUserData(
                    [4],
                    (10.0, 10.0, 0.0),
                    (50.0, 10.0, 0.0),
                    (50.0, 40.0, 0.0),
                    (10.0, 40.0, 0.0)),
                PlineBuilder.ClosedWithUserData(
                    [117],
                    (0.0, 0.0, 0.0),
                    (50.0, 0.0, 0.0),
                    (50.0, 50.0, 0.0),
                    (0.0, 50.0, 0.0)),
                Case(BooleanOp.Or,
                    Set(Props(4, 2500.0, 200.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.And,
                    Set(Props(4, 1200.0, 140.0, 10.0, 10.0, 50.0, 40.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Not,
                    NoPlines,
                    NoPlines),
                Case(BooleanOp.Xor,
                    Set(Props(8, 1300.0, 280.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines));
        }

        [Fact]
        public void RectangleInRectangleTwoEdgeOverlap()
        {
            RunPlineBooleanTests(
                PlineBuilder.ClosedWithUserData(
                    [4],
                    (0.0, 0.0, 0.0),
                    (50.0, 0.0, 0.0),
                    (50.0, 50.0, 0.0),
                    (0.0, 50.0, 0.0)),
                PlineBuilder.ClosedWithUserData(
                    [117],
                    (10.0, 10.0, 0.0),
                    (50.0, 10.0, 0.0),
                    (50.0, 50.0, 0.0),
                    (10.0, 50.0, 0.0)),
                Case(BooleanOp.Or,
                    Set(Props(4, 2500.0, 200.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.And,
                    Set(Props(4, 1600.0, 160.0, 10.0, 10.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Not,
                    Set(Props(6, -900.0, 200.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Xor,
                    Set(Props(6, -900.0, 200.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines));
        }

        [Fact]
        public void RectangleInRectangleTwoEdgeOverlapFlippedOrder()
        {
            RunPlineBooleanTests(
                PlineBuilder.ClosedWithUserData(
                    [4],
                    (10.0, 10.0, 0.0),
                    (50.0, 10.0, 0.0),
                    (50.0, 50.0, 0.0),
                    (10.0, 50.0, 0.0)),
                PlineBuilder.ClosedWithUserData(
                    [117],
                    (0.0, 0.0, 0.0),
                    (50.0, 0.0, 0.0),
                    (50.0, 50.0, 0.0),
                    (0.0, 50.0, 0.0)),
                Case(BooleanOp.Or,
                    Set(Props(4, 2500.0, 200.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.And,
                    Set(Props(4, 1600.0, 160.0, 10.0, 10.0, 50.0, 50.0, 4, 117)),
                    NoPlines),
                Case(BooleanOp.Not,
                    NoPlines,
                    NoPlines),
                Case(BooleanOp.Xor,
                    Set(Props(6, 900.0, 200.0, 0.0, 0.0, 50.0, 50.0, 4, 117)),
                    NoPlines));
        }
    }
}
