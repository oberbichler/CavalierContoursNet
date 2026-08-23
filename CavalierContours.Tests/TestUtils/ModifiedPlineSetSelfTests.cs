using System;
using System.Collections.Generic;
using CavalierContours.Polyline;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    public class ModifiedPlineSetSelfTests
    {
        private static Polyline<double> Square()
            => PlineBuilder.Closed((0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0), (0.0, 10.0, 0.0));

        private static Polyline<double> ArcShape()
            => PlineBuilder.Closed((0.0, 0.0, 0.5), (10.0, 0.0, 0.0), (10.0, 10.0, -0.3), (0.0, 10.0, 0.0));

        [Fact]
        public void CycleStartIndexForwardRotatesVertexes()
        {
            var cycled = ModifiedPlineSet.CycleStartIndexForward(Square(), 1);

            Assert.Equal(4, cycled.VertexCount);
            Assert.Equal(new PlineVertex<double>(10.0, 0.0, 0.0), cycled.Get(0));
            Assert.Equal(new PlineVertex<double>(10.0, 10.0, 0.0), cycled.Get(1));
            Assert.Equal(new PlineVertex<double>(0.0, 10.0, 0.0), cycled.Get(2));
            Assert.Equal(new PlineVertex<double>(0.0, 0.0, 0.0), cycled.Get(3));
        }

        [Fact]
        public void CyclingPreservesAreaAndLength()
        {
            var original = PlineProperties.FromPline(Square(), false);
            for (int i = 1; i < 4; i++)
            {
                var cycled = PlineProperties.FromPline(ModifiedPlineSet.CycleStartIndexForward(Square(), i), false);
                Assert.True(cycled.FuzzyEqEps(original, PlineProperties.PropCmpEps), $"cycle {i} changed properties");
            }
        }

        [Fact]
        public void CyclingPreservesArcGeometry()
        {
            var original = PlineProperties.FromPline(ArcShape(), false);
            for (int i = 1; i < 4; i++)
            {
                var cycled = PlineProperties.FromPline(ModifiedPlineSet.CycleStartIndexForward(ArcShape(), i), false);
                Assert.True(cycled.FuzzyEqEps(original, PlineProperties.PropCmpEps), $"cycle {i} changed arc properties");
            }
        }

        [Fact]
        public void InvertDirectionFlipsAreaSignButKeepsMagnitude()
        {
            var inverted = ModifiedPlineSet.Clone(ArcShape());
            inverted.InvertDirection();

            var original = PlineProperties.FromPline(ArcShape(), false);
            var flipped = PlineProperties.FromPline(inverted, false);

            Assert.Equal(-original.Area, flipped.Area, 10);
            Assert.Equal(original.PathLength, flipped.PathLength, 10);
            Assert.True(PlineProperties.AabbFuzzyEqEps(original.Extents, flipped.Extents, PlineProperties.PropCmpEps));
        }

        [Fact]
        public void CycleStartIndexForwardRejectsInvalidArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ModifiedPlineSet.CycleStartIndexForward(Square(), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ModifiedPlineSet.CycleStartIndexForward(Square(), 4));
            Assert.Throws<ArgumentException>(() => ModifiedPlineSet.CycleStartIndexForward(
                PlineBuilder.Open((0.0, 0.0, 0.0), (1.0, 0.0, 0.0)), 1));
        }

        [Fact]
        public void AcceptVisitsAllVariants()
        {
            var states = new List<ModifiedPlineState>();
            new ModifiedPlineSet(Square(), invertDirection: true, cycleIndexPositions: true)
                .Accept((_, state) => states.Add(state));

            // 1 identity + 1 inverted + 3 cycled + 3 inverted-and-cycled
            Assert.Equal(8, states.Count);
            Assert.Equal(new ModifiedPlineState(false, 0), states[0]);
            Assert.Equal(new ModifiedPlineState(true, 0), states[1]);
            Assert.Equal(8, new HashSet<ModifiedPlineState>(states).Count);
        }

        [Fact]
        public void AcceptHonoursTheFlags()
        {
            static int Count(bool invert, bool cycle)
            {
                int n = 0;
                new ModifiedPlineSet(Square(), invert, cycle).Accept((_, _) => n++);
                return n;
            }

            Assert.Equal(1, Count(false, false));
            Assert.Equal(2, Count(true, false));
            Assert.Equal(4, Count(false, true));
            Assert.Equal(8, Count(true, true));
        }

        [Fact]
        public void AcceptDoesNotCycleOpenPolylines()
        {
            var open = PlineBuilder.Open((0.0, 0.0, 0.0), (10.0, 0.0, 0.0), (10.0, 10.0, 0.0));

            int n = 0;
            new ModifiedPlineSet(open, invertDirection: true, cycleIndexPositions: true).Accept((_, _) => n++);

            Assert.Equal(2, n);
        }

        [Fact]
        public void EveryVariantIsGeometricallyEquivalent()
        {
            var expected = PlineProperties.FromPline(ArcShape(), false);

            new ModifiedPlineSet(ArcShape(), invertDirection: true, cycleIndexPositions: true)
                .Accept((modified, state) =>
                {
                    var actual = PlineProperties.FromPline(modified, state.InvertedDirection);
                    Assert.True(
                        actual.FuzzyEqEps(expected, PlineProperties.PropCmpEps),
                        $"variant differs from the input, state: {state}\n" +
                        $"result:   {actual}\nexpected: {expected}");
                });
        }

        [Fact]
        public void AcceptDoesNotMutateInput()
        {
            var input = Square();
            var before = PlineProperties.FromPline(input, false);

            new ModifiedPlineSet(input, invertDirection: true, cycleIndexPositions: true)
                .Accept((pline, _) => pline.Add(99.0, 99.0, 0.0));

            var after = PlineProperties.FromPline(input, false);
            Assert.True(before.FuzzyEqEps(after, PlineProperties.PropCmpEps));
        }

        [Fact]
        public void VariantsCarryUserDataForward()
        {
            var input = Square();
            input.SetUserDataValues(new ulong[] { 11, 22 });

            new ModifiedPlineSet(input, invertDirection: true, cycleIndexPositions: true)
                .Accept((modified, state) =>
                    Assert.True(
                        PlineProperties.UserDataSetsMatch(modified.UserDataValues, new ulong[] { 11, 22 }),
                        $"userdata lost, state: {state}"));
        }
    }
}
