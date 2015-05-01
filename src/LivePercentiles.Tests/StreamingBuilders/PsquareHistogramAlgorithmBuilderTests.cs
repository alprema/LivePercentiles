using System;
using System.Linq;
using LivePercentiles.StreamingBuilders;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests.StreamingBuilders
{
    [TestFixture]
    public class PsquareHistogramAlgorithmBuilderTests
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
            new Expectation
            {
                Note = "More than 100 percentiles",
                DesiredNumberOfBuckets = 200,
                Values = Enumerable.Range(0, 201).Select(i => (double)i).ToArray(),
                ExpectedPercentiles = Enumerable.Range(1, 199).Select(i => new Percentile(i / 2d, i)).ToArray(),
            },
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
        public void should_throw_with_fewer_than_three_buckets()
        {
            Assert.That(() => new PsquareHistogramAlgorithmBuilder(3),
                        Throws.InstanceOf<ArgumentException>().With.Message.EqualTo("At least four buckets should be provided to obtain meaningful estimates.\r\nParameter name: bucketCount"));
        }

        [Test]
        public void should_return_no_percentiles_if_there_is_not_enough_data()
        {
            var builder = new PsquareHistogramAlgorithmBuilder();

            var percentiles = builder.GetPercentiles().ToList();

            percentiles.Count.ShouldEqual(0);
        }
        
        [Test]
        [Ignore]
        public void performance_test()
        {
            throw new NotImplementedException("Todo");
        }

        [Test]
        [Ignore]
        public void should_handle_more_than_int_maxvalue_observations()
        {
            // TODO
//            var random = new Random();
//            var builder = new PsquareHistogramAlgorithmBuilder();
//            for (long i = 0; i < int.MaxValue + 1L; ++i)
//                builder.AddValue(random.NextDouble() * 100);
//
//            var percentiles = builder.GetPercentiles().ToList();
//
//            // TODO: Use MSE to evaluate accuracy ?
//            Console.WriteLine(string.Join(", ", percentiles));
//            for (var i = 0; i < 9; ++i)
//            {
//                var deltaToPercentile = percentiles[i].Value - ((i + 1) * 10);
//                deltaToPercentile.ShouldBeLessThan(0.15);
//            }
        }
    }
}