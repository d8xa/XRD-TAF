using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;

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
                .Select(j => Enumerable.Range(0, count)
                    .Select(i => 
                        new Vector2(
                            a.Item1 + 1f*i*(b.Item1-a.Item1)/(count-1), 
                            a.Item2 + 1f*j*(b.Item2-a.Item2)/(count-1))
                    ).ToArray())
                .SelectMany(arr => arr)
                .ToArray();
        }

        public static void WriteArray2D(string path, string[] lines, int stride)
        {
            var res_str = Enumerable
                .Range(0, stride)
                .Select(i => String.Join("; ",
                    lines.Skip(i * stride).Take(stride).ToArray())
                )
                .ToArray();
            File.WriteAllLines(path, res_str);
        }
        
        //public static string[] ArrayToString2D()
    }
}