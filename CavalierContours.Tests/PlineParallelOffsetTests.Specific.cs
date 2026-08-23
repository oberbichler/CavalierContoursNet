using System;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Specific test cases for parallel offset that trigger edge case scenarios or specific code
    /// paths. Port of upstream <c>mod test_specific</c> (test_pline_parallel_offset.rs lines
    /// 182-460).
    /// </summary>
    public partial class PlineParallelOffsetTests
    {
        // ---------------------------------------------------------------------------------
        // declare_offset_tests! -> handle_self_intersects = false
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Offset arc just past line, in this case float epsilon values can cause failures.
        /// </summary>
        [Fact]
        public void Case1()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (27.804688, 1.0, 0.0),
                    (28.46842055794889, 0.3429054695163245, 0.0),
                    (32.34577133994935, 0.9269762697003898, 0.0),
                    (32.38116957207762, 1.451312562563487, 0.0),
                    (31.5, 1.0, -0.31783751349740424),
                    (30.79289310940682, 1.5, 0.0),
                    (29.20710689059337, 1.5, -0.31783754777018053),
                    (28.49999981323106, 1.00000000000007, 0.0)),
                0.1,
                new[]
                {
                    new PlineProperties(4, 0.094833810726263, 1.8213211761499, 31.533345690439,
                        0.90572346564886, 32.26949555256, 1.2817628453883, 4UL),
                    new PlineProperties(6, 1.7197931450343, 7.5140262005179, 28.047835685678,
                        0.44926177903859, 31.495431966272, 1.4, 4UL),
                },
                false);
        }

        /// <summary>
        /// First vertex position is on top of intersect with second segment (leading to some edge
        /// cases around the join between the last vertex and first vertex).
        /// </summary>
        [Fact]
        public void Case2()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (27.804688, 1.0, 0.0),
                    (27.804688, 0.75, 0.0),
                    (32.195313, 0.75, 0.0),
                    (32.195313, 1.0, 0.0),
                    (31.5, 1.0, -0.3178375134974),
                    (30.792893109407, 1.5, 0.0),
                    (29.207106890593, 1.5, -0.31783754777018),
                    (28.499999813231, 1.0000000000001, 0.0)),
                0.25,
                new[]
                {
                    new PlineProperties(4, 0.36247092523069, 3.593999211522, 29.16143806012, 1.0,
                        30.838561906052, 1.25, 4UL),
                },
                false);
        }

        /// <summary>
        /// Collapsed rectangle with raw offset polyline having no self intersects.
        /// </summary>
        [Fact]
        public void Case3()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0), (120.0, 0.0, 0.0), (120.0, 40.0, 0.0), (0.0, 40.0, 0.0)),
                30.0,
                Array.Empty<PlineProperties>(),
                false);
        }

        /// <summary>
        /// Three consecutive raw off segments intersect at the same point.
        /// </summary>
        [Fact]
        public void Case4()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (30.123_475_382_979_79, -17.0, 0.0),
                    (42.0, -17.0, 0.0),
                    (42.0, 17.0, 0.0),
                    (30.123475382979798, 17.00000, -0.093_311_550_024_413_19),
                    (30.5, 15.00000, 0.00000),
                    (30.5, -15.00000, -0.093_311_550_024_413_41)),
                -2.0,
                new[]
                {
                    new PlineProperties(9, 0.0, 99.224754131592, 28.12347538298, -19.0, 44.0, 19.0, 4UL),
                },
                false);
        }

        /// <summary>
        /// Tests clipping circle at start of polyline works correctly (with collapsed arc at
        /// start).
        /// </summary>
        [Fact]
        public void Case5()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (100.0, 100.0, -0.5),
                    (80.0, 90.0, 0.374794619217547),
                    (210.0, 0.0, 0.0),
                    (230.0, 0.0, 1.0),
                    (320.0, 0.0, -0.5),
                    (280.0, 0.0, 0.5),
                    (390.0, 210.0, 0.0),
                    (280.0, 120.0, 0.5)),
                -30.0,
                new[]
                {
                    new PlineProperties(7, 0.0, 916.7498699472794, 50.000000000000014, -74.99999999999997, 434.41586988912127, 240.0, 4UL),
                },
                false);
        }

        /// <summary>
        /// Tests line to line join where one of the lines is a collapsed arc and there is no
        /// intersection between them (they should be connected with an arc).
        /// </summary>
        [Fact]
        public void Case6()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (100.0, 100.0, -0.5),
                    (80.0, 90.0, 0.374794619217547),
                    (210.0, 0.0, 0.0),
                    (230.0, 0.0, 1.0),
                    (320.0, 0.0, -0.5),
                    (280.0, 0.0, 0.5),
                    (390.0, 210.0, 0.0),
                    (280.0, 120.0, 0.5)),
                45.0,
                new[]
                {
                    new PlineProperties(9, 0.0, 354.5924544050689, 137.36151283418917, 37.416573867739416, 357.2096858656279, 125.02881860280142, 4UL),
                },
                false);
        }

        /// <summary>
        /// Tests line to line join where one of the lines is a collapsed arc and there is a false
        /// intersect between them (they should be connected with an arc).
        /// </summary>
        [Fact]
        public void Case7()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (347.88382287598745, 269.85890289007887, -0.5),
                    (80.0, 90.0, 0.374794619217547),
                    (204.65318559134363, 55.01294696311311, 0.0),
                    (179.35722417454295, -56.42578188285236, 1.0),
                    (270.7403323676961, -93.94095261477841, -0.5),
                    (346.1511941991571, 157.81558178838168, 0.5),
                    (390.0, 210.0, 0.0),
                    (495.7348032988456, 68.8739763777561, 0.5)),
                47.0,
                new[]
                {
                    new PlineProperties(4, 0.0, 226.5117782356207, 394.0401535993119, 97.05525803008165, 533.3488347822203, 267.40547394194897, 4UL),
                },
                false);
        }

        /// <summary>
        /// Almost collapsed adjacent arcs with true intersects.
        /// </summary>
        [Fact]
        public void Case8()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (30.0, 0.0, 1.0),
                    (30.0, 150.0, 0.0),
                    (-380.0, 0.0, 0.0),
                    (30.0, -150.0, 1.0)),
                71.0,
                new[]
                {
                    new PlineProperties(3, 31.563080748331117, 36.43002218023972, 17.851377192815367, 69.95291962376376, 34.00000003096393, 75.82916586272847, 4UL),
                    new PlineProperties(3, 7211.747093261731, 504.5601794261032, -173.3532697788056, -61.27715478753268, -5.862380026216215, 61.27715478753261, 4UL),
                    new PlineProperties(3, 31.56308032687207, 36.43002208996665, 17.851377192815107, -75.82916586272874, 34.000000000000675, -69.95291962376365, 4UL),
                },
                false);
        }

        /// <summary>
        /// Almost collapsed adjacent arcs with false intersects.
        /// </summary>
        [Fact]
        public void Case9()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (30.0, 0.0, 1.0),
                    (30.0, 150.0, 0.0),
                    (-380.0, 0.0, 0.0),
                    (30.0, -150.0, 1.0)),
                73.0,
                new[]
                {
                    new PlineProperties(3, 6273.618943028112, 440.30207980349326, -167.53223512468745, -54.49913379476977, -18.567936085649954, 54.499133794769804, 4UL),
                },
                false);
        }

        /// <summary>
        /// Collapsed adjacent arcs.
        /// </summary>
        [Fact]
        public void Case10()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (30.0, 0.0, 1.0),
                    (30.0, 150.0, 0.0),
                    (-380.0, 0.0, 0.0),
                    (30.0, -150.0, 1.0)),
                77.0,
                new[]
                {
                    new PlineProperties(3, 4682.865221417136, 359.74976552142584, -155.89016581645112, -45.203002912175684, -32.335291189837534, 45.203002912175705, 4UL),
                },
                false);
        }

        /// <summary>
        /// Sequences of segments aligned along axis.
        /// </summary>
        [Fact]
        public void Case11()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-225.0, 0.0, 0.0),
                    (-200.0, 0.0, 0.0),
                    (-175.0, 0.0, 1.0),
                    (-150.0, 0.0, 1.0),
                    (-125.0, 0.0, 1.0),
                    (-100.0, 0.0, 0.0),
                    (-75.0, 0.0, -1.0),
                    (-50.0, 0.0, -1.0),
                    (-25.0, 0.0, -1.0),
                    (0.0, 0.0, 0.0),
                    (25.0, 0.0, 1.0),
                    (50.0, 0.0, 0.0),
                    (75.0, 0.0, 1.0),
                    (100.0, 0.0, 0.0),
                    (125.0, 0.0, 1.0),
                    (150.0, 0.0, 1.0),
                    (165.0, 0.0, 1.0),
                    (190.0, 0.0, 0.0),
                    (215.0, 0.0, 1.0),
                    (230.0, 0.0, 1.0),
                    (255.0, 0.0, 1.0),
                    (270.0, 0.0, 0.0),
                    (280.0, 0.0, 0.0),
                    (390.0, 200.0, 0.0),
                    (365.0, 200.0, 1.0),
                    (340.0, 200.0, 1.0),
                    (352.5, 200.0, -1.0),
                    (290.0, 200.0, 0.0),
                    (310.0, 200.0, 1.0),
                    (270.0, 200.0, -1.0),
                    (280.0, 200.0, -1.0),
                    (225.0, 200.0, 1.0),
                    (200.0, 200.0, -1.0),
                    (175.0, 200.0, 1.0),
                    (150.0, 200.0, 0.0),
                    (-340.0, 200.0, 0.0)),
                -9.0,
                new[]
                {
                    new PlineProperties(44, 141959.84850931115, 2052.5428168464014, -348.99999999999994, -21.5, 398.99999999999994, 229.0, 4UL),
                },
                false);
        }

        /// <summary>
        /// Sequences of segments aligned along axis with some collapsed arcs.
        /// </summary>
        [Fact]
        public void Case12()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-225.0, 0.0, 0.0),
                    (-200.0, 0.0, 0.0),
                    (-175.0, 0.0, 1.0),
                    (-150.0, 0.0, 1.0),
                    (-125.0, 0.0, 1.0),
                    (-100.0, 0.0, 0.0),
                    (-75.0, 0.0, -1.0),
                    (-50.0, 0.0, -1.0),
                    (-25.0, 0.0, -1.0),
                    (0.0, 0.0, 0.0),
                    (25.0, 0.0, 1.0),
                    (50.0, 0.0, 0.0),
                    (75.0, 0.0, 1.0),
                    (100.0, 0.0, 0.0),
                    (125.0, 0.0, 1.0),
                    (150.0, 0.0, 1.0),
                    (165.0, 0.0, 1.0),
                    (190.0, 0.0, 0.0),
                    (215.0, 0.0, 1.0),
                    (230.0, 0.0, 1.0),
                    (255.0, 0.0, 1.0),
                    (270.0, 0.0, 0.0),
                    (280.0, 0.0, 0.0),
                    (390.0, 200.0, 0.0),
                    (365.0, 200.0, 1.0),
                    (340.0, 200.0, 1.0),
                    (352.5, 200.0, -1.0),
                    (290.0, 200.0, 0.0),
                    (310.0, 200.0, 1.0),
                    (270.0, 200.0, -1.0),
                    (280.0, 200.0, -1.0),
                    (225.0, 200.0, 1.0),
                    (200.0, 200.0, -1.0),
                    (175.0, 200.0, 1.0),
                    (150.0, 200.0, 0.0),
                    (-340.0, 200.0, 0.0)),
                9.0,
                new[]
                {
                    new PlineProperties(45, 105309.44963383305, 1837.9627621817642, -324.4432552044466, -3.5, 374.77855901053806, 203.5, 4UL),
                    new PlineProperties(4, 17.514629264722736, 24.09798450969452, 285.0, 208.2192186706253, 296.32455532033674, 211.00000000000003, 4UL),
                },
                false);
        }

        /// <summary>
        /// Involves near parallel lines with intersect ending up at the end of a segment (failed
        /// previously due to skipping all global self intersects at pline segment end points).
        /// </summary>
        [Fact]
        public void Case13()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (274.2654113251365, -33.83458301699362, 0.0),
                    (272.8148939219459, -33.40645153702632, 0.0),
                    (270.5612637345483, -32.77332971826808, 0.0),
                    (254.8141988521534, -28.965635958672898, -0.004278242823226474),
                    (231.7006747719357, -21.716714720129538, 0.0),
                    (230.37047477193764, -21.12631472013047, -0.012056833164683494),
                    (267.72224120666004, -39.430804834601496, -0.007322055738970315),
                    (271.8159625814055, -35.8506489176749, 0.0)),
                0.8,
                new[]
                {
                    new PlineProperties(8, 75.74000463672292, 65.09412644187906, 242.84368242831727, -38.465496032789176, 272.5928914363709, -26.104129186572788, 4UL),
                },
                false);
        }

        /// <summary>
        /// Starting with a collapsed loop offsetting negative.
        /// </summary>
        [Fact]
        public void Case14()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (1.0, 0.0, 0.0),
                    (-1.0, 0.0, 0.0)),
                1.0,
                new[]
                {
                    new PlineProperties(4, -7.141592653589793, 10.283185307179586, -2.0, -1.0, 2.0, 1.0, 4UL),
                },
                false);
        }

        /// <summary>
        /// Starting with a collapsed loop offsetting positive.
        /// </summary>
        [Fact]
        public void Case15()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (1.0, 0.0, 0.0),
                    (-1.0, 0.0, 0.0)),
                -1.0,
                new[]
                {
                    new PlineProperties(4, 7.141592653589793, 10.283185307179586, -2.0, -1.0, 2.0, 1.0, 4UL),
                },
                false);
        }

        /// <summary>
        /// Raw offset polyline has many segments which intersect near a point, including two
        /// segments overlapping.
        /// </summary>
        [Fact]
        public void Case16()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (134.242345653389, -52.5319708744162, 0.0),
                    (133.495570653389, -53.1545458744162, 0.0),
                    (132.757683153389, -53.8411333744163, 0.0),
                    (132.026208153389, -54.6092833744162, 0.0),
                    (131.298783153389, -55.4762083744163, 0.0),
                    (130.572820653389, -56.4591208744163, 0.0),
                    (129.846070653389, -57.5755708744162, 0.0),
                    (124.578933153389, -67.0887958744163, 0.0),
                    (128.979483153389, -96.1860208744162, 0.0),
                    (165.171183153389, -77.3810833744163, 0.0),
                    (148.907620653389, 34.4037541255838, 0.0)),
                17.3,
                new[]
                {
                    new PlineProperties(8, 5.0181294125859495, 11.036602381320794, 143.0997883790256, -69.35328673171023, 146.28062481696807, -65.28735172305409, 4UL),
                },
                false);
        }

        /// <summary>
        /// Same as case 16 but with slightly different offset.
        /// </summary>
        [Fact]
        public void Case17()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (134.242345653389, -52.5319708744162, 0.0),
                    (133.495570653389, -53.1545458744162, 0.0),
                    (132.757683153389, -53.8411333744163, 0.0),
                    (132.026208153389, -54.6092833744162, 0.0),
                    (131.298783153389, -55.4762083744163, 0.0),
                    (130.572820653389, -56.4591208744163, 0.0),
                    (129.846070653389, -57.5755708744162, 0.0),
                    (124.578933153389, -67.0887958744163, 0.0),
                    (128.979483153389, -96.1860208744162, 0.0),
                    (165.171183153389, -77.3810833744163, 0.0),
                    (148.907620653389, 34.4037541255838, 0.0)),
                17.4,
                new[]
                {
                    new PlineProperties(8, 3.9751779587732017, 9.823062094528733, 143.34784890970013, -69.11170309917232, 146.17143083814483, -65.48750771700713, 4UL),
                },
                false);
        }

        // ---------------------------------------------------------------------------------
        // declare_self_intersecting_offset_tests! -> handle_self_intersects = true
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Tests clipping circle at start and end of polyline works correctly (with self intersect
        /// between first and last segment).
        /// </summary>
        [Fact]
        public void SelfIntersectingCase1()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (305.8082007608764, 149.26270215110728, -0.5),
                    (80.0, 90.0, 0.374794619217547),
                    (210.0, 0.0, 0.0),
                    (230.0, 0.0, 1.0),
                    (320.0, 0.0, -0.5),
                    (280.0, 0.0, 0.5),
                    (390.0, 210.0, 0.0),
                    (280.0, 120.0, 0.5)),
                -30.0,
                new[]
                {
                    new PlineProperties(3, 0.0, 24.810068463598633, 261.00286629228214, 143.21871897609964, 278.0250516267822, 160.58068007088974, 4UL),
                    new PlineProperties(8, 0.0, 1047.5088824641757, 50.00000000000001, -74.99999999999997, 434.41586988912127, 240.0, 4UL),
                },
                true);
        }

        /// <summary>
        /// Self intersecting adjacent arcs.
        /// </summary>
        [Fact]
        public void SelfIntersectingCase2()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-54.126705892111374, -9.012072327640396, 1.0),
                    (0.0, 200.0, 0.0),
                    (-200.0, 0.0, 0.0),
                    (0.0, -200.0, 1.0)),
                -9.0,
                new[]
                {
                    new PlineProperties(7, 72784.07553221736, 1139.217753852123, -209.0, -208.99999999999997, 89.89004749763792, 209.0, 4UL),
                    new PlineProperties(4, 0.0, 137.47770415252796, -63.126705892111374, -21.459436607513837, -0.0036782264819059662, 3.748798488343695, 4UL),
                },
                true);
        }
    }
}
