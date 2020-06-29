using System.Linq;
using Vector2 = UnityEngine.Vector2;

namespace util
{
    public static class MathTools
    {
        public static float[] LinSpace1D(float a, float b, int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => a + i*(b-a)/(count-1))
                .ToArray();
        }

        public static Vector2[] LinSpace2D(float a, float b, int count)
        {
            return LinSpace2D((a, a), (b, b), count);
        }
        
        public static Vector2[] LinSpace2D((float, float) a, (float, float) b, int count)
        {
            return Enumerable.Range(0, count)
                .Select(j => Enumerable.Range(0, count)
                    .Select(i => 
                        new Vector2(
                            a.Item1 + 1f*i*(b.Item1-a.Item1)/(count-1), 
                            a.Item2 + 1f*j*(b.Item2-a.Item2)/(count-1))
                    ).ToArray())
                .SelectMany(arr => arr)
                .ToArray();
        }
    }
}