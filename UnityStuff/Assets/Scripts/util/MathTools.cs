using System;
using System.Linq;
using Vector2 = UnityEngine.Vector2;

namespace util
{
    public static class MathTools
    {
        public static double[] LinSpace1D(double a, double b, int count, bool centered)
        {
            var stepSize = (b - a) / (count - 1);
            var left = a;
            var right = b;
            if (centered)
            {
                stepSize = (b - a) / count;
                left = a + 0.5f * stepSize;
            }
            return Enumerable.Range(0, count)
                .Select(i => left + i*stepSize)
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

        public static float AsDegree(float angle)
        {
            return (float) (angle * 180 / Math.PI);
        }

        public static double AsDegree(double angle)
        {
            return angle * 180 / Math.PI;
        }
        
        public static float AsRadian(float angle)
        {
            return (float) (angle * Math.PI / 180);
        }

        public static double AsRadian(double angle)
        {
            return (angle * Math.PI / 180);
        }
    }
}