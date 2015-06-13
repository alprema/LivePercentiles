using System;
using System.Collections.Generic;

namespace LivePercentiles.StreamingBuilders
{
    public class ConstantErrorBasicCKMSBuilder : IPercentileBuilder
    {
        private readonly double _precision;
        private readonly double[] _desiredPercentiles;
        private List<Bucket> _buckets;
        private long _count;

        public ConstantErrorBasicCKMSBuilder(double precision, double[] desiredPercentiles)
        {
            _precision = precision;
            _desiredPercentiles = desiredPercentiles;
            _buckets = new List<Bucket>();
        }

        public void AddValue(double value)
        {
            // TODO: Add compression step
            if (_count == 0)
                _buckets.Add(new Bucket(value, 1, 0));
            else if (value < _buckets[0].ActualValue)
                _buckets.Insert(0, new Bucket(value, 1, 0));
            else if (value > _buckets[_buckets.Count - 1].ActualValue)
                _buckets.Add(new Bucket(value, 1, 0));
            else
            {
                var index = 0;
                for (; index < _buckets.Count; ++index)
                {
                    if (value < _buckets[index].ActualValue)
                        break;
                }
                // TODO: Check this out (should be ri)
                var previousBucketTrueRankLowerBound = 0;
                for (var i = 0; i < index; ++i)
                    previousBucketTrueRankLowerBound += _buckets[i].Gi;
                // Not removing 1 from Delta to make it work
                _buckets.Insert(index, new Bucket(value, 1, (int)Math.Floor(GetAllowableBucketSpread(previousBucketTrueRankLowerBound, _count + 1)) ));}
            ++_count;
        }

        /// <summary>
        /// Corresponds to f(ri, n) = 2ϵn
        /// </summary>
        /// <param name="rank">ri</param>
        /// <param name="totalEntriesCount">n</param>
        /// <returns>Gets the maximum spread of a bucket</returns>
        private double GetAllowableBucketSpread(int rank, long totalEntriesCount)
        {
            // Use size instead of count?
            return 2 * _precision * totalEntriesCount;
        }
        
        public IEnumerable<Percentile> GetPercentiles()
        {
            foreach (var desiredPercentile in _desiredPercentiles)
            {
                var targetIndex = (int) (desiredPercentile / 100 * _count);
                var targetIndexAccountingForError = targetIndex + GetAllowableBucketSpread(targetIndex, _count) / 2;

                Bucket previous = null;
                var ri = 0;
                foreach (var currentBucket in _buckets)
                {
                    if (previous != null)
                        ri += previous.Gi;

                    if (ri + currentBucket.Gi + currentBucket.Delta > targetIndexAccountingForError)
                    {
                        yield return new Percentile(desiredPercentile, (previous ?? currentBucket).ActualValue);
                        break;
                    }

                    previous = currentBucket;
                }
            }
        }
    }

    public class Bucket
    {
        public double ActualValue { get; set; }
        public int Gi { get; set; }
        /// <summary>
        /// Range of the bucket
        /// </summary>
        public int Delta { get; set; }

        public Bucket(double actualValue, int gi, int delta)
        {
            ActualValue = actualValue;
            Gi = gi;
            Delta = delta;
        }

        public override string ToString()
        {
            return "ActualValue: " + ActualValue + ", Gi: " + Gi + ", Delta: " + Delta;
        }
    }
}