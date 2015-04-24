using System;
using System.Collections.Generic;
using System.Linq;

namespace LivePercentiles
{
    public class PsquareBuilder : IPercentileBuilder
    {
        private bool _isInitialized;
        private long _count;
        private readonly double[] _desiredPercentiles;
        private List<double> _startupQueue = new List<double>(); 
        private List<Marker> _markers = new List<Marker>();

        public PsquareBuilder(double[] desiredPercentiles = null)
        {
            _desiredPercentiles = desiredPercentiles;
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

        public IEnumerable<Percentile> GetPercentiles()
        {
            throw new System.NotImplementedException();
        }
    }

    internal struct Marker
    {
        public int CurrentPosition;
        public double Value;

        public Marker(int currentPosition, double value)
        {
            CurrentPosition = currentPosition;
            Value = value;
        }
    }
}