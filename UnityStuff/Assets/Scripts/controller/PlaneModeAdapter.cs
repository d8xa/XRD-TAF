using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using FoPra.model;
using UnityEngine;
using util;
using Logger = util.Logger;


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
            _nrAnglesTheta = // TODO
            _nrAnglesAlpha = // TODO
            _nrSegments = SegmentResolution * SegmentResolution;
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesTheta, _nrAnglesAlpha];

            // get indicator mask where each point diffracts in each case.
            //_diffractionMask = new Vector3Int[Coordinates.Length];
            ComputeIndicatorMask();
            
            // count diffracting points in each case.
            //_nrDiffractionPoints = _diffractionMask.AsParallel()
              //  .Aggregate(Vector3Int.zero, (a, v) => a + v);
            //_logger.Log(Logger.EventType.Step, 
              //  $"InitializeOtherFields(): found {_nrDiffractionPoints} diffraction points (of {_nrSegments}).");

            _logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): done.");
        }
        
        private void ComputeIndicatorMask()
        {
            _logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): started.");

            // prepare required variables.
            Shader.SetFloat("r_cell", Model.get_r_cell());
            Shader.SetFloat("r_sample", Model.get_r_sample());
            Shader.SetInts("indicatorCount", 0, 0, 0);
            var maskHandle = Shader.FindKernel("getIndicatorMask");
            _inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            _maskBuffer = new ComputeBuffer(Coordinates.Length, sizeof(uint)*3);

            _inputBuffer.SetData(Coordinates);
            //maskBuffer.SetData(_diffractionMask);    // TODO: check if necessary.
            
            Shader.SetBuffer(maskHandle, "segment", _inputBuffer);
            Shader.SetBuffer(maskHandle, "indicatorMask", _maskBuffer);

            _logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader dispatch.");
            Shader.Dispatch(maskHandle, ThreadGroupsX, 1, 1);
            _logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader return.");
            //_maskBuffer.GetData(_diffractionMask);

            //_inputBuffer.Release();
            //_maskBuffer.Release();
            
            _logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): done.");
        }

        protected override void Compute()
        {
            _logger.Log(Logger.EventType.Method, "Compute(): started.");

            var sw = new Stopwatch();
            sw.Start();
            
            
            // initialize g1 distance arrays.
            //var g1DistsOuter = new Vector2[_nrSegments];
            //var g1DistsInner = new Vector2[_nrSegments];
            //Array.Clear(g1DistsOuter, 0, _nrSegments);    // necessary ? 
            //Array.Clear(g1DistsInner, 0, _nrSegments);    // necessary ? 
            //_logger.Log(Logger.EventType.Step, "Initialized g1 distance arrays.");

            
            // initialize parameters in shader.
            // necessary here already?
            Shader.SetFloats("mu", Model.get_mu_cell(), Model.get_mu_sample());
            Shader.SetFloat("r_cell", Model.get_r_cell());
            Shader.SetFloat("r_sample", Model.get_r_sample());
            Shader.SetFloat("r_cell_sq", Model.get_r_cell_sq());
            Shader.SetFloat("r_sample_sq", Model.get_r_sample_sq());
            Shader.SetInt("bufCount_Segments", _nrSegments);
            _logger.Log(Logger.EventType.Step, "Set shader parameters.");
            
            
            // get kernel handles.
            var g1Handle = Shader.FindKernel("g1_dists");
            var g2Handle = Shader.FindKernel("g2_dists");
            //var absorptionsHandle = Shader.FindKernel("Absorptions");
            var absorptionFactorsHandle = Shader.FindKernel("AbsorptionFactors");
            _logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");
            
            
            // make buffers.
            var inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g1OutputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g1OutputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g2OutputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g2OutputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            //var absorptionsBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*3);
            var absorptionFactorsBuffer = new ComputeBuffer(_nrAnglesAlpha, sizeof(float)*3);
            _logger.Log(Logger.EventType.Data, "Created buffers.");
            
            
            // set buffers for g1 kernel.
            Shader.SetBuffer(g1Handle, "segment", inputBuffer);
            Shader.SetBuffer(g1Handle, "g1DistancesOuter", g1OutputBufferInner);
            Shader.SetBuffer(g1Handle, "g1DistancesInner", g1OutputBufferOuter);
            _logger.Log(Logger.EventType.ShaderInteraction, "Wrote data to buffers.");
            
            inputBuffer.SetData(Coordinates);
            
            // compute g1 distances.
            _logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel dispatch.");
            Shader.Dispatch(g1Handle, ThreadGroupsX, 1, 1);
            _logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel return.");
            
            var absorptionFactorColumn = new Vector3[_nrAnglesAlpha];
            //Array.Clear(absorptionFactorColumn, 0, absorptionFactorColumn.Length);

            for (int j = 0; j < _nrAnglesTheta; j++)
            {
                // set rotation parameters.
                Shader.SetFloat("cos", (float) Math.Cos((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                Shader.SetFloat("sin", (float) Math.Sin((180 - Model.get_angles2D()[j]) * Math.PI / 180));
                
                // set buffers for g2 kernel.
                Shader.SetBuffer(g2Handle, "segment", inputBuffer);
                Shader.SetBuffer(g2Handle, "g1DistancesInner", g1OutputBufferInner);
                Shader.SetBuffer(g2Handle, "g1DistancesOuter", g1OutputBufferOuter);
                Shader.SetBuffer(g2Handle, "g2DistancesInner", g2OutputBufferInner);
                Shader.SetBuffer(g2Handle, "g2DistancesOuter", g2OutputBufferOuter);
                
                // set buffers for absorption factors kernel.
                Shader.SetBuffer(absorptionFactorsHandle, "segment", inputBuffer);
                Shader.SetBuffer(absorptionFactorsHandle, "g1DistancesInner", g1OutputBufferInner);
                Shader.SetBuffer(absorptionFactorsHandle, "g1DistancesOuter", g1OutputBufferOuter);
                Shader.SetBuffer(absorptionFactorsHandle, "g2DistancesInner", g2OutputBufferInner);
                Shader.SetBuffer(absorptionFactorsHandle, "g2DistancesOuter", g2OutputBufferOuter);
                Shader.SetBuffer(absorptionFactorsHandle, "absorptionFactors", absorptionFactorsBuffer);
                
                Shader.Dispatch(absorptionFactorsHandle, ThreadGroupsX, 1, 1);
                absorptionFactorsBuffer.GetData(absorptionFactorColumn);
                var j1 = j;
                ParallelEnumerable.Range(0, _nrAnglesAlpha)
                    .ForAll(i => _absorptionFactors[i,j1] = absorptionFactorColumn[i]);
                // TODO: rewrite to save without copying from temporary column array. 
            }
            
            _logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");
            // TODO: add performance measure log entry.
            
            // release buffers.
            inputBuffer.Release();
            g1OutputBufferOuter.Release();
            g1OutputBufferInner.Release();
            g2OutputBufferOuter.Release();
            g2OutputBufferInner.Release();
            absorptionFactorsBuffer.Release();
            _logger.Log(Logger.EventType.ShaderInteraction, "Shader buffers released.");
            
            sw.Stop();
            _logger.Log(Logger.EventType.Method, "Compute(): done.");
        }

        protected override void Write()
        { 
            // TODO: create path if not exists.
            var path = Path.Combine("Logs", "Absorptions3D", $"Output n={SegmentResolution}.txt");
            ArrayWriteTools.Write2D(path, _absorptionFactors);
        }

        private void WriteAbsorptionFactors()
        {
            throw new NotImplementedException();
        }
    }
}