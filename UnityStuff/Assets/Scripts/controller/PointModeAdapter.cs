using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using FoPra.model;
using FoPra.util;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace controller
{
    public class PointModeAdapter : ShaderAdapter
    {
        private Vector3[] _absorptionFactors;
        private readonly int _nrSegments;
        private readonly int _nrAnglesTheta;
        
        public PointModeAdapter(
            ComputeShader shader, 
            Model model, 
            float margin, 
            bool writeDistances, 
            bool writeFactors
            ) : base(shader, model, margin, writeDistances, writeFactors)
        {
            _nrAnglesTheta = Model.get_angles2D().Length;
            _nrSegments = _segmentResolution * _segmentResolution;            // number of points in probe _model.
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesTheta];
        }

        public PointModeAdapter(
            ComputeShader shader, 
            Model model
            ) : base(shader, model)
        {
            _nrAnglesTheta = Model.get_angles2D().Length;
            _nrSegments = _segmentResolution * _segmentResolution;            // number of points in probe _model.
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesTheta];
        }

        protected override void Compute()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            // initialize g1 distance arrays.
            var g1DistsOuter = new Vector2[_nrSegments];
            var g1DistsInner = new Vector2[_nrSegments];
            Array.Clear(g1DistsOuter, 0, _nrSegments);
            Array.Clear(g1DistsInner, 0, _nrSegments);
            
            Debug.Log($"{sw.Elapsed}: Initialized g1 distance arrays.");

            // initialize parameters in shader.
            Shader.SetFloats("mu", Model.get_mu_cell(), Model.get_mu_sample());
            Shader.SetFloat("r_cell", Model.get_r_cell());
            Shader.SetFloat("r_sample", Model.get_r_sample());
            Shader.SetFloat("r_cell_sq", Model.get_r_cell_sq());
            Shader.SetFloat("r_sample_sq", Model.get_r_sample_sq());
            
            // necessary here already?
            Shader.SetInt("bufCount_Segments", _nrSegments);
            Shader.SetInt("bufIndex_Factors", 0);
            
            Debug.Log($"{sw.Elapsed}: Set shader parameters.");


            // get kernel handles.
            var g1Handle = Shader.FindKernel("g1_dists");
            //var g2Handle = Shader.FindKernel("g2_dists");
            var absorptionsHandle = Shader.FindKernel("Absorptions");
            //var absorptionFactorsHandle = Shader.FindKernel("AbsorptionFactors");
            
            Debug.Log($"{sw.Elapsed}: Retrieved kernel handles.");

 
            // make buffers.
            var inputBuffer = new ComputeBuffer(_coordinates.Length, 8);
            var outputBufferOuter = new ComputeBuffer(_coordinates.Length, 8);
            var outputBufferInner = new ComputeBuffer(_coordinates.Length, 8);
            var absorptionsBuffer = new ComputeBuffer(_coordinates.Length, 12);
            //var absorptionFactorsBuffer = new ComputeBuffer(_nrAnglesTheta, 12);
            
            Debug.Log($"{sw.Elapsed}: Created buffers.");


            // set thread groups on X axis
            var threadGroupsX = (int) Math.Min(Math.Pow(2, 16) - 1, Math.Pow(_nrSegments, 2));
            // TODO: handle case threadGroupsX > 1024.

            
            // write data to buffers.
            inputBuffer.SetData(_coordinates);
            Shader.SetBuffer(g1Handle, "segment", inputBuffer);
            Shader.SetBuffer(g1Handle, "distancesInner", outputBufferInner);
            Shader.SetBuffer(g1Handle, "distancesOuter", outputBufferOuter);
            
            Debug.Log($"{sw.Elapsed}: Wrote data to buffers.");

            
            // compute g1 distances.
            Shader.Dispatch(g1Handle, threadGroupsX, 1, 1);
            
            Debug.Log($"{sw.Elapsed}: Calculated g1 distances.");
            
            /*
            // initialize buffers for absorption factor calculation.
            Shader.SetBuffer(absorptionFactorsHandle, "segment", inputBuffer);
            Shader.SetBuffer(absorptionFactorsHandle, "absorptionFactors", absorptionFactorsBuffer);
            
            Debug.Log($"{sw.Elapsed}: Initialized absorption factor buffers.");
            */


            var loop_ts = new TimeSpan();
            var factors_ts = new TimeSpan();
            var absorptions = new Vector3[_nrSegments];
            Array.Clear(absorptions, 0, absorptions.Length);
            
            // for each angle:
            for (int j = 0; j < _nrAnglesTheta; j++) {
                // TODO: check if g2 kernel can access filled distances buffer of g1 kernel.
                var loopStart = sw.Elapsed;
                
                /*
                //TEST
                Shader.SetBuffer(g1Handle, "distancesInner", outputBufferInner);
                Shader.SetBuffer(g1Handle, "distancesOuter", outputBufferOuter);
                
                Shader.SetBuffer(g2Handle, "segment", inputBuffer);
                Shader.SetFloat("cos", (float) Math.Cos((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                Shader.SetFloat("sin", (float) Math.Sin((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                Shader.SetBuffer(g2Handle, "distancesInner", outputBufferInner);
                Shader.SetBuffer(g2Handle, "distancesOuter", outputBufferOuter);
                Shader.Dispatch(g2Handle, threadGroupsX, 1, 1);
                */

                
                // set coordinate buffer. remove?
                Shader.SetBuffer(absorptionsHandle, "segment", inputBuffer);
                Shader.SetFloat("cos", (float) Math.Cos((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                Shader.SetFloat("sin", (float) Math.Sin((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                Shader.SetBuffer(absorptionsHandle, "distancesInner", outputBufferInner);
                Shader.SetBuffer(absorptionsHandle, "distancesOuter", outputBufferOuter);
                Shader.SetBuffer(absorptionsHandle, "absorptions", absorptionsBuffer);
                Shader.Dispatch(absorptionsHandle, threadGroupsX, 1, 1);
                

                absorptionsBuffer.GetData(absorptions);
                var factor_start = sw.Elapsed;
                _absorptionFactors[j] =
                    GetAbsorptionFactorLINQ(absorptions, Vector3.kEpsilon);
                    //GetAbsorptionFactor(absorptions, j);
                var factor_stop = sw.Elapsed;
                factors_ts += factor_stop - factor_start;

                // TODO: getData for buffer necessary?
                // TODO: research buffer counter:
                // https://docs.unity3d.com/ScriptReference/ComputeBufferType.Counter.html
                /*
                Shader.SetInt("bufIndex_Factors", j);
                Shader.SetBuffer(absorptionFactorsHandle, "absorptions", absorptionsBuffer);
                Shader.Dispatch(absorptionFactorsHandle, threadGroupsX, 1, 1);
                */
                var loopStop = sw.Elapsed;
                loop_ts += loopStop - loopStart;
            }
            
            Debug.Log($"{sw.Elapsed}: Computation done. Took {loop_ts}." 
                      + $" Took {TimeSpan.FromTicks(loop_ts.Ticks/_nrAnglesTheta)} avg. per loop,"
                      + $" {TimeSpan.FromTicks(factors_ts.Ticks/_nrAnglesTheta)} for absorption factor calculation.");

            // save absorption factors from buffer to variable.
            var readBufferStart = sw.Elapsed;
            inputBuffer.Release();
            Debug.Log($"{sw.Elapsed}: Input buffer released.");
            
            //System.Threading.Thread.Sleep(10000);
            //Debug.Log($"{sw.Elapsed}: slept 10s.");

            //absorptionFactorsBuffer.GetData(_absorptionFactors);
            //absorptionsBuffer.GetData(absorptions);
            //Debug.Log($"{sw.Elapsed}: Retrieved factors from buffers. Took {sw.Elapsed-readBufferStart}");
            
            // release buffers.
            outputBufferOuter.Release();
            outputBufferInner.Release();
            absorptionsBuffer.Release();
            //absorptionFactorsBuffer.Release();
            Debug.Log($"{sw.Elapsed}: Buffers released.");

            if (_writeFactors)
            {
                Debug.Log($"{sw.Elapsed}: Started writing data to disk.");
                WriteAbsorptionFactors();
                Debug.Log($"{sw.Elapsed}: Finished writing data to disk.");
            }
            
            sw.Stop();
        }

        protected override void Write()
        {
            var path = Path.Combine("Logs", "Absorptions2D", $"Output n={_segmentResolution}.txt");
            var headRow = string.Join("\t", "2 theta", "A_{s,sc}", "A_{c,sc}", "A_{c,c}");
            var headCol = Model.get_angles2D()
                .Select(angle => angle.ToString("G", CultureInfo.InvariantCulture))
                .ToArray();
            var data = new float[_nrAnglesTheta, 3];
            for (int i = 0; i < _nrAnglesTheta; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    data[i, j] = _absorptionFactors[i][j];
                }
            }
            
            util.ArrayWriteTools.Write2D(path, headCol, headRow, data);
        }

        private Vector3 GetAbsorptionFactor(Vector3[] absorptions, int j)
        {
            float[] absorptionSum = new float[]{0.0f, 0.0f, 0.0f};
            uint[] count = new uint[]{0,0,0};

            for (int i = 0; i < _nrSegments; i++)
            {
                double norm = Vector3.Magnitude(_coordinates[i]);
                if (norm <= Model.get_r_cell())
                {
                    if (norm > Model.get_r_sample())
                    {
                        absorptionSum[1] += absorptions[i].y;
                        absorptionSum[2] += absorptions[i].z;
                        count[1] += 1;
                        count[2] += 1;
                    }
                    else
                    {
                        absorptionSum[0] += absorptions[i].x;
                        count[0] += 1;
                    }
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (count[i] == 0) count[i] = 1;
            }

            return _absorptionFactors[j] = new Vector3(
                    absorptionSum[0] / count[0],
                    absorptionSum[1] / count[1],
                    absorptionSum[2] / count[2]
                    );
        }
        
        
        Vector3 GetAbsorptionFactorLINQ(Vector3[] absorptions, float tol)
        {
            return new Vector3(
                absorptions.AsParallel().Select(v => v.x).Where(x => x >= tol).Average(),
                absorptions.AsParallel().Select(v => v.y).Where(x => x >= tol).Average(),
                absorptions.AsParallel().Select(v => v.z).Where(x => x >= tol).Average()
            );
        }

        private float IsContained(float norm, int @case)
        {
            if (@case == 0)
                return norm <= Model.get_r_cell() && norm <= Model.get_r_sample() ? 1f : 0f;
            else
                return norm <= Model.get_r_cell() && norm > Model.get_r_sample() ? 1f : 0f;
        }

        private Vector3 IsContained(Vector3 v)
        {
            var norm = v.magnitude;
            return new Vector3(
                IsContained(norm, 0), 
                IsContained(norm, 1), 
                IsContained(norm, 2)
                );
        }
        
        
        private void WriteAbsorptionFactors()
        {
            using (FileStream fileStream = File.Create(Path.Combine("Logs", "Absorptions2D", $"Output n={_segmentResolution}.txt")))
            using (BufferedStream buffered = new BufferedStream(fileStream))
            using (StreamWriter writer = new StreamWriter(buffered))
            {
                writer.WriteLine(string.Join("\t", "2 theta","A_{s,sc}", "A_{c,sc}", "A_{c,c}"));
                for (int i = 0; i < _nrAnglesTheta; i++)
                {
                    writer.WriteLine(string.Join("\t", 
                        Model.get_angles2D()[i].ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].x.ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].y.ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].z.ToString("G", CultureInfo.InvariantCulture)));
                }
            }
            //File.WriteAllText(Path.Combine("Logs", "Distances2D", $"Output n={size}.txt"), res_string);
        }
    }
}