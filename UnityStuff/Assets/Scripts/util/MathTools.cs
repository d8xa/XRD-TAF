using System.Linq;

namespace FoPra.util
{
    public class MathTools
    {
        public static double[] LinSpace(int length, decimal a, decimal b)
        {
            return Enumerable.Range(0, length)
                .Select(idx => idx != length-1 ? a + (b - a) / length * idx : b)
                .Select(e => (double) e)
                .ToArray();
        }
    }
}