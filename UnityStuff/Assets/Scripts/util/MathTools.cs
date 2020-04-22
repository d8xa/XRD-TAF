using System.Linq;
using UnityEngine;

namespace FoPra.util
{
    public class MathTools
    {
        public static float[] LinSpace1D(float a, float b, int count)
        {
            return Enumerable.Range(0, count)
                .Select(idx => idx != count-1 ? a + (b - a) / count * idx : b)
                .ToArray();
        }

        public static Vector2[] LinSpace2D(float a, float b, int count)
        {
            return LinSpace2D((a, a), (b, b), count);
        }
        
        public static Vector2[] LinSpace2D((float, float) a, (float, float) b, int count)
        {
            return Enumerable.Range(0, count)
                .Select(i => Enumerable.Range(0, count)
                    .Select(j => 
                        new Vector2(
                            a.Item1 + 1f*i*(b.Item1-a.Item1)/(count-1), 
                            a.Item2 + 1f*j*(b.Item2-a.Item2)/(count-1))
                    ).ToArray())
                .SelectMany(arr => arr)
                .ToArray();
        }
    }
}