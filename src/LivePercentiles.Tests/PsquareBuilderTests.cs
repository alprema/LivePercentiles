using System;
using System.Linq;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests
{
    [TestFixture]
    public class PsquareBuilderTests
    {
        public class Expectation
        {
            public string Note { get; set; }
            public double[] Values { get; set; }
            public int DesiredNumberOfBuckets { get; set; }
            public Percentile[] ExpectedPercentiles { get; set; }

            public override string ToString()
            {
                return Note;
            }
        }

        private readonly Expectation[] _testExpectations =
        {
            // TODO: Add a safety to handle >100 percentiles
//            new Expectation
//            {
//                Note = "More than 100 percentiles",
//                DesiredNumberOfBuckets = 100,
//                Values = Enumerable.Range(1, 100).Select(i => (double)i).ToArray(),
//                ExpectedPercentiles = Enumerable.Range(1, 99).Select(p => new Percentile(p, p + 0.5)).Concat(new [] { new Percentile(99.9, 100), new Percentile(99.99, 100) }).ToArray(),
//            },
            // TODO
//            new Expectation
//            {
//                Note = "Only two buckets",
//                DesiredNumberOfBuckets = 2,
//                Values = new double[] { 1 },
//                ExpectedPercentiles = new[]
//                {
//                    new Percentile(70, 1)
//                }
//            },
            new Expectation
            {
                Note = "No data",
                DesiredNumberOfBuckets = Constants.DefaultBucketCount,
                Values = new double[] { },
                ExpectedPercentiles = new Percentile[0]
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 5",
                DesiredNumberOfBuckets = 4,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(25, 0.15),
                    new Percentile(50, 0.74),
                    new Percentile(75, 0.83)
                }
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 6",
                DesiredNumberOfBuckets = 4,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(25, 0.15),
                    new Percentile(50, 0.74),
                    new Percentile(75, 0.83)
                }
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 7",
                DesiredNumberOfBuckets = 4,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(25, 0.15),
                    new Percentile(50, 0.74),
                    new Percentile(75, 4.46)
                }
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 13",
                DesiredNumberOfBuckets = 4,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15, 15.43, 38.62, 15.92, 34.60, 10.28, 1.47 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(25, 2.13),
                    new Percentile(50, 9.27),
                    new Percentile(75, 21.57)
                }
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 20",
                DesiredNumberOfBuckets = 4,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15, 15.43, 38.62, 15.92, 34.60, 10.28, 1.47, 0.40, 0.05, 11.39, 0.27, 0.42, 0.09, 11.37 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(25, 0.49),
                    new Percentile(50, 4.44),
                    new Percentile(75, 17.2)
                }
            }
        };

        [Test]
        [TestCaseSource("_testExpectations")]
        public void should_return_percentiles_for_given_data(Expectation expectation)
        {
            var builder = new PsquareHistogramAlgorithmBuilder(expectation.DesiredNumberOfBuckets);
            foreach (var datum in expectation.Values.ToArray())
                builder.AddValue(datum);

            var percentiles = builder.GetPercentiles().ToList();

            var roundedPercentiles = percentiles.Select(p => new Percentile(p.Rank, Math.Round(p.Value, 2))).ToList();
            roundedPercentiles.ShouldBeEquivalentTo(expectation.ExpectedPercentiles, true);
        }

        [Test]
        public void should_work_with_random_uniform_distribution()
        {
            var random = new Random();
            var builder = new PsquareHistogramAlgorithmBuilder();
            for (var i = 0; i < 1000000; ++i)
                builder.AddValue(random.NextDouble() * 100);

            var percentiles = builder.GetPercentiles().ToList();

            // TODO: Use MSE to evaluate accuracy ?
            Console.WriteLine(string.Join(", ", percentiles));
            for (var i = 0; i < 9; ++i)
            {
                var deltaToPercentile = percentiles[i].Value - ((i + 1) * 10);
                deltaToPercentile.ShouldBeLessThan(0.15);
            }
        }

        [Test]
        public void should_recompute_marker_using_the_psquare_formula()
        {
            // Using the example data of Jain & Chlamtac's paper to verify the implementation
            var previous = new Marker(3, 0.74, double.NaN);
            var current = new Marker(4, 0.83, double.NaN);
            var next = new Marker(7, 22.37, double.NaN);

            var newMarkerValue = PsquareHistogramAlgorithmBuilder.ComputePsquareValueForMarker(previous, current, next, 1);

            newMarkerValue.ShouldEqual(4.465);
        }

        [Test]
        public void should_throw_with_one_bucket()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void throw_if_not_enough_observations()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void performance_test()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void should_handle_more_than_int_maxvalue_observations()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void should_handle_negative_number_of_buckets()
        {
            throw new NotImplementedException();
        }
    }
}