using System.Collections.Generic;

namespace LivePercentiles
{
    public interface IPercentileBuilder
    {
        void AddValue(double value);
        IEnumerable<Percentile> GetPercentiles();
    }
}
