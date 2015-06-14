using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LivePercentiles.StaticBuilders;
using LivePercentiles.StreamingBuilders;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests.StreamingBuilders
{
    public class ConstantErrorBasicCKMSBuilderTests
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
                Values = new double[] { 2, 7, 1, 10, 8, 9, 3, 4, 5, 6 },
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
                Note = "Repeated data",
                DesiredPercentiles = Constants.DefaultPercentiles,
                Values = new double[] { 2, 7, 1, 10, 10, 10, 10, 10, 8, 9, 3, 4, 5, 6 },
                ExpectedPercentiles = new[]
                {
                    new Percentile(10, 2),
                    new Percentile(20, 3),
                    new Percentile(30, 5),
                    new Percentile(40, 6),
                    new Percentile(50, 7),
                    new Percentile(60, 9),
                    new Percentile(70, 10),
                    new Percentile(80, 10),
                    new Percentile(90, 10)
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
                Note = "No data",
                DesiredPercentiles = new double[] { 50 },
                Values = new double[] { },
                ExpectedPercentiles = new Percentile[0]
            }
        };


        [Test]
        [TestCaseSource("_testExpectations")]
        [Repeat(10)]
        public void should_return_percentiles_for_given_data(Expectation expectation)
        {
            var builder = new ConstantErrorBasicCKMSBuilder(0.00000001, expectation.DesiredPercentiles);
            foreach (var datum in expectation.Values.Shuffle().ToArray())
                builder.AddValue(datum);
            
            var percentiles = builder.GetPercentiles().ToList();

            percentiles.ShouldBeEquivalentTo(expectation.ExpectedPercentiles, true);
        }

        [Test]
        public void  should_handle_hard_case()
        {
            var values = new double[] { 2, 7, 1, 10, 8, 9, 3, 4, 5, 6 };
            var expectedPercentiles = new[]
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

            };
            var builder = new ConstantErrorBasicCKMSBuilder(0.00000001, Constants.DefaultPercentiles);
            foreach (var datum in values.ToArray())
                builder.AddValue(datum);
            
            var percentiles = builder.GetPercentiles().ToList();

            percentiles.ShouldBeEquivalentTo(expectedPercentiles, true);
        }
        
        [Test]
        public void should_handle_simple_range()
        {
            var expectedPercentiles = new[]
            {
                new Percentile(10, 10),
                new Percentile(20, 20),
                new Percentile(30, 30),
                new Percentile(40, 40),
                new Percentile(50, 50),
                new Percentile(60, 60),
                new Percentile(70, 70),
                new Percentile(80, 80),
                new Percentile(90, 90)

            };
            var builder = new ConstantErrorBasicCKMSBuilder(0.001, Constants.DefaultPercentiles);
            foreach (var i in Enumerable.Range(1, 100).Shuffle())
                builder.AddValue(i);

            var percentiles = builder.GetPercentiles().ToList();

            percentiles.ShouldBeEquivalentTo(expectedPercentiles, true);
        }

        [Test]
        public void should_work_with_random_uniform_distribution()
        {
            var random = new Random();
            var desiredPercentiles = new double[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
            var builder = new ConstantErrorBasicCKMSBuilder(0.00001, desiredPercentiles);
            var referenceBuilder = new NearestRankBuilder(desiredPercentiles);
            for (var i = 0; i < 10000; ++i)
            {
                var value = random.NextDouble() * 100;
                builder.AddValue(value);
                referenceBuilder.AddValue(value);
            }

            var percentiles = builder.GetPercentiles().ToList();
            var referencePercentiles = referenceBuilder.GetPercentiles().ToList();

            Console.WriteLine("CKMS basic histogram");
            var squaredErrors = new List<double>();
            for (var i = 0; i < 9; ++i)
            {
                var deltaToPercentile = Math.Abs(percentiles[i].Value - referencePercentiles[i].Value);
                Console.WriteLine("[" + percentiles[i].Rank + "] => " + percentiles[i].Value + " (" + deltaToPercentile + ")");
                deltaToPercentile.ShouldBeLessThan(0.15);
                squaredErrors.Add(Math.Pow(deltaToPercentile, 2));
            }
            Console.WriteLine("MSE: " + squaredErrors.Average());
        }

        public class SampleFile
        {
            public string Filename { get; set; }
            public int[] ExpectedValues { get; set; }

            public override string ToString() { return Filename; }
        }

        private SampleFile[] _sampleFiles =
        {
            new SampleFile { Filename = "TestData/latency_sample_100", ExpectedValues = new[] { 73, 80, 125, 269, 269, 269 } },
            new SampleFile { Filename = "TestData/latency_sample_1000", ExpectedValues = new[] { 75, 82, 183, 320, 659, 659 } },
            new SampleFile { Filename = "TestData/latency_sample_10000", ExpectedValues = new[] { 75, 82, 177, 342, 551, 603 } }
        };

        [Test]
        [TestCaseSource("_sampleFiles")]
        public void should_work_with_sample_data(SampleFile sampleFile)
        {
            var builder = new ConstantErrorBasicCKMSBuilder(0.000000000000000001, new[] { 80, 90, 99, 99.9, 99.99, 99.999 });

            foreach (var line in File.ReadAllLines(sampleFile.Filename))
            {
                var value = double.Parse(line, CultureInfo.InvariantCulture);
                builder.AddValue(value);
            }

            builder.GetPercentiles().Select(p => (int)p.Value).ShouldBeEquivalentTo(sampleFile.ExpectedValues);
        }
    }
}