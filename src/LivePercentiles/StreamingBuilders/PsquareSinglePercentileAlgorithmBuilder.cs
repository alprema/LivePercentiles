using System;
using System.Collections.Generic;
using System.Linq;

namespace LivePercentiles.StreamingBuilders
{
    /// <summary>
    /// Implementation using the P² single value algorithm described in 
    /// Jain & Chlamtac's 1985 paper.
    /// The desired percentile must be provided along with a precision,
    /// the more precise the estimate, the slower the calculation.
    /// The resulting percentile is an estimate but the input
    /// data is not stored, resulting in a very small memory
    /// footprint.
    /// (cf. http://www.cse.wustl.edu/~jain/papers/ftp/psqr.pdf)
    /// </summary>
    public class PsquareSinglePercentileAlgorithmBuilder : BasePsquareBuilder, IPercentileBuilder
    {
        private readonly double _desiredPercentile;
        private readonly int _intermediateMarkersCount;
        private readonly int _halfBucketCount;
        private readonly int _desiredPercentileIndex;

        public PsquareSinglePercentileAlgorithmBuilder(double desiredPercentile, Precision precision = Constants.DefaultPrecision)
        {
            if (desiredPercentile < 0)
                throw new ArgumentException("Only positive percentiles are allowed.", "desiredPercentile");
            _desiredPercentile = desiredPercentile;
            _intermediateMarkersCount = GetNumberOfIntermediateMarkersFromPrecision(precision);
            _halfBucketCount = (_intermediateMarkersCount + 2) / 2;
            _desiredPercentileIndex = (_intermediateMarkersCount + 3) / 2;
        }

        private int GetNumberOfIntermediateMarkersFromPrecision(Precision precision)
        {
            switch (precision)
            {
                case Precision.LessPreciseAndFaster:
                    return 2;
                case Precision.Normal:
                    return 4;
                case Precision.MorePreciseAndSlower:
                    return 6;
                default:
                    throw new ArgumentException("Unknown precision");
            }
        }

        protected override bool IsReadyForNormalPhase()
        {
            return _observationsCount >= _intermediateMarkersCount + 3;
        }

        protected override void InitializeMarkers()
        {
            _markers = _startupQueue.OrderBy(x => x).Select((x, i) =>
            {
                if (i == 0)
                    return new Marker(i + 1, x, double.NaN);
                if (i == _startupQueue.Count - 1)
                    return new Marker(i + 1, x, double.NaN);
                if (i < _desiredPercentileIndex)
                    return new Marker(i + 1, x, _desiredPercentile / _halfBucketCount * i);
                if (i > _desiredPercentileIndex)
                    return new Marker(i + 1, x, _desiredPercentile + ((100d - _desiredPercentile) / _halfBucketCount * (i - _desiredPercentileIndex)));
                
                return new Marker(i + 1, x, _desiredPercentile);
                
            }).ToList();
        }

        // TODO: Factorize with other implem
        protected override void RecomputeNonExtremeMarkersValuesIfNecessary()
        {
            for (var i = 1; i < _markers.Count - 1; ++i)
            {
                var desiredPosition = ComputeDesiredPositionForIndex(i);
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

        private double ComputeDesiredPositionForIndex(int i)
        {
            return 1 + (_observationsCount - 1) * _markers[i].Percentile / 100;
        }

        public IEnumerable<Percentile> GetPercentiles()
        {
            if (!IsInitialized)
                return Enumerable.Empty<Percentile>();
            return new[] { new Percentile(_desiredPercentile, _markers[_desiredPercentileIndex].Value) };
        }
    }
}