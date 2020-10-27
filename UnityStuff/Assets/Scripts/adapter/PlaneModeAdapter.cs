using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using model;
using UnityEngine;
using util;
using Logger = util.Logger;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace adapter
{
    public class PlaneModeAdapter : ShaderAdapter
    {
        #region Fields

        private Vector3[,] _absorptionFactors;
        private int _nrSegments;
        private int _nrAnglesTheta;
        private int _nrAnglesAlpha;

        private float[] angles;
        private float[] vCosines;

        private Vector2 _nrDiffractionPoints;
        private int[] _innerIndices;
        private int[] _outerIndices;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;

        #endregion

        #region Constructors

        public PlaneModeAdapter(
            ComputeShader shader, 
            Properties properties,
            bool writeFactors,
            Logger customLogger
        ) : base(shader, properties, writeFactors, customLogger)
        {
            if (logger == null) SetLogger(new Logger());
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        #endregion

        #region Methods

        private void InitializeOtherFields()
        {
            logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): started.");
            SetStatusMessage($"Step 1/{(writeFactors ? 4 : 3)}: Initializing...");
            
            _nrAnglesTheta = properties.detector.resolution.x;
            _nrAnglesAlpha = properties.detector.resolution.y;
            _nrSegments = sampleResolution * sampleResolution;
            
            angles = Enumerable.Range(0, properties.detector.resolution.x)
                .Select(j => properties.detector.GetRatioFromIndex(j, false))
                .Select(v => properties.detector.GetAngleFromRatio(v))
                .ToArray();
            vCosines = Enumerable.Range(0, properties.detector.resolution.y)
                .Select(j => (float) properties.detector.GetRatioFromIndex(j, true))
                .ToArray();
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesAlpha, _nrAnglesTheta];

            // get indicator mask where each point diffracts in each case.
            //_diffractionMask = new Vector3Int[Coordinates.Length];
            ComputeIndicatorMask();
            
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
            SetStatusMessage($"Step 2/{(writeFactors ? 4 : 3)}: Computing indicator mask...");

            // prepare required variables.
            SetSharedParameters();
            var maskHandle = shader.FindKernel("get_indicator");
            _inputBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            _maskBuffer = new ComputeBuffer(coordinates.Length, sizeof(uint)*2);

            _inputBuffer.SetData(coordinates);
            
            shader.SetBuffer(maskHandle, "segment", _inputBuffer);
            shader.SetBuffer(maskHandle, "indicatorMask", _maskBuffer);

            logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader dispatch.");
            shader.Dispatch(maskHandle, threadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader return.");

            logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): done.");
        }
        
        private void SetSharedParameters()
        {
            shader.SetFloats("mu", mu.cell, mu.sample);
            shader.SetFloats("r", r.cell, r.sample);
            shader.SetFloats("r2", rSq.cell, rSq.sample);
            shader.SetFloats("ray_dim", properties.ray.dimensions.x / 2, properties.ray.dimensions.y / 2);
        }

        protected override void Compute()
        {
            logger.Log(Logger.EventType.Method, "Compute(): started.");
            SetStatusMessage($"Step 3/{(writeFactors ? 4 : 3)}: Computing absorption factors...");

            var sw = new Stopwatch();
            sw.Start();

            // initialize parameters in shader.
            // SetSharedParameters();
            logger.Log(Logger.EventType.Step, "Set shader parameters.");
            
            
            // get kernel handles.
            var g1Handle = shader.FindKernel("dists_part1");
            var g2Handle = shader.FindKernel("dists_part2");
            var absorptionsHandle = shader.FindKernel("get_absorptions");
            logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");
            
            
            // make buffers.
            //var inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            var g1OutputBufferOuter = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var g1OutputBufferInner = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var g2OutputBufferOuter = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var g2OutputBufferInner = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*3);
            logger.Log(Logger.EventType.Data, "Created buffers.");
            
            
            // set buffers for g1 kernel.
            shader.SetBuffer(g1Handle, "segment", _inputBuffer);
            shader.SetBuffer(g1Handle, "g1DistancesInner", g1OutputBufferInner);
            shader.SetBuffer(g1Handle, "g1DistancesOuter", g1OutputBufferOuter);
            logger.Log(Logger.EventType.ShaderInteraction, "Wrote data to buffers.");
            
            _inputBuffer.SetData(coordinates);
            
            // compute g1 distances.
            logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel dispatch.");
            shader.Dispatch(g1Handle, threadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel return.");
            
            //Array.Clear(absorptionFactorColumn, 0, absorptionFactorColumn.Length);
            
            // set buffers for g2 kernel.
            shader.SetBuffer(g2Handle, "segment", _inputBuffer);
            shader.SetBuffer(g2Handle, "g2DistancesInner", g2OutputBufferInner);
            shader.SetBuffer(g2Handle, "g2DistancesOuter", g2OutputBufferOuter);

            // set shared buffers for absorption factor kernel.
            shader.SetBuffer(absorptionsHandle, "segment", _inputBuffer);
            shader.SetBuffer(absorptionsHandle, "g1DistancesInner", g1OutputBufferInner);
            shader.SetBuffer(absorptionsHandle, "g1DistancesOuter", g1OutputBufferOuter);
            shader.SetBuffer(absorptionsHandle, "absorptions", absorptionsBuffer);
            shader.SetBuffer(absorptionsHandle, "indicatorMask", _maskBuffer);

            var absorptionsTemp = new Vector3[coordinates.Length];
            Array.Clear(absorptionsTemp, 0, absorptionsTemp.Length);
            absorptionsBuffer.SetData(absorptionsTemp);

            var totalOuterLoop = sw.Elapsed;
            var avgInnerLoop = sw.Elapsed;
            
            for (int j = 0; j < _nrAnglesTheta; j++)
            {
                // set rotation parameters.
                shader.SetFloats("rot", (float) Math.Cos(Math.PI - GetThetaAt(j)),
                    (float) Math.Sin(Math.PI - GetThetaAt(j)));
                
                // compute g2 distances.
                logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel dispatch.");
                shader.Dispatch(g2Handle, threadGroupsX, 1, 1);
                logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel return.");

                // set iterative buffers for absorption factors kernel.
                shader.SetBuffer(absorptionsHandle, "g2DistancesInner", g2OutputBufferInner);
                shader.SetBuffer(absorptionsHandle, "g2DistancesOuter", g2OutputBufferOuter);

                for (int i = 0; i < _nrAnglesAlpha; i++)
                {
                    var startInnerLoop = sw.Elapsed;
                    
                    shader.SetFloat("stretch", vCosines[i]);
                    shader.SetBuffer(absorptionsHandle, "absorptionFactors", absorptionsBuffer);
                    shader.Dispatch(absorptionsHandle, threadGroupsX, 1, 1);
                    absorptionsBuffer.GetData(absorptionsTemp);

                    _absorptionFactors[i, j] = GetAbsorptionFactor(absorptionsTemp);
                    
                    avgInnerLoop += sw.Elapsed - startInnerLoop;
                }
            }

            avgInnerLoop = TimeSpan.FromTicks(avgInnerLoop.Ticks/_nrSegments);
            totalOuterLoop = sw.Elapsed - totalOuterLoop;

            logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");
            logger.Log(Logger.EventType.Performance, 
                $"Absorption factor calculation took {totalOuterLoop}"
                + $", {avgInnerLoop} on avg. for each inner loop (Column)"
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
            SetStatusMessage($"Step 3/{(writeFactors ? 4 : 3)}: Saving results to disk...");
            

            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Output", "Absorptions3D");
            Directory.CreateDirectory(saveDir);
            var saveName = $"Output res={sampleResolution}, n={_nrAnglesTheta}, m={_nrAnglesAlpha}.txt";

            if (Settings.flags.planeModeWriteSeparateFiles)
            {
                float[,] current = new float[_nrAnglesAlpha, _nrAnglesTheta];
                for (int k = 0; k < 3; k++)
                for (int j = 0; j < _nrAnglesTheta; j++)
                for (int i = 0; i < _nrAnglesAlpha; i++)
                    current[i, j] = _absorptionFactors[i, j][k];
                ArrayWriteTools.Write2D(Path.Combine(saveDir, saveName), current, reverse: true);


            }
            else
                ArrayWriteTools.Write2D(Path.Combine(saveDir, saveName), _absorptionFactors, reverse: true);
        }

        protected override void Cleanup()
        {
            _inputBuffer.Release();
            _maskBuffer.Release();
            
            SetStatusMessage("Done.");
        }

        #endregion

        #region Helper methods

        private Vector3 GetAbsorptionFactor(Vector3[] absorptions)
        {
            return new Vector3(
                _innerIndices.AsParallel().Select(i => absorptions[i].x).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].y).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].z).Average()
            );
        }

        private double GetThetaAt(int index)
        {
            return MathTools.AsRadian(Math.Abs(angles[index]));
        }

        #endregion
    }
}