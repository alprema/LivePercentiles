﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LivePercentiles.StaticBuilders;
using LivePercentiles.StreamingBuilders;
using LivePercentiles.Tests.Extensions;
using NUnit.Framework;

namespace LivePercentiles.Tests
{
    public class ComparisonTests
    {
        // TODO: make this a real thing and test it
        class CombinedPsquareSinglePercentileAlgorithmBuilder : IPercentileBuilder
        {
            private List<PsquareSinglePercentileAlgorithmBuilder> _innerBuilders;

            public CombinedPsquareSinglePercentileAlgorithmBuilder(double[] desiredPercentiles, Precision precision = Constants.DefaultPrecision)
            {
                _innerBuilders = desiredPercentiles.Select(p => new PsquareSinglePercentileAlgorithmBuilder(p, precision)).ToList();
            }

            public void AddValue(double value)
            {
                foreach (var builder in _innerBuilders)
                    builder.AddValue(value);
            }

            public IEnumerable<Percentile> GetPercentiles()
            {
                return _innerBuilders.Select(b => b.GetPercentiles().Single());
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
            new SampleFile { Filename = "TestData/latency_sample_100" },
            new SampleFile { Filename = "TestData/latency_sample_1000" },
            new SampleFile { Filename = "TestData/latency_sample_10000" },
            new SampleFile { Filename = "TestData/random_sample_100" },
            new SampleFile { Filename = "TestData/random_sample_1000" },
            new SampleFile { Filename = "TestData/random_sample_10000" }
        };

        [Test]
        [TestCaseSource("_sampleFiles")]
        public void should_work_with_sample_data(SampleFile sampleFile)
        {
            var desiredPercentiles = new[] { 80, 90, 99, 99.9, 99.99, 99.999 };
            var builders = new List<Tuple<string, IPercentileBuilder>>
            {
                new Tuple<string, IPercentileBuilder>("Nearest rank", new NearestRankBuilder(desiredPercentiles)),
                new Tuple<string, IPercentileBuilder>("P² single value (fast)", new CombinedPsquareSinglePercentileAlgorithmBuilder(desiredPercentiles, Precision.LessPreciseAndFaster)),
                new Tuple<string, IPercentileBuilder>("P² single value (normal)", new CombinedPsquareSinglePercentileAlgorithmBuilder(desiredPercentiles)),
                new Tuple<string, IPercentileBuilder>("Hdr histogram)", new HdrHistogramBuilder(int.MaxValue /* Assuming we don't know the data */, 2, desiredPercentiles))
            };
            var nearestRank = builders[0].Item2;

            foreach (var line in File.ReadAllLines(sampleFile.Filename))
            {
                var value = double.Parse(line, CultureInfo.InvariantCulture);
                foreach (var builder in builders)
                    builder.Item2.AddValue(value);
            }

            Console.WriteLine("Percentile;" + string.Join(";", builders.Select(x => x.Item1)));
            foreach (var desiredPercentile in desiredPercentiles)
                Console.WriteLine(desiredPercentile + ";" + string.Join(";", builders.Select(x => (int) x.Item2.GetPercentiles().Single(p => p.Rank == desiredPercentile).Value)));
            Console.Write("MSE to Nearest rank");
            foreach (var builder in builders)
            {
                var squaredErrors = nearestRank.GetPercentiles().Zip(builder.Item2.GetPercentiles(), (a, b) => Math.Pow(a.Value - b.Value, 2)).ToList();
                Console.Write(";" + squaredErrors.Average());
            }

            Console.WriteLine("\r\n\r\nData size in bytes: " + sizeof(double) * File.ReadAllLines(sampleFile.Filename).Count());
            Console.WriteLine("Hdr estimated size: " + ((HdrHistogramBuilder)builders.Last().Item2).GetEstimatedSize());
            Console.WriteLine("P² (fast) estimated size: 200");
            Console.WriteLine("P² (normal) estimated size: 270");
        }
    }
}