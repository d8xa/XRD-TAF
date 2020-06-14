using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using model;
using UnityEngine;
using util;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Logger = util.Logger;

namespace controller
{
    public class PointModeAdapter : ShaderAdapter
    {
        private Vector3[] _absorptionFactors;
        private int _nrSegments;
        private int _nrAnglesTheta;
        
        // Mask of diffraction points
        private Vector2Int[] _diffractionMask;
        private Vector2 _nrDiffractionPoints;
        
        private ComputeBuffer _inputBuffer;
        
        public PointModeAdapter(
            ComputeShader shader,
            Model model,
            float margin,
            bool writeFactorsFlag
        ) : base(shader, model, margin, writeFactorsFlag)
        {
            SetLogger(new NullLogger());
            _logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        public PointModeAdapter(
            ComputeShader shader, 
            Model model
            ) : base(shader, model)
        {
            SetLogger(new Logger());
            _logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        private void InitializeOtherFields()
        {
            _logger.SetPrintLevel(Logger.LogLevel.All);
            _logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): started.");
            _nrAnglesTheta = Model.GetAngles2D().Length;
            _nrSegments = SegmentResolution * SegmentResolution;
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesTheta];

            // get indicator mask where each point diffracts in each case.
            _diffractionMask = new Vector2Int[Coordinates.Length];
            ComputeIndicatorMask();
            
            // count diffracting points in each case.
            _nrDiffractionPoints = _diffractionMask.AsParallel()
                .Aggregate(Vector2.zero, (a, v) => a + v);
            _logger.Log(Logger.EventType.Step, 
                $"InitializeOtherFields(): found {_nrDiffractionPoints} diffraction points (of {_nrSegments}).");
            
            _logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): done.");
        }

        private void ComputeIndicatorMask()
        {
            _logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): started.");

            // prepare required variables.
            Shader.SetFloat("r_cell", Model.GetRCell());
            Shader.SetFloat("r_sample", Model.GetRSample());
            Shader.SetInts("indicatorCount", 0, 0, 0);
            var maskHandle = Shader.FindKernel("getIndicatorMask");
            _inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var maskBuffer = new ComputeBuffer(Coordinates.Length, sizeof(uint)*2);

            _inputBuffer.SetData(Coordinates);
            maskBuffer.SetData(_diffractionMask);
            
            Shader.SetBuffer(maskHandle, "segment", _inputBuffer);
            Shader.SetBuffer(maskHandle, "indicatorMask", maskBuffer);

            _logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader dispatch.");
            Shader.Dispatch(maskHandle, ThreadGroupsX, 1, 1);
            _logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader return.");
            maskBuffer.GetData(_diffractionMask);
            
            maskBuffer.Release();

