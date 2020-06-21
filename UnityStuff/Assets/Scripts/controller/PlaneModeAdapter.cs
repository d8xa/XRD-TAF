using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using model;
using UnityEngine;
using util;
using Debug = UnityEngine.Debug;
using Logger = util.Logger;
using Vector3 = UnityEngine.Vector3;


namespace controller
{
    public class PlaneModeAdapter : ShaderAdapter
    {

        private Vector3[,] _absorptionFactors;
        private float[] _cosAlpha;
        private int _nrSegments;
        private int _nrAnglesTheta;
        private int _nrAnglesAlpha;
        
        // Mask of diffraction points
        //private Vector3Int[] _diffractionMask;
        //private Vector3 _nrDiffractionPoints;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;
        
        public PlaneModeAdapter(
            ComputeShader shader, 
            Model model, 
            float margin, 
            bool writeFactorsFlag
            ) : base(shader, model, margin, writeFactorsFlag)
        {
            SetLogger(new Logger());
            _logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        public PlaneModeAdapter(ComputeShader shader, Model model) : base(shader, model)
        {
            SetLogger(new Logger());
            _logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }
        
        private void InitializeOtherFields()
        {
            _logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): started.");
            _nrAnglesTheta = Model.detector.resolution.x;
            _nrAnglesAlpha = Model.detector.resolution.y;
            _nrSegments = SegmentResolution * SegmentResolution;
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesAlpha, _nrAnglesTheta];

            // get indicator mask where each point diffracts in each case.
            //_diffractionMask = new Vector3Int[Coordinates.Length];
            ComputeIndicatorMask();
            
            // TODO: fix count
            // count diffracting points in each case.
            var mask = new Vector2Int[_nrSegments];
            _maskBuffer.GetData(mask);
            var indicatorCount = mask.AsParallel()
                .Aggregate(Vector2Int.zero, (a, v) => a + v);
            _logger.Log(Logger.EventType.Step, 
                $"InitializeOtherFields(): found {indicatorCount} diffraction points (of {_nrSegments}).");
            
            Shader.SetInts("indicatorCount", indicatorCount.x, indicatorCount.y, indicatorCount.y);
            _logger.Log(Logger.EventType.Step, "InitializeOtherFields(): Set indicatorCount in shader.");

