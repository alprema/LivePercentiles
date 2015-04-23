namespace LivePercentiles
{
    public class Percentile
    {
        public double Rank { get; set; }
        public double Value { get; set; }

        public Percentile(double rank, double value)
        {
            Rank = rank;
            Value = value;
        }

        public override string ToString()
        {
            return "[" + Rank + "] " + Value;
        }
    }
}