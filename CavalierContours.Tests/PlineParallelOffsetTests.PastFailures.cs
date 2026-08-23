using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Test cases that have failed or had issues in the past but are otherwise seemingly
    /// unremarkable. Port of upstream <c>mod test_past_failures</c>
    /// (test_pline_parallel_offset.rs lines 463-649), declared via <c>declare_offset_tests!</c>
    /// so handle_self_intersects is false.
    /// </summary>
    public partial class PlineParallelOffsetTests
    {
        [Fact]
        public void OpenPline1()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (8.25, 0.0, 0.0),
                    (8.25, 0.0625, -0.414214),
                    (8.5, 0.3125, 0.0)),
                0.25,
                new[]
                {
                    new PlineProperties(3, 0.0, 0.84789847066602, 7.9999999999999, 0.0, 8.5000001870958, 0.56250000000015, 4UL),
                },
                false);
        }

        [Fact]
        public void OpenPline2()
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
                30.0,
                new[]
                {
                    new PlineProperties(9, 0.0, 480.07132994083656, 119.08533878718923, 16.583123951777, 374.4158698891213, 158.00772717933913, 4UL),
                },
                false);
        }

        /// <summary>Failed when making changes to polyline slices.</summary>
        [Fact]
        public void OpenPline3()
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
                25.0,
                new[]
                {
                    new PlineProperties(9, 0.0, 535.7065850258826, 112.87974759922413, 0.0000000000000284217, 379.4158698891212, 167.5240988148737, 4UL),
                },
                false);
        }

        /// <summary>Failed when making changes to polyline slices.</summary>
        [Fact]
        public void OpenPline4()
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
                57.0,
                new[]
                {
                    new PlineProperties(8, 0.0, 174.88800664020044, 151.6690225512594, 51.22499389946279, 302.1362925265432, 89.80353002260095, 4UL),
                },
                false);
        }

        /// <summary>
        /// Triggered debug asserts due to epsilon values/comparing around repeat vertex positions
        /// arising when slices formed into polylines/stitched to polylines.
        /// </summary>
        [Fact]
        public void OpenPline5()
        {
            RunPlineOffsetTests(
                PlineBuilder.OpenWithUserData(
                    UserData4,
                    (151.529431796616, 2672.360415934566, 0.0),
                    (151.52944705175477, 2672.3604162683446, -0.0000000232537211708),
                    (151.52946162808874, 2672.3604165872725, -0.0024145466234173404),
                    (177.34188421196347, 2672.8004877792832, 0.0),
                    (177.34191528862172, 2672.8004881589986, 0.0),
                    (177.34193697590484, 2672.8004884239886, 0.0)),
                81.0,
                new[]
                {
                    new PlineProperties(2, 0.0, 26.59869314313687, 149.7575959931641, 2753.3410345904244, 176.35229795820428, 2753.7944426095614, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedPline1()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (100.0, 100.0, -0.5),
                    (80.0, 90.0, 0.374794619217547),
                    (100.0, 0.0, 1.0),
                    (225.0, 0.0, 1.0),
                    (320.0, 0.0, -0.5),
                    (280.0, 0.0, 0.5),
                    (390.0, 210.0, 0.0),
                    (280.0, 120.0, 0.5)),
                26.0,
                new[]
                {
                    new PlineProperties(12, 26880.50880023272, 879.9419421394236, 97.46410017370246, -36.5, 378.41586988912127, 165.65896506528978, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedPline2()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (112.41916161761486, 317.6090172318188, 0.374794619217547),
                    (283.91125822540016, 113.83906801254867, -1.0),
                    (320.0, 0.0, -0.5),
                    (416.19973184838693, -118.5880908230576, 0.5),
                    (390.0, 210.0, 0.0),
                    (280.0, 120.0, 0.5)),
                11.0,
                new[]
                {
                    new PlineProperties(4, 22967.88418361544, 725.8310555703592, 306.51750709258783, -88.76884688852556, 474.8986719957476, 196.35636086795802, 4UL),
                    new PlineProperties(2, 14876.690185910866, 512.884459926642, 123.52142671939367, 127.7080000599289, 273.04351973980494, 306.89237970059116, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedPline3()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (-225.0, 0.0, 0.0),
                    (280.0, 0.0, 0.0),
                    (390.0, 200.0, 0.0),
                    (310.0, 200.0, 1.0),
                    (270.0, 200.0, -1.0),
                    (280.0, 200.0, -1.0),
                    (150.0, 200.0, 0.0),
                    (-340.0, 200.0, 0.0)),
                16.0,
                new[]
                {
                    new PlineProperties(7, 89881.66357519358, 1621.6223053868894, -312.34356480790507, 16.0, 362.93966046317865, 192.8544998953781, 4UL),
                },
                false);
        }

        [Fact]
        public void ClosedPline4()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (100.0, 100.0, -0.5),
                    (80.0, 90.0, 0.374794619217547),
                    (210.0, 0.0, 0.0),
                    (230.0, 0.0, 1.0),
                    (320.0, 0.0, -0.5),
                    (280.0, 0.0, 0.5),
                    (390.0, 210.0, 0.0),
                    (280.0, 120.0, 0.5)),
                -9.0,
                new[]
                {
                    new PlineProperties(11, 53340.59364855598, 1008.1487200240091, 71.0, -54.00000000000001, 413.41586988912127, 219.0, 4UL),
                },
                false);
        }

        /// <summary>
        /// Had problems with intersect at very end of segment arising due to epsilon value
        /// mismatches for comparing if two positions are equal.
        /// </summary>
        [Fact]
        public void ClosedPline5()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (264.0, 189.60769515458668, -0.6866165717616879),
                    (237.0, 200.0, 0.9999999999999999),
                    (188.0, 200.0, -1.0),
                    (186.99999999999997, 200.0, 0.7720018726587661),
                    (141.1399906367063, 212.0, 0.0),
                    (-340.0, 212.0, 0.5767622536477675),
                    (-350.4028756366904, 194.01834650890305, 0.0),
                    (-235.4028756366904, -5.9816534910969885, 0.2684220435725749),
                    (-225.0, -12.0, 0.0),
                    (280.0, -12.0, 0.2735184224363523),
                    (290.5145909041198, -5.783024997265875, 0.0),
                    (400.5145909041198, 194.2169750027341, 0.5704523505626424),
                    (390.0, 212.0, 0.0),
                    (373.86000936329407, 212.0, 0.7720018726587679),
                    (328.0, 200.0, 0.22133565492006524),
                    (334.5, 186.03575995623103, -0.4396641198250874),
                    (306.1980067765089, 188.0, 0.0),
                    (310.0, 188.0, 0.41421356237309503),
                    (322.0, 200.0, 0.9999999999999999),
                    (258.0, 200.0, 0.26794919243112475)),
                -3.0,
                new[]
                {
                    new PlineProperties(18, 151176.94826984024, 1955.7049177723648, -355.0, -15.0, 405.00000000000006, 234.99999999999994, 4UL),
                },
                false);
        }

        /// <summary>Failed when making changes to polyline slices.</summary>
        [Fact]
        public void ClosedPline6()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (100.0, 100.0, -0.5),
                    (80.0, 90.0, 0.374794619217547),
                    (210.0, 0.0, 0.0),
                    (230.0, 0.0, 1.0),
                    (320.0, 0.0, -0.5),
                    (280.0, 0.0, 0.5),
                    (390.0, 210.0, 0.0),
                    (280.0, 120.0, 0.5)),
                25.0,
                new[]
                {
                    new PlineProperties(10, 21487.825530978065, 727.9542629450341, 112.87974759922413, 0.0000000000000284217, 379.4158698891212, 167.5240988148737, 4UL),
                },
                false);
        }

        /// <summary>
        /// Failed due to issues around construction of polyline slices, involves
        /// coincident/overlapping result after offset.
        /// </summary>
        [Fact]
        public void ClosedPline7()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (0.0, 0.0, 0.0),
                    (432.22004474869937, 0.0, 0.0),
                    (432.22004474869937, -620.7191231042452, 0.0),
                    (414.22004474869937, -620.7191231042452, 0.0),
                    (414.22004474869937, -18.0, 0.0),
                    (0.0, -18.0, 0.0)),
                -9.0,
                new[]
                {
                    new PlineProperties(5, -17.38274876480773, 2030.0155026470434, 9.0, -611.7191231042452, 423.22004474869937, -9.0, 4UL),
                },
                false);
        }

        /// <summary>
        /// Failed due to a bug introduced when making line-arc intersects "sticky" to line end
        /// points for consistency across segment intersects.
        /// </summary>
        [Fact]
        public void ClosedPline8()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (290.0, -4.0, 0.5),
                    (390.0, 210.0, 0.0),
                    (255.0, 23.0, 0.5)),
                26.0,
                new[]
                {
                    new PlineProperties(2, 3401.4557886082257, 338.29794704218466, 286.1826241465677, 21.774491471132933, 381.38235587092686, 152.78252663170932, 4UL),
                },
                false);
        }

        /// <summary>
        /// Triggered debug assert failures around slice creation due to line-arc intersects
        /// returning intersect points too far from segment.
        /// </summary>
        [Fact]
        public void ClosedPline9()
        {
            RunPlineOffsetTests(
                PlineBuilder.ClosedWithUserData(
                    UserData4,
                    (28.938897894888974, 10.959_303_862_638_93, 0.0000000000000000),
                    (28.886532906360166, 10.916_459_781_115_36, -0.394_310_318_761_913_2),
                    (26.979_612_699_068_42, 11.041_454_353_670_33, 0.49377669119506246),
                    (11.308203844176965, 9.458_380_715_736_016, -0.20600333237336405),
                    (9.895_116_435_209_998, 7.757_401_315_567_152, 0.330_443_922_787_453),
                    (20.844033287063855, 1.3912851556945007, -0.027916381806689476),
                    (21.000000000000057, 1.4000000000000001, 0.0000000000000000),
                    (23.000000000000000, 1.4000000000000001, -0.414_213_579_775_936),
                    (24.400_000_000_000_02, -8.318_380_800_647_987e-8, 0.887_752_370_928_299_7),
                    (30.512933651613217, -0.729_541_510_402_689_9, -0.439_446_060_182_688_7)),
                0.1,
                new[]
                {
                    new PlineProperties(9, 195.40133874861155, 61.346378550776606, 10.023725045710194, -3.0000000085491503, 30.399051687627463, 14.069231422671779, 4UL),
                },
                false);
        }

        /// <summary>
        /// Regression test for issue #77: offset should work correctly when the input has vertices
        /// that are close together (this input is the result of a previous offset operation).
        /// Before the fix, this failed with handle_self_intersects=true.
        /// https://github.com/jbuckmccready/cavalier_contours/issues/77
        /// </summary>
        [Fact]
        public void Issue77RepeatedOffset()
        {
            RunPlineOffsetTests(
                PlineBuilder.Closed(
                    (2.0, 11.0, -0.6681786379192991),
                    (2.7071067811865475, 9.292893218813452, 0.0),
                    (-0.2928932188134524, 6.292893218813452, -0.6681786379192989),
                    (-2.0, 7.0, 0.0),
                    (-2.0, 15.0, -0.6681786379192989),
                    (-0.2928932188134524, 15.707106781186548, 0.0),
                    (2.7071067811865475, 12.707106781186548, -0.6681786379192991)),
                1.0,
                new[]
                {
                    new PlineProperties(7, -64.3633792727984, 31.14604709099094, -3.0, 5.0, 4.0, 17.0),
                },
                false);
        }
    }
}
