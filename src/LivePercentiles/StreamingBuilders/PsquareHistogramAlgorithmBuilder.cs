using System;
using System.Collections.Generic;
using System.Linq;

namespace LivePercentiles.StreamingBuilders
{
    /// <summary>
    /// Implementation using the P² histogram algorithm described in 
    /// Jain & Chlamtac's 1985 paper.
    /// A number of "buckets" must be provided in the constructor,
    /// passing "5" will result in the algorithm calculating the
    /// 20th, 40th, 60th and 80th percentiles.
    /// The resulting percentiles are estimates but the input
    /// data is not stored, resulting in a very small memory
    /// footprint.
    /// (cf. http://www.cse.wustl.edu/~jain/papers/ftp/psqr.pdf)
    /// </summary>
    public class PsquareHistogramAlgorithmBuilder : BasePsquareBuilder, IPercentileBuilder
    {
        private readonly int _bucketCount;
        
        private readonly double[] _desiredPercentiles;
        
        public PsquareHistogramAlgorithmBuilder(int bucketCount = Constants.DefaultBucketCount)
        {
            if (bucketCount < 4)
                throw new ArgumentException("At least four buckets should be provided to obtain meaningful estimates.", "bucketCount");
            _bucketCount = bucketCount;
            _desiredPercentiles = Enumerable.Range(1, _bucketCount - 1).Select(i => 100d / _bucketCount * i).ToArray();
        }

        protected override bool IsReadyForNormalPhase()
        {
            return _observationsCount >= _desiredPercentiles.Length + 2;
        }

        protected override void InitializeMarkers()
        {
            _markers = _startupQueue.OrderBy(x => x).Select((x, i) =>
            {
                if (i == 0)
                    return new Marker(i + 1, x, double.NaN);
                if (i == _startupQueue.Count - 1)
                    return new Marker(i + 1, x, double.NaN);
                return new Marker(i + 1, x, _desiredPercentiles[i - 1]);
            }).ToList();
        }

        protected override void RecomputeNonExtremeMarkersValuesIfNecessary()
        {
            for (var i = 1; i < _markers.Count - 1; ++i)
            {
                var desiredPosition = 1 + i * (_observationsCount - 1.0) / _bucketCount;

                var deltaToDesiredPosition = desiredPosition - _markers[i].Position;
                var deltaToNextMarker = _markers[i + 1].Position - _markers[i].Position;
                var deltaToPreviousMarker = _markers[i - 1].Position - _markers[i].Position;

                if ((deltaToDesiredPosition >= 1 && deltaToNextMarker > 1) || (deltaToDesiredPosition <= -1 && deltaToPreviousMarker < -1))
                {
                    var unaryShift = deltaToDesiredPosition < 0 ? -1 : 1;
                    var newMarkerValue = ComputePsquareValueForMarker(_markers[i - 1], _markers[i], _markers[i + 1], unaryShift);
                    if (_markers[i - 1].Value < newMarkerValue && newMarkerValue < _markers[i + 1].Value)
                        _markers[i].Value = newMarkerValue;
                    else
                        _markers[i].Value = ComputeLinearValueForMarker(_markers[i - 1], _markers[i], _markers[i + 1], unaryShift);
                    _markers[i].Position += unaryShift;
                }
            }
        }

        public IEnumerable<Percentile> GetPercentiles()
        {
            return _markers.Skip(1).Take(_desiredPercentiles.Length).Select(x => new Percentile(x.Percentile, x.Value));
        }
    }
}