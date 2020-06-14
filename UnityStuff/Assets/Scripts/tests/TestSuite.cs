using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using util;
using Debug = UnityEngine.Debug;

namespace FoPra.tests
{
    public class TestSuite
    {
        static readonly Dictionary<string, bool> enabledTests = new List<KeyValuePair<string, bool>>() 
        {
            new KeyValuePair<string, bool>("Distances2D", true),
            new KeyValuePair<string, bool>("Distances2D_runtime", false)
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
                execute_Distances2D(computeShader, sizes[i], true);
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;

                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                Debug.Log($"RunTime (n={sizes[i]}, n^2={sizes[i] * sizes[i]}):\t{elapsedTime}");
            }
        }

        

        public static void test_Distances2D_runtime(ComputeShader computeShader, int size, int runs)
        {
            if (!enabledTests["Distances2D_runtime"])
            {
                return;
            }

            TimeSpan[] durations = new TimeSpan[runs];

            for (int i = 0; i < runs; i++)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                execute_Distances2D(computeShader, (int) Math.Pow(2, size), false);
                stopWatch.Stop();
                durations[i] = stopWatch.Elapsed;
            }

            var ts =  new TimeSpan(durations.Sum(d => d.Ticks));
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Debug.Log($"RunTime (runs={runs}, n^2={Math.Pow(2,size)}):\t{elapsedTime}");
        }


        public static Vector3[] execute_Distances2D(ComputeShader cs, int size, bool write)
        {
            /* Data parameters */
            int scale_factor = 1; // (float) Math.Pow(2, 10);
            var r_cell = 1.01f*scale_factor;
            var r_sample = 1.0f*scale_factor;
            var margin = 1.05f;
            var theta = 15.0;
            var (a, b) = (-r_sample*margin, r_sample*margin);
            
            /* Data generation */
            var data = MathTools.LinSpace2D(-r_cell * margin, r_cell * margin, size);
            var res = new Vector3[size*size];
            Array.Clear(res,0,size*size);


            var inputBuffer = new ComputeBuffer(data.Length, 8);
            var outputBufferOuter = new ComputeBuffer(data.Length, 8);
            var outputBufferInner = new ComputeBuffer(data.Length, 8);
            var absorptionsBuffer = new ComputeBuffer(data.Length, 12);
            cs.SetFloats("mu", (float) .54747E-1, (float) 6.70333E-1);
            cs.SetFloat("r_cell", r_cell);
            cs.SetFloat("r_sample", r_sample);
            cs.SetFloat("r_cell_sq", (float) Math.Pow(r_cell, 2));
            cs.SetFloat("r_sample_sq", (float) Math.Pow(r_sample, 2));
            cs.SetFloat("cos", (float) Math.Cos((180 - 2 * theta) * Math.PI / 180));
            cs.SetFloat("sin", (float) Math.Sin((180 - 2 * theta) * Math.PI / 180));
            
            inputBuffer.SetData(data);


            /* g1 pass: Compute g1 dists in shader buffers */
            int g1_handle = cs.FindKernel("g1_dists");
            cs.SetBuffer(g1_handle, "segment", inputBuffer);
            cs.SetBuffer(g1_handle, "distancesInner", outputBufferInner);
            cs.SetBuffer(g1_handle, "distancesOuter", outputBufferOuter);
            cs.Dispatch(g1_handle, size, 1, 1);
            
            /* g2 pass: Add g2 dists to g1 dists in shader buffers */
            int g2_handle = cs.FindKernel("g2_dists");
            cs.SetBuffer(g2_handle, "segment", inputBuffer);
            cs.SetBuffer(g2_handle, "distancesInner", outputBufferInner);
            cs.SetBuffer(g2_handle, "distancesOuter", outputBufferOuter);
            cs.Dispatch(g2_handle, size, 1, 1);

            /* absorptions pass: calculate absorptions from distances in shader buffers */
            int absorptions_handle = cs.FindKernel("Absorptions");
            cs.SetBuffer(absorptions_handle, "absorptions", absorptionsBuffer);
            cs.SetBuffer(absorptions_handle, "distancesInner", outputBufferInner);
            cs.SetBuffer(absorptions_handle, "distancesOuter", outputBufferOuter);
            cs.Dispatch(absorptions_handle, size, 1, 1);

            /* Read absorptions buffer, release buffers */
            absorptionsBuffer.GetData(res);
            inputBuffer.Release();
            outputBufferOuter.Release();
            outputBufferInner.Release();
            absorptionsBuffer.Release();
            
            if (write)
            {
                var res_scaled = res.Select(v => (new Vector3(v.x, v.y, v.z))).ToArray();
                res = res_scaled;
                
                var strings = res.Select(v => v.ToString("G")).ToArray();
                var path = Path.Combine("Logs", "Distances2D", $"Output n={size}.txt");
                WriteArray2D(path, strings, size);
                Debug.Log($"executed_Distances2D(cs, {size}, {write}): Wrote data");
                return res_scaled;
            }
            else return res;
        }
        
        public static void WriteArray2D(string path, string[] lines, int stride)
        {
            var resStr = Enumerable
                .Range(0, stride)
                .Select(i => String.Join("; ",
                    lines.Skip(i * stride).Take(stride).ToArray())
                )
                .ToArray();
            File.WriteAllLines(path, resStr);
        }
    }
}