using System;
using System.Collections.Generic;
using System.Linq;

namespace LivePercentiles.StaticBuilders
{
    /// <summary>
    /// Simple implementation used as a reference, should not be used
    /// for streaming data since it actually stores all the data.
    /// The method used is Nearest Rank
    /// (cf. http://en.wikipedia.org/wiki/Percentile)
    /// </summary>
    public class NearestRankBuilder : IPercentileBuilder
    {
        private readonly double[] _desiredPercentiles;
        private readonly List<double> _values = new List<double>();

        public NearestRankBuilder(double[] desiredPercentiles = null)
        {
            _desiredPercentiles = desiredPercentiles ?? Constants.DefaultPercentiles;
        }

        public void AddValue(double value)
        {
            _values.Add(value);
        }

        public IEnumerable<Percentile> GetPercentiles()
        {
            if (!_values.Any())
                yield break;

            var orderedValues = _values.OrderBy(x => x).ToList();

            foreach (var desiredPercentile in _desiredPercentiles)
            {
                if (desiredPercentile < 0)
                {
                    yield return new Percentile(desiredPercentile, orderedValues[0]);
                    continue;
                }

                if (desiredPercentile == 100)
                {
                    yield return new Percentile(desiredPercentile, orderedValues[orderedValues.Count - 1]);
                    continue;
                }
                
                yield return new Percentile(desiredPercentile, orderedValues[(int)Math.Ceiling((decimal)desiredPercentile / 100m * orderedValues.Count) - 1]);
            }
        }

        private static int GetPercentRankUnderDesiredPercentile(List<RankedValue> rankedValues, double desiredPercentile)
        {
            for (var i = 0; i < rankedValues.Count; ++i)
            {
                if (rankedValues[i].PercentRank > desiredPercentile)
                    return i - 1;
            }
            return rankedValues.Count - 1;
        }

        private class RankedValue
        {
            public double Value { get; set; }
            public double PercentRank { get; set; }

            public RankedValue(double percentRank, double value)
            {
                PercentRank = percentRank;
                Value = value;
            }
        }
    }
}