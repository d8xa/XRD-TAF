using System.Linq;

namespace FoPra.math
{
    public class Math
    {
        // all methods are for 1D case. 2D arrays need to be flattened before calling these.
        
        public double absorption(
            double muSample,
            double muCell,
            double distSample,
            double distCell
            )
        {
            return System.Math.Exp(-muCell * distCell - muSample * distSample);
        }

        public double[] absorptions(
            double muSample,
            double muCell,
            double[] distSample,
            double[] distCell
            )
        {
            return Enumerable.Range(0, distCell.Length)
                .Select(i => absorption(muSample, muCell, distSample[i], distCell[i]))
                .ToArray();
        }

        public double intensity(double[] absorptions, double[] intensities)
        {
            return absorptions.Zip(intensities, (ai, ii) => ai * ii).Sum();
        }
        
        //public double[] dist(double)
    }
}