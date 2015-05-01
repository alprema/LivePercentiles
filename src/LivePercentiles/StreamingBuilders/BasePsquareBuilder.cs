using System;
using System.Collections.Generic;
using System.Linq;

namespace LivePercentiles.StreamingBuilders
{
    /// <summary>
    /// Base class for implementations based on the
    /// Jain & Chlamtac's 1985 paper.
    /// (cf. http://www.cse.wustl.edu/~jain/papers/ftp/psqr.pdf)
    /// </summary>
    public abstract class BasePsquareBuilder
    {
        protected long _observationsCount;
        private bool _isInitialized;
        
        protected readonly List<double> _startupQueue = new List<double>();
        protected List<Marker> _markers = new List<Marker>();

        public void AddValue(double value)
        {
            ++_observationsCount;

            if (!_isInitialized)
                StartupPhase(value);
            else
                NormalPhase(value);
        }

        private void StartupPhase(double value)
        {
            _startupQueue.Add(value);
            if (!IsReadyForNormalPhase())
                return;

            InitializeMarkers();
            _isInitialized = true;
        }

        protected abstract bool IsReadyForNormalPhase();

        protected abstract void InitializeMarkers();

        private void NormalPhase(double value)
        {
            var containingBucketIndex = FindContainingBucket(value);
            IncrementImpactedMarkersPositions(containingBucketIndex + 1);
            RecomputeNonExtremeMarkersValuesIfNecessary();

            // TODO: Remove after thorough testing
            if (_observationsCount != _markers.Last().Position)
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

        protected abstract void RecomputeNonExtremeMarkersValuesIfNecessary();
        
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

        protected static double ComputeLinearValueForMarker(Marker previousMarker, Marker currentMarker, Marker nextMarker, int markerShift)
        {
            var otherMarker = markerShift < 0 ? previousMarker : nextMarker;
            var differenceBetweenOtherAndCurrentValue = otherMarker.Value - currentMarker.Value;
            var differenceBetweenOtherAndCurrentPosition = otherMarker.Position - currentMarker.Position;

            return currentMarker.Value + markerShift * (differenceBetweenOtherAndCurrentValue / differenceBetweenOtherAndCurrentPosition);
        }
    }
}