using System.Collections.Generic;
using CavalierContours.Polyline;
using CavalierContours.Shape;
using CavalierContours.Tests.TestUtils;
using Xunit;

namespace CavalierContours.Tests
{
    /// <summary>
    /// Pins that the collections handed out by the result containers cannot be mutated or
    /// swapped out by callers.
    /// </summary>
    public class ResultCollectionsReadOnlyTests
    {
        private static BooleanResult<Polyline<double>, double> IntersectingRectangles()
        {
            var pline1 = PlineBuilder.Closed(
                (0.0, 0.0, 0.0),
                (10.0, 0.0, 0.0),
                (10.0, 10.0, 0.0),
                (0.0, 10.0, 0.0));

            var pline2 = PlineBuilder.Closed(
                (5.0, 5.0, 0.0),
                (15.0, 5.0, 0.0),
                (15.0, 15.0, 0.0),
                (5.0, 15.0, 0.0));

            return PlineBoolean.PolylineBoolean<Polyline<double>, double>(
                pline1,
                pline2,
                BooleanOp.Or,
                new PlineBooleanOptions<double>());
        }

        [Fact]
        public void BooleanResultPlineListsAreNotMutableLists()
        {
            var result = IntersectingRectangles();

            AssertNotAMutableList(result.PosPlines);
            AssertNotAMutableList(result.NegPlines);

            Assert.NotEmpty(result.PosPlines);
            foreach (var resultPline in result.PosPlines)
            {
                AssertNotAMutableList(resultPline.Subslices);
            }
        }

        [Fact]
        public void ShapePlineListsAreNotMutableLists()
        {
            var shape = Shape<double>.FromPlines(
            [
                PlineBuilder.Closed(
                    (0.0, 0.0, 0.0),
                    (10.0, 0.0, 0.0),
                    (10.0, 10.0, 0.0),
                    (0.0, 10.0, 0.0)),
                PlineBuilder.Closed(
                    (8.0, 2.0, 0.0),
                    (8.0, 8.0, 0.0),
                    (2.0, 8.0, 0.0),
                    (2.0, 2.0, 0.0))
            ]);

            AssertNotAMutableList(shape.CcwPlines);
            AssertNotAMutableList(shape.CwPlines);

            var offset = shape.ParallelOffset(0.5, new ShapeOffsetOptions<double>());
            AssertNotAMutableList(offset.CcwPlines);
            AssertNotAMutableList(offset.CwPlines);
        }

        private static void AssertNotAMutableList<T>(IReadOnlyList<T> collection)
        {
            Assert.IsNotType<List<T>>(collection);
            Assert.Null(collection as List<T>);

            if (collection is IList<T> asList)
            {
                Assert.True(asList.IsReadOnly, "expected an IList view of the result collection to be read-only");
            }

            if (collection is System.Collections.IList nonGeneric)
            {
                Assert.True(nonGeneric.IsReadOnly, "expected a non-generic IList view of the result collection to be read-only");
            }
        }
    }
}
