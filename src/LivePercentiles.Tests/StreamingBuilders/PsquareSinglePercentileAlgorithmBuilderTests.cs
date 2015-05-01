using System;
using System.Collections.Generic;
using System.Linq;
using LivePercentiles.StreamingBuilders;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests.StreamingBuilders
{
    [TestFixture]
    public class PsquareSinglePercentileAlgorithmBuilderTests
    {
        public class Expectation
        {
            public string Note { get; set; }
            public double[] Values { get; set; }
            public double DesiredPercentile { get; set; }
            public Percentile ExpectedPercentile { get; set; }

            public override string ToString()
            {
                return Note;
            }
        }

        private readonly Expectation[] _testExpectations =
        {
            new Expectation
            {
                Note = "Other than 50th percentile",
                DesiredPercentile = 95,
                Values = Enumerable.Range(0, 100).Select(i => (double)i).ToArray(),
                ExpectedPercentile = new Percentile(95, 94),
            },
            new Expectation
            {
                Note = "More than 100 percentiles",
                DesiredPercentile = 95,
                Values = Enumerable.Range(0, 201).Select(i => (double)i).ToArray(),
                ExpectedPercentile = new Percentile(95, 190),
            },
            new Expectation
            {
                Note = "No data",
                DesiredPercentile = 50,
                Values = new double[] { },
                ExpectedPercentile = null
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 5",
                DesiredPercentile = 50,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83 },
                ExpectedPercentile = new Percentile(50, 0.74)
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 7",
                DesiredPercentile = 50,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15 },
                ExpectedPercentile = new Percentile(50, 0.74)
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 13",
                DesiredPercentile = 50,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15, 15.43, 38.62, 15.92, 34.60, 10.28, 1.47 },
                ExpectedPercentile = new Percentile(50, 9.27)
            },
            new Expectation
            {
                Note = "Jain & Chlamtac's paper example - Step 20",
                DesiredPercentile = 50,
                Values = new [] { 0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15, 15.43, 38.62, 15.92, 34.60, 10.28, 1.47, 0.40, 0.05, 11.39, 0.27, 0.42, 0.09, 11.37 },
                ExpectedPercentile = new Percentile(50, 4.44)
            }
        };

        [Test]
        [TestCaseSource("_testExpectations")]
        public void should_return_percentiles_for_given_data(Expectation expectation)
        {
            var builder = new PsquareSinglePercentileAlgorithmBuilder(expectation.DesiredPercentile, Precision.LessPreciseAndFaster);
            foreach (var datum in expectation.Values.ToArray())
                builder.AddValue(datum);

            var percentiles = builder.GetPercentiles().ToList();

            if (expectation.ExpectedPercentile == null)
            {
                percentiles.ShouldBeEmpty();
                return;
            }
            Math.Round(percentiles.Single().Value, 2).ShouldEqual(expectation.ExpectedPercentile.Value);
        }

        [Test]
        public void should_work_with_random_uniform_distribution()
        {
            const Precision precision = Precision.LessPreciseAndFaster;
            var basicPercentiles = new [] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
            var random = new Random();
            var builders = basicPercentiles.Select(x => new PsquareSinglePercentileAlgorithmBuilder(x, precision)).ToList();
            for (var i = 0; i < 1000000; ++i)
                foreach (var builder in builders)
                    builder.AddValue(random.NextDouble() * 100);

            var percentiles = builders.Select(b => b.GetPercentiles().Single()).ToList();

            Console.WriteLine("P² single (" + precision + ")");
            var squaredErrors = new List<double>();
            for (var i = 0; i < 9; ++i)
            {
                var deltaToPercentile = percentiles[i].Value - ((i + 1) * 10);
                deltaToPercentile.ShouldBeLessThan(0.15);
                Console.WriteLine("[" + percentiles[i].Rank + "] => " + percentiles[i].Value + " (" + deltaToPercentile + ")");
                squaredErrors.Add(Math.Pow(deltaToPercentile, 2));
            }
            Console.WriteLine("MSE: " + squaredErrors.Average());
        }

        [Test]
        public void should_throw_with_negative_percentile()
        {
            Assert.That(() => new PsquareSinglePercentileAlgorithmBuilder(-3),
                        Throws.InstanceOf<ArgumentException>().With.Message.EqualTo("Only positive percentiles are allowed.\r\nParameter name: desiredPercentile"));
        }

        [Test]
        public void should_return_no_percentiles_if_there_is_not_enough_data()
        {
            var builder = new PsquareSinglePercentileAlgorithmBuilder(50);

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
//            var builder = new PsquareSinglePercentileAlgorithmBuilder();
//            for (long i = 0; i < int.MaxValue + 1L; ++i)
//                builder.AddValue(random.NextDouble() * 100);
//
//            var percentiles = builder.GetPercentiles().ToList();
//
//            Console.WriteLine(string.Join(", ", percentiles));
//            for (var i = 0; i < 9; ++i)
//            {
//                var deltaToPercentile = percentiles[i].Value - ((i + 1) * 10);
//                deltaToPercentile.ShouldBeLessThan(0.15);
//            }
        }
    }
}