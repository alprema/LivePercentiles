using System.Collections.Generic;
using System.Linq;

namespace LivePercentiles.StaticBuilders
{
    /// <summary>
    /// Simple implementation used as a reference, should not be used
    /// for streaming data since it actually stores all the data.
    /// The method used is Linear Interpolation Between Closest Ranks
    /// (cf. http://en.wikipedia.org/wiki/Percentile)
    /// </summary>
    public class LinearInterpolationBuilder : IPercentileBuilder
    {
        private readonly double[] _desiredPercentiles;
        private readonly List<double> _values = new List<double>();

        public LinearInterpolationBuilder(double[] desiredPercentiles = null)
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

            var rankedValues = _values.OrderBy(x => x).Select((v, i) => new RankedValue(100d / _values.Count * (i + 0.5), v)).ToList();

            foreach (var desiredPercentile in _desiredPercentiles)
            {
                if (desiredPercentile < rankedValues.First().PercentRank)
                {
                    yield return new Percentile(desiredPercentile, rankedValues.First().Value);
                    continue;
                }

                if (desiredPercentile > rankedValues.Last().PercentRank)
                {
                    yield return new Percentile(desiredPercentile, rankedValues.Last().Value);
                    continue;
                }

                var exactMatch = rankedValues.SingleOrDefault(x => x.PercentRank == desiredPercentile);
                if (exactMatch != null)
                {
                    yield return new Percentile(desiredPercentile, exactMatch.Value);
                    continue;
                }

                var underIndex = GetPercentRankUnderDesiredPercentile(rankedValues, desiredPercentile);
                var rankedValueUnder = rankedValues[underIndex];
                var rankedValueOver = rankedValues[underIndex + 1];
                var value = rankedValueUnder.Value + rankedValues.Count * ((desiredPercentile - rankedValueUnder.PercentRank) / 100) * (rankedValueOver.Value - rankedValueUnder.Value);
                yield return new Percentile(desiredPercentile, value);
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