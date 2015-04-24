using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace LivePercentiles
{
    /// <summary>
    /// Implementation using the P² algorithm described in 
    /// Jain & Chlamtac's 1985 paper.
    /// The resulting percentiles are estimates but the input
    /// data is not stored, resulting in a very small memory
    /// footprint.
    /// (cf. http://www.cse.wustl.edu/~jain/papers/ftp/psqr.pdf)
    /// </summary>
    public class PsquareBuilder : IPercentileBuilder
    {
        private bool _isInitialized;
        private long _count;
        private readonly double[] _desiredPercentiles;
        private List<double> _startupQueue = new List<double>(); 
        private List<Marker> _markers = new List<Marker>();

        public PsquareBuilder(double[] desiredPercentiles = null)
        {
            _desiredPercentiles = desiredPercentiles ?? Constants.DefaultPercentiles;
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
            if (_count >= _desiredPercentiles.Length)
            {
                InitializeMarkers();
                _isInitialized = true;
            }
        }

        private void InitializeMarkers()
        {
            _markers = _startupQueue.OrderBy(x => x).Select((x, i) => new Marker(i + 1, x)).ToList();
        }

        private void NormalPhase(double value)
        {
            throw new NotImplementedException("Implement normal phase");
        }

        internal double ComputePsquareForMarker(ref Marker previousMarker, ref Marker currentMarker, ref Marker nextMarker, int markerShift)
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

        public IEnumerable<Percentile> GetPercentiles()
        {
            throw new System.NotImplementedException();
        }
    }
    
    internal struct Marker
    {
        public int Position;
        public double Value;

        public Marker(int position, double value)
        {
            Position = position;
            Value = value;
        }
    }
}