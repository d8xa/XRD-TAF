using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using model;
using UnityEngine;
using util;
using Debug = UnityEngine.Debug;
using Logger = util.Logger;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace controller
{
    public class TestModeAdapter : ShaderAdapter
    {

        private Vector3[,] _absorptionFactors;
        private float[] _cosAlpha;
        private int _nrSegments;
        private int _nrAnglesTheta;
        private int _nrAnglesAlpha;
        
        // Mask of diffraction points
        //private Vector3Int[] _diffractionMask;
        private Vector2 _nrDiffractionPoints;
        private int[] _innerIndices;
        private int[] _outerIndices;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;
        
        public TestModeAdapter(
            ComputeShader shader, 
            Model model, 
            float margin, 
            bool writeFactorsFlag
            ) : base(shader, model, margin, writeFactorsFlag)
        {
            SetLogger(new Logger());
            logger.SetPrintFilter(new List<Logger.EventType>() 
                {
                    Logger.EventType.Performance, 
                    //Logger.EventType.Class, 
                    //Logger.EventType.InitializerMethod
                }
            );
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        public TestModeAdapter(ComputeShader shader, Model model) : base(shader, model)
        {
            SetLogger(new Logger());
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }
        
        private void InitializeOtherFields()
        {
            logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): started.");
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
            _innerIndices = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].x > 0.0)
                .ToArray();
            _outerIndices = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].y > 0.0)
                .ToArray();
            _nrDiffractionPoints = new Vector2(_innerIndices.Length, _outerIndices.Length);
            logger.Log(Logger.EventType.Step, 
                $"InitializeOtherFields(): found {_nrDiffractionPoints} diffraction points (of {_nrSegments}).");
            
            logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): done.");
        }
        
        private void ComputeIndicatorMask()
        {
            logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): started.");

            // prepare required variables.
            Shader.SetFloat("r_cell", Model.GetRCell());
            Shader.SetFloat("r_sample", Model.GetRSample());
            var maskHandle = Shader.FindKernel("getIndicatorMask");
            _inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            _maskBuffer = new ComputeBuffer(Coordinates.Length, sizeof(uint)*2);

            _inputBuffer.SetData(Coordinates);
            
            Shader.SetBuffer(maskHandle, "segment", _inputBuffer);
            Shader.SetBuffer(maskHandle, "indicatorMask", _maskBuffer);

            logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader dispatch.");
            Shader.Dispatch(maskHandle, ThreadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader return.");

            logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): done.");
        }

        protected override void Compute()
        {
            logger.Log(Logger.EventType.Method, "Compute(): started.");

            var sw = new Stopwatch();
            sw.Start();

            // initialize parameters in shader.
            // necessary here already?
            Shader.SetFloats("mu", Model.GetMuCell(), Model.GetMuSample());
            Shader.SetFloat("r_cell", Model.GetRCell());
            Shader.SetFloat("r_sample", Model.GetRSample());
            Shader.SetFloat("r_cell_sq", Model.GetRCellSq());
            Shader.SetFloat("r_sample_sq", Model.GetRSampleSq());
            logger.Log(Logger.EventType.Step, "Set shader parameters.");
            
            
            // get kernel handles.
            var g1Handle = Shader.FindKernel("g1_dists");
            var g2Handle = Shader.FindKernel("g2_dists");
            var absorptionsHandle = Shader.FindKernel("Absorptions");
            logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");
            
            
            // make buffers.
            //var inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g1OutputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g1OutputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g2OutputBufferOuter = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g2OutputBufferInner = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*3);
            logger.Log(Logger.EventType.Data, "Created buffers.");
            
            
            // set buffers for g1 kernel.
            Shader.SetBuffer(g1Handle, "segment", _inputBuffer);
            Shader.SetBuffer(g1Handle, "g1DistancesOuter", g1OutputBufferInner);
            Shader.SetBuffer(g1Handle, "g1DistancesInner", g1OutputBufferOuter);
            logger.Log(Logger.EventType.ShaderInteraction, "Wrote data to buffers.");
            
            _inputBuffer.SetData(Coordinates);
            
            // compute g1 distances.
            logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel dispatch.");
            Shader.Dispatch(g1Handle, ThreadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel return.");
            
            //Array.Clear(absorptionFactorColumn, 0, absorptionFactorColumn.Length);
            
            // set buffers for g2 kernel.
            Shader.SetBuffer(g2Handle, "segment", _inputBuffer);
            Shader.SetBuffer(g2Handle, "g2DistancesInner", g2OutputBufferInner);
            Shader.SetBuffer(g2Handle, "g2DistancesOuter", g2OutputBufferOuter);

            // set shared buffers for absorption factor kernel.
            Shader.SetBuffer(absorptionsHandle, "segment", _inputBuffer);
            Shader.SetBuffer(absorptionsHandle, "g1DistancesInner", g1OutputBufferInner);
            Shader.SetBuffer(absorptionsHandle, "g1DistancesOuter", g1OutputBufferOuter);
            Shader.SetBuffer(absorptionsHandle, "absorptions", absorptionsBuffer);
            Shader.SetBuffer(absorptionsHandle, "indicatorMask", _maskBuffer);

            var absorptionsTemp = new Vector3[Coordinates.Length];
            Array.Clear(absorptionsTemp, 0, absorptionsTemp.Length);
            absorptionsBuffer.SetData(absorptionsTemp);

            var total_outer_loop = sw.Elapsed;
            var avg_inner_loop = sw.Elapsed;
            
            
            for (int j = 0; j < _nrAnglesTheta; j++)
            {
                // set rotation parameters.
                Shader.SetFloat("cos", (float) Math.Cos((180 - Model.GetAngles()[j]) * Math.PI / 180));
                Shader.SetFloat("sin", (float) Math.Sin((180 - Model.GetAngles()[j]) * Math.PI / 180));
                
                // compute g2 distances.
                logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel dispatch.");
                Shader.Dispatch(g2Handle, ThreadGroupsX, 1, 1);
                logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel return.");

                // set iterative buffers for absorption factors kernel.
                Shader.SetBuffer(absorptionsHandle, "g2DistancesInner", g2OutputBufferInner);
                Shader.SetBuffer(absorptionsHandle, "g2DistancesOuter", g2OutputBufferOuter);

                for (int i = 0; i < _nrAnglesAlpha; i++)
                {
                    var start_inner_loop = sw.Elapsed;
                    
                    Shader.SetFloat("vCos", Model.GetCos3D()[i]);
                    Shader.SetBuffer(absorptionsHandle, "absorptionFactors", absorptionsBuffer);
                    Shader.Dispatch(absorptionsHandle, ThreadGroupsX, 1, 1);
                    absorptionsBuffer.GetData(absorptionsTemp);

                    _absorptionFactors[i, j] = GetAbsorptionFactor(absorptionsTemp);
                    
                    avg_inner_loop += sw.Elapsed - start_inner_loop;
                }
            }

            avg_inner_loop = TimeSpan.FromTicks(avg_inner_loop.Ticks/_nrSegments);
            total_outer_loop = sw.Elapsed - total_outer_loop;

            logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");
            logger.Log(Logger.EventType.Performance, 
                $"Absorption factor calculation took {total_outer_loop}"
                + $", {avg_inner_loop} on avg. for each inner loop (Column)"
                + ".");
            
            // release buffers.
            _inputBuffer.Release();
            _maskBuffer.Release();
            g1OutputBufferOuter.Release();
            g1OutputBufferInner.Release();
            g2OutputBufferOuter.Release();
            g2OutputBufferInner.Release();
            absorptionsBuffer.Release();
            logger.Log(Logger.EventType.ShaderInteraction, "Shader buffers released.");
            
            sw.Stop();
            logger.Log(Logger.EventType.Method, "Compute(): done.");
        }

        protected override void Write()
        {
            var saveDir = Path.Combine("Logs", "Absorptions3D");
            Directory.CreateDirectory(saveDir);
            var saveName = $"Output res={SegmentResolution}, n={_nrAnglesTheta}, m={_nrAnglesAlpha}.txt";
            ArrayWriteTools.Write2D(Path.Combine(saveDir, saveName), _absorptionFactors);
        }

        protected override void Cleanup()
        {
            _inputBuffer.Release();
            _maskBuffer.Release();
        }

        private Vector3 GetAbsorptionFactor(Vector3[] absorptions)
        {
            return new Vector3(
                _innerIndices.AsParallel().Select(i => absorptions[i].x).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].y).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].z).Average()
            );
        }
    }
}