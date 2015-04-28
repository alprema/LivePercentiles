using System;
using System.Globalization;
using System.IO;
using System.Linq;
using LivePercentiles.StaticBuilders;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests.StaticBuilders
{
    public class NearestRankBuilderTests
    {
        public class Expectation
        {
            public string Note { get; set; }
            public double[] Values { get; set; }
            public double[] DesiredPercentiles { get; set; }
            public Percentile[] ExpectedPercentiles { get; set; }

            public override string ToString() { return Note; }
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
                    new Percentile(10, 1),
                    new Percentile(20, 2),
                    new Percentile(30, 3),
                    new Percentile(40, 4),
                    new Percentile(50, 5),
                    new Percentile(60, 6),
                    new Percentile(70, 7),
                    new Percentile(80, 8),
                    new Percentile(90, 9)
                }
            },
            new Expectation
            {
                Note = "Wikipedia Nearest Rank example - 1",
                DesiredPercentiles = new double[] { 30, 40, 50, 100 },
                Values = new double[] { 15, 20, 35, 40, 50 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(30, 20),
                    new Percentile(40, 20),
                    new Percentile(50, 35),
                    new Percentile(100, 50)
                }
            },
            new Expectation
            {
                Note = "Wikipedia Nearest Rank example - 2",
                DesiredPercentiles = new double[] { 25, 50, 75, 100 },
                Values = new double[] { 3, 6, 7, 8, 8, 10, 13, 15, 16, 20 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(25, 7),
                    new Percentile(50, 8),
                    new Percentile(75, 15),
                    new Percentile(100, 20)
                }
            },
            new Expectation
            {
                Note = "Wikipedia Nearest Rank example - 3",
                DesiredPercentiles = new double[] { 25, 50, 75, 100 },
                Values = new double[] { 3, 6, 7, 8, 8, 9, 10, 13, 15, 16, 20 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(25, 7),
                    new Percentile(50, 9),
                    new Percentile(75, 15),
                    new Percentile(100, 20)
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
                ExpectedPercentiles = Enumerable.Range(1, 99).Select(p => new Percentile(p, p)).Concat(new [] { new Percentile(99.9, 100), new Percentile(99.99, 100) }).ToArray(),
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
                    new Percentile(50, 1)
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
            var builder = new NearestRankBuilder(expectation.DesiredPercentiles);
            foreach (var datum in expectation.Values.Shuffle().ToArray())
                builder.AddValue(datum);

            var percentiles = builder.GetPercentiles().ToList();

            percentiles.ShouldBeEquivalentTo(expectation.ExpectedPercentiles, true);
        }

        [Test]
        public void should_work_with_random_uniform_distribution()
        {
            var random = new Random();
            var builder = new NearestRankBuilder();
            for (var i = 0; i < 1000000; ++i)
                builder.AddValue(random.NextDouble() * 100);

            var percentiles = builder.GetPercentiles().ToList();

            Console.WriteLine(string.Join(", ", percentiles));
            for (var i = 0; i < 9; ++i)
            {
                var deltaToPercentile = percentiles[i].Value - ((i + 1) * 10);
                deltaToPercentile.ShouldBeLessThan(0.1);
            }
        }

        public class SampleFile
        {
            public string Filename { get; set; }
            public int[] ExpectedValues { get; set; }

            public override string ToString() { return Filename; }
        }

        private SampleFile[] _sampleFiles =
        {
            new SampleFile { Filename = "TestData/sample_data_100", ExpectedValues = new[] { 73, 80, 125, 269, 269, 269 } },
            new SampleFile { Filename = "TestData/sample_data_1000", ExpectedValues = new[] { 75, 82, 183, 320, 659, 659 } },
            new SampleFile { Filename = "TestData/sample_data_10000", ExpectedValues = new[] { 75, 82, 177, 342, 551, 603 } }
        };

        [Test]
        [TestCaseSource("_sampleFiles")]
        public void should_work_with_sample_data(SampleFile sampleFile)
        {
            var builder = new NearestRankBuilder(new [] { 80, 90, 99, 99.9, 99.99, 99.999 });
            
            foreach (var line in File.ReadAllLines(sampleFile.Filename))
            {
                var value = double.Parse(line, CultureInfo.InvariantCulture);
                builder.AddValue(value);
            }

            builder.GetPercentiles().Select(p => (int) p.Value).ShouldBeEquivalentTo(sampleFile.ExpectedValues);
        }
    }
}