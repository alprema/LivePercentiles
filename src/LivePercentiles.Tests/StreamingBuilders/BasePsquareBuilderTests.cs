using LivePercentiles.StreamingBuilders;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests.StreamingBuilders
{
    [TestFixture]
    public class BasePsquareBuilderTests
    {
        [Test]
        public void should_recompute_marker_using_the_psquare_formula()
        {
            // Using the example data of Jain & Chlamtac's paper to verify the implementation
            var previous = new Marker(3, 0.74, double.NaN);
            var current = new Marker(4, 0.83, double.NaN);
            var next = new Marker(7, 22.37, double.NaN);

            var newMarkerValue = BasePsquareBuilder.ComputePsquareValueForMarker(previous, current, next, 1);

            newMarkerValue.ShouldEqual(4.465);
        }
    }
}