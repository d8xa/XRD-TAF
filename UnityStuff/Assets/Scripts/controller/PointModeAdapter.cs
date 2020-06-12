using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using FoPra.model;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace controller
{
    public class PointModeAdapter : ShaderAdapter
    {
        private Vector3[] _absorptionFactors;
        private int _nrSegments;
        private int _nrAnglesTheta;
        
        // Mask of diffraction points
        private Vector3Int[] _diffractionMask;
        private Vector3 _nrDiffractionPoints;

        public PointModeAdapter(
            ComputeShader shader,
            Model model,
            float margin,
            bool writeDistances,
            bool writeFactors
        ) : base(shader, model, margin, writeDistances, writeFactors)
        {
            InitializeOtherFields();
        }

        public PointModeAdapter(
            ComputeShader shader, 
            Model model
            ) : base(shader, model)
        {
            InitializeOtherFields();
        }

        private void InitializeOtherFields()
        {
            _nrAnglesTheta = Model.get_angles2D().Length;
            _nrSegments = SegmentResolution * SegmentResolution;
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesTheta];

            // get indicator mask where each point diffracts in each case.
            _diffractionMask = new Vector3Int[Coordinates.Length];
            ComputeIndicatorMask();
            
            // count diffracting points in each case.
            _nrDiffractionPoints = _diffractionMask.AsParallel()
                .Aggregate(Vector3.zero, (a, v) => a + v);
            
            Debug.Log($"Indicator count: {_nrDiffractionPoints}");
        }

        private void ComputeIndicatorMask()
        {
            Shader.SetFloat("r_cell", Model.get_r_cell());
            Shader.SetFloat("r_sample", Model.get_r_sample());
            Shader.SetInts("indicatorCount", 0, 0, 0);
            var maskHandle = Shader.FindKernel("getIndicatorMask");
            var inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var maskBuffer = new ComputeBuffer(Coordinates.Length, sizeof(uint)*3);

            inputBuffer.SetData(Coordinates);
            maskBuffer.SetData(_diffractionMask);
            
            Shader.SetBuffer(maskHandle, "segment", inputBuffer);
            Shader.SetBuffer(maskHandle, "indicatorMask", maskBuffer);

            Shader.Dispatch(maskHandle, ThreadGroupsX, 1, 1);
            maskBuffer.GetData(_diffractionMask);
            
            inputBuffer.Release();
            maskBuffer.Release();
        }

        protected override void Compute()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            // initialize g1 distance arrays.
            var g1DistsOuter = new Vector2[_nrSegments];
            var g1DistsInner = new Vector2[_nrSegments];
            Array.Clear(g1DistsOuter, 0, _nrSegments);    // necessary ? 
            Array.Clear(g1DistsInner, 0, _nrSegments);    // necessary ? 
            Debug.Log($"{sw.Elapsed}: Initialized g1 distance arrays.");

            // initialize parameters in shader.
            // necessary here already?
            Shader.SetFloats("mu", Model.get_mu_cell(), Model.get_mu_sample());
            Shader.SetFloat("r_cell", Model.get_r_cell());
            Shader.SetFloat("r_sample", Model.get_r_sample());
            Shader.SetFloat("r_cell_sq", Model.get_r_cell_sq());
            Shader.SetFloat("r_sample_sq", Model.get_r_sample_sq());
            Debug.Log($"{sw.Elapsed}: Set shader parameters.");


            // get kernel handles.
            var g1Handle = Shader.FindKernel("g1_dists");
            var absorptionsHandle = Shader.FindKernel("Absorptions");
            Debug.Log($"{sw.Elapsed}: Retrieved kernel handles.");

 
            // make buffers.
            var inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var outputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var outputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*3);
            Debug.Log($"{sw.Elapsed}: Created buffers.");


            // set thread groups on X axis
            // TODO: handle case threadGroupsX > 1024.

            
            // set buffers for g1 kernel.
            Shader.SetBuffer(g1Handle, "segment", inputBuffer);
            Shader.SetBuffer(g1Handle, "distancesInner", outputBufferInner);
            Shader.SetBuffer(g1Handle, "distancesOuter", outputBufferOuter);
            Debug.Log($"{sw.Elapsed}: Wrote data to buffers.");

            
            // compute g1 distances.
            Shader.Dispatch(g1Handle, ThreadGroupsX, 1, 1);
            Debug.Log($"{sw.Elapsed}: Calculated g1 distances.");
            

            var loop_ts = new TimeSpan();
            var factors_ts = new TimeSpan();
            var absorptions = new Vector3[_nrSegments];
            Array.Clear(absorptions, 0, absorptions.Length);
            
            // for each angle:
            for (int j = 0; j < _nrAnglesTheta; j++) {
                // TODO: check if g2 kernel can access filled distances buffer of g1 kernel.
                var loopStart = sw.Elapsed;

                // set coordinate buffer. remove?
                Shader.SetBuffer(absorptionsHandle, "segment", inputBuffer);
                Shader.SetFloat("cos", (float) Math.Cos((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                Shader.SetFloat("sin", (float) Math.Sin((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                Shader.SetBuffer(absorptionsHandle, "distancesInner", outputBufferInner);
                Shader.SetBuffer(absorptionsHandle, "distancesOuter", outputBufferOuter);
                Shader.SetBuffer(absorptionsHandle, "absorptions", absorptionsBuffer);
                Shader.Dispatch(absorptionsHandle, ThreadGroupsX, 1, 1);
                

                absorptionsBuffer.GetData(absorptions);
                var factor_start = sw.Elapsed;
                _absorptionFactors[j] =
                    //GetAbsorptionFactorLINQ(absorptions, Vector3.kEpsilon);
                    GetAbsorptionFactor(absorptions);
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

            if (WriteFactors)
            {
                Debug.Log($"{sw.Elapsed}: Started writing data to disk.");
                WriteAbsorptionFactors();
                Debug.Log($"{sw.Elapsed}: Finished writing data to disk.");
            }
            
            sw.Stop();
        }

        protected override void Write()
        {
            var path = Path.Combine("Logs", "Absorptions2D", $"Output n={SegmentResolution}.txt");
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

        private Vector3 GetAbsorptionFactorNaive(Vector3[] absorptions)
        {
            // TODO: make thread safe.
            var absorptionSum = new[]{0.0f, 0.0f, 0.0f};
            var count = new uint[]{0,0,0};

            for (int i = 0; i < _nrSegments; i++)
            {
                double norm = Vector3.Magnitude(Coordinates[i]);
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

            return new Vector3(
                    absorptionSum[0] / count[0],
                    absorptionSum[1] / count[1],
                    absorptionSum[2] / count[2]
                    );
        }

        private Vector3 GetAbsorptionFactor(Vector3[] absorptions)
        {
            return new Vector3(
                ParallelEnumerable.Range(0, absorptions.Length)
                    .Where(i => _diffractionMask[i].x > 0)
                    .Select(i => absorptions[i].x)
                    .Sum() / _nrDiffractionPoints.x,
                ParallelEnumerable.Range(0, absorptions.Length)
                    .Where(i => _diffractionMask[i].y > 0)
                    .Select(i => absorptions[i].y)
                    .Sum() / _nrDiffractionPoints.y,
                ParallelEnumerable.Range(0, absorptions.Length)
                    .Where(i => _diffractionMask[i].z > 0)
                    .Select(i => absorptions[i].z)
                    .Sum() / _nrDiffractionPoints.z
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

        private int IsContained(float norm, int @case)
        {
            if (@case == 0)
                return norm <= Model.get_r_cell() && norm <= Model.get_r_sample() ? 1 : 0;
            else
                return norm <= Model.get_r_cell() && norm > Model.get_r_sample() ? 1 : 0;
        }

        private Vector3Int IsContained(Vector3 v)
        {
            var norm = v.magnitude;
            return new Vector3Int(
                IsContained(norm, 0), 
                IsContained(norm, 1), 
                IsContained(norm, 2)
                );
        }
        
        
        private void WriteAbsorptionFactors()
        {
            using (FileStream fileStream = File.Create(Path.Combine("Logs", "Absorptions2D", $"Output n={SegmentResolution}.txt")))
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