            _logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): done.");
        }

        protected override void Compute()
        {
            _logger.Log(Logger.EventType.Method, "Compute(): started.");

            var sw = new Stopwatch();
            sw.Start();
            
            // initialize g1 distance arrays.
            var g1DistsOuter = new Vector2[_nrSegments];
            var g1DistsInner = new Vector2[_nrSegments];
            Array.Clear(g1DistsOuter, 0, _nrSegments);    // necessary ? 
            Array.Clear(g1DistsInner, 0, _nrSegments);    // necessary ? 
            _logger.Log(Logger.EventType.Step, "Initialized g1 distance arrays.");

            // initialize parameters in shader.
            // necessary here already?
            Shader.SetFloats("mu", Model.GetMuCell(), Model.GetMuSample());
            Shader.SetFloat("r_cell", Model.GetRCell());
            Shader.SetFloat("r_sample", Model.GetRSample());
            Shader.SetFloat("r_cell_sq", Model.GetRCellSq());
            Shader.SetFloat("r_sample_sq", Model.GetRSampleSq());
            _logger.Log(Logger.EventType.Step, "Set shader parameters.");


            // get kernel handles.
            var g1Handle = Shader.FindKernel("g1_dists");
            var absorptionsHandle = Shader.FindKernel("Absorptions");
            _logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");

 
            // make buffers.
            //var _inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            //maskBuffer = new ComputeBuffer(Coordinates.Length, sizeof(uint)*3);
            var outputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var outputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*3);
            _logger.Log(Logger.EventType.Data, "Created buffers.");
            
            _inputBuffer.SetData(Coordinates);
            
            // TODO: handle case threadGroupsX > 1024.

            // set buffers for g1 kernel.
            Shader.SetBuffer(g1Handle, "segment", _inputBuffer);
            Shader.SetBuffer(g1Handle, "distancesInner", outputBufferInner);
            Shader.SetBuffer(g1Handle, "distancesOuter", outputBufferOuter);
            _logger.Log(Logger.EventType.ShaderInteraction, "Wrote data to buffers.");

            
            // compute g1 distances.
            _logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel dispatch.");
            Shader.Dispatch(g1Handle, ThreadGroupsX, 1, 1);
            _logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel return.");
            
                     
            var loopTs = new TimeSpan();
            var factorsTs = new TimeSpan();
            var absorptions = new Vector3[_nrSegments];
            Array.Clear(absorptions, 0, absorptions.Length);
            
            Shader.SetBuffer(absorptionsHandle, "segment", _inputBuffer);

            // for each angle:
            for (int j = 0; j < _nrAnglesTheta; j++) {
                // TODO: check if g2 kernel can access filled distances buffer of g1 kernel.
                var loopStart = sw.Elapsed;

                // set coordinate buffer. remove?
                Shader.SetFloat("cos", (float) Math.Cos((180 - Model.GetAngles2D()[j]) * Math.PI / 180));
                Shader.SetFloat("sin", (float) Math.Sin((180 - Model.GetAngles2D()[j]) * Math.PI / 180));
                Shader.SetBuffer(absorptionsHandle, "distancesInner", outputBufferInner);
                Shader.SetBuffer(absorptionsHandle, "distancesOuter", outputBufferOuter);
                Shader.SetBuffer(absorptionsHandle, "absorptions", absorptionsBuffer);
                
                Shader.Dispatch(absorptionsHandle, ThreadGroupsX, 1, 1);
                

                absorptionsBuffer.GetData(absorptions);
                //Debug.Log(string.Join(", ", 
                  //  absorptions.Select(v => v.ToString("G"))));
                var factorStart = sw.Elapsed;
                _absorptionFactors[j] = GetAbsorptionFactor(absorptions);
                var factorStop = sw.Elapsed;
                factorsTs += factorStop - factorStart;
                
                var loopStop = sw.Elapsed;
                loopTs += loopStop - loopStart;
            }
            
            _logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");
            _logger.Log(Logger.EventType.Performance, 
                $"Absorption calculation took {loopTs}, " +
                $"{TimeSpan.FromTicks(loopTs.Ticks/_nrAnglesTheta)} avg. per loop, " + 
                $"{TimeSpan.FromTicks(factorsTs.Ticks/_nrAnglesTheta)} of which for absorption factor calculation.");
            
            // release buffers.
            _inputBuffer.Release();
            outputBufferOuter.Release();
            outputBufferInner.Release();
            absorptionsBuffer.Release();
            _logger.Log(Logger.EventType.ShaderInteraction, "Shader buffers released.");

            sw.Stop();
            _logger.Log(Logger.EventType.Method, "Compute(): done.");
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
                    .Where(i => _diffractionMask[i].y > 0)
                    .Select(i => absorptions[i].z)
                    .Sum() / _nrDiffractionPoints.y
            );
        }

        protected override void Write()
        {
            if (WriteFactorsFlag) WriteAbsorptionFactors();
        }

        public void WriteAbsorptionFactors()
        {
            var path = Path.Combine("Logs", "Absorptions2D", $"Output n={SegmentResolution}.txt");
            var headRow = string.Join("\t", "2 theta", "A_{s,sc}", "A_{c,sc}", "A_{c,c}");
            var headCol = Model.GetAngles2D()
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
            
            ArrayWriteTools.Write2D(path, headCol, headRow, data);
        }
        
        private void WriteAbsorptionFactorsNative()
        {
            _logger.Log(Logger.EventType.Method, "WriteAbsorptionFactors(): started.");

            using (FileStream fileStream = File.Create(Path.Combine("Logs", "Absorptions2D", $"Output n={SegmentResolution}.txt")))
            using (BufferedStream buffered = new BufferedStream(fileStream))
            using (StreamWriter writer = new StreamWriter(buffered))
            {
                writer.WriteLine(string.Join("\t", "2 theta","A_{s,sc}", "A_{c,sc}", "A_{c,c}"));
                for (int i = 0; i < _nrAnglesTheta; i++)
                {
                    writer.WriteLine(string.Join("\t", 
                        Model.GetAngles2D()[i].ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].x.ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].y.ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].z.ToString("G", CultureInfo.InvariantCulture)));
                }
            }
            
            _logger.Log(Logger.EventType.Method, "WriteAbsorptionFactors(): done.");
        }
    }
}