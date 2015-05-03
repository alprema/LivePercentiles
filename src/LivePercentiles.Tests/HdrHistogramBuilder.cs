using HdrHistogram.NET;
using System.Collections.Generic;

namespace LivePercentiles.Tests
{
    /// <summary>
    /// Wrapper around Hdr histogram for benching / comparison
    /// </summary>
    public class HdrHistogramBuilder : IPercentileBuilder
    {
        private readonly Histogram _histogram;
        private readonly double[] _desiredPercentiles;

        public HdrHistogramBuilder(int highestTrackableValue, int numberOfSignificantValueDigits, double[] desiredPercentiles = null)
        {
            _desiredPercentiles = desiredPercentiles ?? Constants.DefaultPercentiles;
            _histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
        }

        public void AddValue(double value)
        {
            _histogram.recordValue((long)value);
        }

        public IEnumerable<Percentile> GetPercentiles()
        {
            foreach (var desiredPercentile in _desiredPercentiles)
            {
                yield return new Percentile(desiredPercentile, _histogram.getValueAtPercentile(desiredPercentile));
            }
        }

        public int GetEstimatedSize()
        {
            return _histogram.getEstimatedFootprintInBytes();
        }
    }
}