            _logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): done.");
        }
        
        private void ComputeIndicatorMask()
        {
            _logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): started.");

            // prepare required variables.
            Shader.SetFloat("r_cell", Model.GetRCell());
            Shader.SetFloat("r_sample", Model.GetRSample());
            var maskHandle = Shader.FindKernel("getIndicatorMask");
            _inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            _maskBuffer = new ComputeBuffer(Coordinates.Length, sizeof(uint)*2);

            _inputBuffer.SetData(Coordinates);
            
            Shader.SetBuffer(maskHandle, "segment", _inputBuffer);
            Shader.SetBuffer(maskHandle, "indicatorMask", _maskBuffer);

            _logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader dispatch.");
            Shader.Dispatch(maskHandle, ThreadGroupsX, 1, 1);
            _logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader return.");

            _logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): done.");
        }

        protected override void Compute()
        {
            _logger.Log(Logger.EventType.Method, "Compute(): started.");

            var sw = new Stopwatch();
            sw.Start();

            // initialize parameters in shader.
            // necessary here already?
            Shader.SetFloats("mu", Model.GetMuCell(), Model.GetMuSample());
            Shader.SetFloat("r_cell", Model.GetRCell());
            Shader.SetFloat("r_sample", Model.GetRSample());
            Shader.SetFloat("r_cell_sq", Model.GetRCellSq());
            Shader.SetFloat("r_sample_sq", Model.GetRSampleSq());
            Shader.SetInt("bufCount_Segments", _nrSegments);
            _logger.Log(Logger.EventType.Step, "Set shader parameters.");
            
            
            // get kernel handles.
            var g1Handle = Shader.FindKernel("g1_dists");
            var g2Handle = Shader.FindKernel("g2_dists");
            var absorptionFactorsHandle = Shader.FindKernel("AbsorptionFactors");
            _logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");
            
            
            // make buffers.
            //var inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g1OutputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g1OutputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g2OutputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g2OutputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var cosBuffer = new ComputeBuffer(_nrAnglesAlpha, sizeof(float));
            var absorptionFactorsBuffer = new ComputeBuffer(_nrAnglesAlpha, sizeof(float)*3);
            _logger.Log(Logger.EventType.Data, "Created buffers.");
            
            
            // set buffers for g1 kernel.
            Shader.SetBuffer(g1Handle, "segment", _inputBuffer);
            Shader.SetBuffer(g1Handle, "g1DistancesOuter", g1OutputBufferInner);
            Shader.SetBuffer(g1Handle, "g1DistancesInner", g1OutputBufferOuter);
            _logger.Log(Logger.EventType.ShaderInteraction, "Wrote data to buffers.");
            
            _inputBuffer.SetData(Coordinates);
            
            // compute g1 distances.
            _logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel dispatch.");
            Shader.Dispatch(g1Handle, ThreadGroupsX, 1, 1);
            _logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel return.");
            
            var absorptionFactorColumn = new Vector3[_nrAnglesAlpha];
            //Array.Clear(absorptionFactorColumn, 0, absorptionFactorColumn.Length);
            
            cosBuffer.SetData(Model.GetCos3D());

            // set buffers for g2 kernel.
            Shader.SetBuffer(g2Handle, "segment", _inputBuffer);
            Shader.SetBuffer(g2Handle, "g2DistancesInner", g2OutputBufferInner);
            Shader.SetBuffer(g2Handle, "g2DistancesOuter", g2OutputBufferOuter);

            // set shared buffers for absorption factor kernel.
            Shader.SetBuffer(absorptionFactorsHandle, "segment", _inputBuffer);
            Shader.SetBuffer(absorptionFactorsHandle, "g1DistancesInner", g1OutputBufferInner);
            Shader.SetBuffer(absorptionFactorsHandle, "g1DistancesOuter", g1OutputBufferOuter);
            Shader.SetBuffer(absorptionFactorsHandle, "cosBuffer", cosBuffer);
            Shader.SetBuffer(absorptionFactorsHandle, "indicatorMask", _maskBuffer);

            var cosValues = new float[_nrAnglesAlpha];
            cosBuffer.GetData(cosValues);
            Debug.Log(string.Join(", ", 
                cosValues.Select(v => v.ToString("F3"))));
            


            var start_loop = sw.Elapsed;
            
            for (int j = 0; j < _nrAnglesTheta; j++)
            {
                // set rotation parameters.
                Shader.SetFloat("cos", (float) Math.Cos((180 - Model.GetAngles2D()[j]) * Math.PI / 180));
                Shader.SetFloat("sin", (float) Math.Sin((180 - Model.GetAngles2D()[j]) * Math.PI / 180));
                
                // compute g2 distances.
                _logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel dispatch.");
                Shader.Dispatch(g2Handle, ThreadGroupsX, 1, 1);
                _logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel return.");

                // set iterative buffers for absorption factors kernel.
                Shader.SetBuffer(absorptionFactorsHandle, "g2DistancesInner", g2OutputBufferInner);
                Shader.SetBuffer(absorptionFactorsHandle, "g2DistancesOuter", g2OutputBufferOuter);
                Shader.SetBuffer(absorptionFactorsHandle, "absorptionFactors", absorptionFactorsBuffer);
                
                Shader.Dispatch(absorptionFactorsHandle, ThreadGroupsX, 1, 1);
                absorptionFactorsBuffer.GetData(absorptionFactorColumn);
                
                /*
                var tmp1Count = absorptionFactorColumn
                    .Count(v => Math.Abs(v.x) > 0 || Math.Abs(v.y) > 0 || Math.Abs(v.z) > 0);
                Debug.Log(tmp1Count);
                
                if (tmp1Count > 0) Debug.Log(string.Join(", ", 
                    absorptionFactorColumn.Select(v => v.ToString("F3")).ToArray())
                );
                */
                
                var j1 = j;
                ParallelEnumerable.Range(0, _nrAnglesAlpha)
                    .ForAll(i => _absorptionFactors[i,j1] = absorptionFactorColumn[i]);
                // TODO: rewrite to save without copying from temporary column array. 
            }

            var loop_time = sw.Elapsed - start_loop;

            _logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");
            _logger.Log(Logger.EventType.Performance, $"Absorption factor calculation took {loop_time}.");
            // TODO: add performance measure log entry.
            
            // release buffers.
            _inputBuffer.Release();
            _maskBuffer.Release();
            g1OutputBufferOuter.Release();
            g1OutputBufferInner.Release();
            g2OutputBufferOuter.Release();
            g2OutputBufferInner.Release();
            cosBuffer.Release();
            absorptionFactorsBuffer.Release();
            _logger.Log(Logger.EventType.ShaderInteraction, "Shader buffers released.");
            
            sw.Stop();
            _logger.Log(Logger.EventType.Method, "Compute(): done.");
        }

        protected override void Write()
        {
            var saveDir = Path.Combine("Logs", "Absorptions3D");
            Directory.CreateDirectory(saveDir);
            var saveName = $"Output n={SegmentResolution}.txt";
            ArrayWriteTools.Write2D(Path.Combine(saveDir, saveName), _absorptionFactors);
        }
    }
}