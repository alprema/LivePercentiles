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
    public class PsquareHistogramAlgorithmBuilder : IPercentileBuilder
    {
        private bool _isInitialized;
        private long _count;
        private readonly int _bucketCount;
        
        private readonly double[] _desiredPercentiles;
        private readonly List<double> _startupQueue = new List<double>(); 
        private List<Marker> _markers = new List<Marker>();
        
        public PsquareHistogramAlgorithmBuilder(int bucketCount = Constants.DefaultBucketCount)
        {
            if (bucketCount < 4)
                throw new ArgumentException("At least four buckets should be provided to obtain meaningful estimates.", "bucketCount");
            _bucketCount = bucketCount;
            _desiredPercentiles = Enumerable.Range(1, _bucketCount - 1).Select(i => 100d / _bucketCount * i).ToArray();
        }

        public void AddValue(double value)
        {
            ++_count;

            if (!_isInitialized)
                StartupPhase(value);
            else
                NormalPhase(value);
        }

        private void StartupPhase(double value)
        {
            _startupQueue.Add(value);
            if (_count < _desiredPercentiles.Length + 2)
                return;
            
            InitializeMarkers();
            _isInitialized = true;
        }

        private void InitializeMarkers()
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

        private void NormalPhase(double value)
        {
            var containingBucketIndex = FindContainingBucket(value);
            IncrementImpactedMarkersPositions(containingBucketIndex + 1);
            RecomputeNonExtremeMarkersValuesIfNecessary();

            // TODO: Remove after thorough testing
            if (_count != _markers.Last().Position)
                throw new InvalidOperationException("That can't be !");
        }

        private int FindContainingBucket(double value)
        {
            if (value < _markers.First().Value)
            {
                _markers.First().Value = value;
                return 0;
            }

            for (var i = 0; i < _markers.Count - 2; ++i)
            {
                if (_markers[i].Value <= value && value < _markers[i + 1].Value)
                    return i;
            }

            // TODO: simplify
            if (_markers[_markers.Count - 2].Value <= value && value <= _markers[_markers.Count - 1].Value)
                return _markers.Count - 2;

            if (value > _markers.Last().Value)
            {
                _markers.Last().Value = value;
                return _markers.Count - 2;
            }

            throw new InvalidOperationException("Should not happen");
        }

        private void IncrementImpactedMarkersPositions(int firstImpactedBucketIndex)
        {
            for (var i = firstImpactedBucketIndex; i < _markers.Count; i++)
                _markers[i].IncrementPosition();
        }

        private void RecomputeNonExtremeMarkersValuesIfNecessary()
        {
            for (var i = 1; i < _markers.Count - 1; ++i)
            {
                var desiredPosition = 1 + i * (_count - 1.0) / _bucketCount;

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

        internal static double ComputePsquareValueForMarker(Marker previousMarker, Marker currentMarker, Marker nextMarker, int markerShift)
        {
            var ratioBetweenPreviousAndNextPosition = (double)markerShift / (nextMarker.Position - previousMarker.Position);
            var distanceBetweenPreviousAndNewPosition = currentMarker.Position - previousMarker.Position + markerShift;
            var differenceBetweenNextAndCurrentValue = nextMarker.Value - currentMarker.Value;
            var differenceBetweenNextAndCurrentPosition = nextMarker.Position - currentMarker.Position;
            var distanceBetweenNextAndNewPosition = nextMarker.Position - currentMarker.Position - markerShift;
            var differenceBetweenPreviousAndCurrentValue = currentMarker.Value - previousMarker.Value;
            var differenceBetweenPreviousAndCurrentPosition = currentMarker.Position - previousMarker.Position;

            return currentMarker.Value
                   + ratioBetweenPreviousAndNextPosition
                   * (distanceBetweenPreviousAndNewPosition * (differenceBetweenNextAndCurrentValue / differenceBetweenNextAndCurrentPosition)
                      + distanceBetweenNextAndNewPosition * (differenceBetweenPreviousAndCurrentValue / differenceBetweenPreviousAndCurrentPosition));

        }

        private static double ComputeLinearValueForMarker(Marker previousMarker, Marker currentMarker, Marker nextMarker, int markerShift)
        {
            var otherMarker = markerShift < 0 ? previousMarker : nextMarker;
            var differenceBetweenOtherAndCurrentValue = otherMarker.Value - currentMarker.Value;
            var differenceBetweenOtherAndCurrentPosition = otherMarker.Position - currentMarker.Position;

            return currentMarker.Value + markerShift * (differenceBetweenOtherAndCurrentValue / differenceBetweenOtherAndCurrentPosition);
        }

        public IEnumerable<Percentile> GetPercentiles()
        {
            return _markers.Skip(1).Take(_desiredPercentiles.Length).Select(x => new Percentile(x.Percentile, x.Value));
        }
    }
    
    internal class Marker
    {
        public int Position { get; set; }
        public double Value { get; set; }
        public double Percentile { get; set; }

        public void IncrementPosition()
        {
            Position = Position + 1;
        }

        public Marker(int position, double value, double percentile)
        {
            Position = position;
            Value = value;
            Percentile = percentile;
        }
    }
}