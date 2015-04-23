using System;
using System.Linq;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests
{
    [TestFixture]
    public class NaiveBuilderTests
    {
        public class Expectation
        {
            public string Note { get; set; }
            public double[] Values { get; set; }
            public double[] DesiredPercentiles { get; set; }
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
                Note = "Basic data",
                DesiredPercentiles = Constants.DefaultPercentiles,
                Values = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(10, 1.5),
                    new Percentile(20, 2.5),
                    new Percentile(30, 3.5),
                    new Percentile(40, 4.5),
                    new Percentile(50, 5.5),
                    new Percentile(60, 6.5),
                    new Percentile(70, 7.5),
                    new Percentile(80, 8.5),
                    new Percentile(90, 9.5)
                }
            },
            new Expectation
            {
                Note = "Wikipedia example",
                DesiredPercentiles = new double[] { 5, 30, 40, 95 },
                Values = new double[] { 15, 20, 35, 40, 50 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(5, 15),
                    new Percentile(30, 20),
                    new Percentile(40, 27.5),
                    new Percentile(95, 50)
                }
            },
            new Expectation
            {
                Note = "Negative percentile",
                DesiredPercentiles = new double[] { -5 },
                Values = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(-5, 1)
                }
            },
            new Expectation
            {
                Note = "Lower than one percentile",
                DesiredPercentiles = new [] { 0.2 },
                Values = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(0.2, 1)
                }
            },
            new Expectation
            {
                Note = "More than 100 percentiles",
                DesiredPercentiles = Enumerable.Range(1, 99).Select(i => (double)i).Concat(new [] { 99.9, 99.99 }).ToArray(),
                Values = Enumerable.Range(1, 100).Select(i => (double)i).ToArray(),
                ExpectedPercentiles = Enumerable.Range(1, 99).Select(p => new Percentile(p, p + 0.5)).Concat(new [] { new Percentile(99.9, 100), new Percentile(99.99, 100) }).ToArray(),
            },
            new Expectation
            {
                Note = "Only one value",
                DesiredPercentiles = new double[] { 70 },
                Values = new double[] { 1 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(70, 1)
                }
            },
            new Expectation
            {
                Note = "Two values",
                DesiredPercentiles = new double[] { 50 },
                Values = new double[] { 1, 2 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(50, 1.5)
                }
            },
            new Expectation
            {
                Note = "No data",
                DesiredPercentiles = new double[] { 50 },
                Values = new double[] { },
                ExpectedPercentiles = new Percentile[0]
            }
        };

        [Test]
        [TestCaseSource("_testExpectations")]
        public void should_return_percentiles_for_given_data(Expectation expectation)
        {
            var builder = new NaiveBuilder(expectation.DesiredPercentiles);
            foreach (var datum in expectation.Values.Shuffle().ToArray())
                builder.AddValue(datum);

            var percentiles = builder.GetPercentiles().ToList();

            percentiles.ShouldBeEquivalentTo(expectation.ExpectedPercentiles, true);
        }

        [Test]
        public void should_work_with_random_uniform_distribution()
        {
            // Is Random uniform ?
            var random = new Random();
            var builder = new NaiveBuilder();
            for (var i = 0; i < 1000000; ++i)
                builder.AddValue(random.NextDouble() * 99 + 1);

            var percentiles = builder.GetPercentiles().ToList();

            // TODO: Use MSE to evaluate accuracy ?
            for (var i = 0; i < 9; ++i)
            {
                var deltaToPercentile = percentiles[i].Value - ((i + 1) * 10);
                deltaToPercentile.ShouldBeLessThan(1);
            }
        }
    }
}