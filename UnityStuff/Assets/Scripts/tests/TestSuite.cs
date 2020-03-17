using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace FoPra.tests
{
    public class TestSuite
    {
        static readonly Dictionary<string, bool> enabledTests = new List<KeyValuePair<string, bool>>() 
        {
            new KeyValuePair<string, bool>("Distances2D", true)
        }
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        public static void test_Distances2D(ComputeShader computeShader)
        {
            if (!enabledTests["Distances2D"])
            {
                return;
            }
            var sizes = Enumerable.Range(1, 10).Select(i => (int) Math.Pow(2, i)).ToArray();
            TimeSpan[] durations = new TimeSpan[sizes.Length];

            for (int i = 0; i < sizes.Length; i++)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var res = execute_Distances2D(computeShader, sizes[i]);
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Debug.Log($"RunTime (n={sizes[i]}, n^2={sizes[i]*sizes[i]}):\t{elapsedTime}");
            }
        }


        public static Vector2[] execute_Distances2D(ComputeShader cs, int size)
        {
            /* Data parameters */
            var r_cell = 1.01f / 2f;
            var r_sample = 1f / 2f;
            var scale_factor = (float) Math.Pow(2,10);
            var margin = 1.2f;
            var theta = 20d;
            var (a, b) = (-r_sample*scale_factor*margin, r_sample*scale_factor*margin);
            
            /* Data generation */
            var data = Enumerable.Range(0, size)
                .Select(i => Enumerable.Range(0, size)
                    .Select(j => 
                        new Vector2(a + 1f*i*(b-a)/(size-1), a + 1f*j*(b-a)/(size-1))
                        //new Vector2(i,j)
                    ).ToArray())
                .SelectMany(arr => arr)
                .ToArray();
            var res = new Vector2[size*size];
            Array.Clear(res,0,size*size);

            /* Pass data to shader */
            int kernelHandle = cs.FindKernel("Distances");
            var inputBuffer = new ComputeBuffer(data.Length,8);
            var outputBuffer = new ComputeBuffer(data.Length, 8);
            cs.SetBuffer(kernelHandle, "segment", inputBuffer);
            cs.SetBuffer(kernelHandle, "distances", outputBuffer);
            cs.SetFloat("r_cell", r_cell*scale_factor);
            cs.SetFloat("r_sample", r_sample*scale_factor);
            cs.SetFloat("m", (float) Math.Tan(theta * Math.PI/90));
            cs.SetFloats("n", 1f, (float) -(1f/Math.Tan(theta * Math.PI/90)));

            /* Execute shader */
            inputBuffer.SetData(data);
            cs.Dispatch(kernelHandle,64,1,1);
            outputBuffer.GetData(res);
            inputBuffer.Release();
            outputBuffer.Release();
            
            /* Rescale results */
            var res_scaled = res.Select(v => v/scale_factor).ToArray();
            res = res_scaled; 

            //Debug.Log(String.Join("", Enumerable.Range(0,size-1).Select(i => res[i].ToString()).ToArray()));
            var strings = res.Select(v => v.ToString()).ToArray();
            var res_str = Enumerable
                .Range(0, size - 1)
                .Select(i => String.Join("; ",
                    strings.Skip(i * size).Take(size).ToArray())
                )
                .ToArray();
            var sep = Path.DirectorySeparatorChar;
            File.WriteAllLines($"Output Files{sep}Distances2D Output n={size}.txt", res_str);
            return res_scaled;
        }
    }
}