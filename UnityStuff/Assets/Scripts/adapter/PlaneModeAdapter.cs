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

        private float[] _angles;

        private Vector2 _nrDiffractionPoints;
        private int[] _innerIndices;
        private int[] _outerIndices;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;

        #endregion

        #region Constructors

        public PlaneModeAdapter(
            ComputeShader shader, 
            Preset preset,
            bool writeFactors,
            Logger customLogger
        ) : base(shader, preset, writeFactors, customLogger)
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
            
            _angles = Enumerable.Range(0, properties.detector.resolution.x)
                .Select(j => properties.detector.GetAngleFromIndex(j))
                .Select(v => (float) v)
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
            shader.SetFloat("r_cell", r.cell);
            shader.SetFloat("r_sample", r.sample);
            shader.SetFloat("ray_width", properties.ray.dimensions.x/2);
            var maskHandle = shader.FindKernel("getIndicatorMask");
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

        protected override void Compute()
        {
            logger.Log(Logger.EventType.Method, "Compute(): started.");
            SetStatusMessage($"Step 3/{(writeFactors ? 4 : 3)}: Computing absorption factors...");

            var sw = new Stopwatch();
            sw.Start();

            // initialize parameters in shader.
            // TODO: extract.
            shader.SetFloats("mu", mu.cell, mu.sample);
            shader.SetFloat("r_cell", r.cell);
            shader.SetFloat("r_sample", r.sample);
            shader.SetFloat("r_cell_sq", rSq.cell);
            shader.SetFloat("r_sample_sq", rSq.sample);
            logger.Log(Logger.EventType.Step, "Set shader parameters.");
            
            
            // get kernel handles.
            var g1Handle = shader.FindKernel("g1_dists");
            var g2Handle = shader.FindKernel("g2_dists");
            var absorptionsHandle = shader.FindKernel("Absorptions");
            logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");
            
            
            // make buffers.
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
                shader.SetFloat("rot_cos", (float) Math.Cos(Math.PI - GetThetaAt(j)));
                shader.SetFloat("rot_sin", (float) Math.Sin(Math.PI - GetThetaAt(j)));
                
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

                    var v = GetDistanceVector(i, j);
                    var stretchFactor = GetStretchFactor(v);
                    
                    shader.SetFloat("vCos", stretchFactor);
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

            var saveFolderTop = FieldParseTools.IsValue(metadata.pathOutputData) ? metadata.pathOutputData : "";
            var saveFolderBottom = FieldParseTools.IsValue(metadata.saveName) ? metadata.saveName : "No preset";
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Output", saveFolderTop, saveFolderBottom);
            var saveFileName = properties.FilenameFormatter(_nrAnglesTheta);
            var savePath = Path.Combine(saveDir, saveFileName);
            Directory.CreateDirectory(saveDir);

            if (Settings.flags.planeModeWriteSeparateFiles)
            {
                float[,] current = new float[_nrAnglesAlpha, _nrAnglesTheta];
                for (int col = 0; col < 3; col++)
                {
                    for (int j = 0; j < _nrAnglesTheta; j++)
                    for (int i = 0; i < _nrAnglesAlpha; i++)
                        current[i, j] = _absorptionFactors[i, j][col];
                    
                    ArrayWriteTools.Write2D(savePath.Replace("[mode=1]", $"[mode=1][case={col}]"), current,
                        reverse: true);
                }
            }
            else
                ArrayWriteTools.Write2D(savePath, _absorptionFactors, reverse: true);
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
            return Math.Abs(_angles[index]);
        }

        // TODO: check axes
        private Vector2 GetDistanceVector(int i, int j)
        {
            return new Vector2(
                (j + 0.5f)*properties.detector.pixelSize.x,
                (i + 0.5f)*properties.detector.pixelSize.y
            ) - properties.detector.offset;
        }

        private float GetStretchFactor(Vector2 distance)
        {
            var hypotXZ = Math.Sqrt(Math.Pow(distance.x, 2) + Math.Pow(properties.detector.distToSample, 2));
            var hypot = Math.Sqrt(Math.Pow(distance.y, 2) + Math.Pow(hypotXZ, 2));

            return (float) (hypot / hypotXZ);
        }

        #endregion
    }
}