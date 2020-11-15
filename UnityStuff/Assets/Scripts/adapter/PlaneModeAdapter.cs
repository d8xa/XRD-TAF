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
        private int _nrCoordinates;
        private int _nrAnglesTheta;
        private int _nrAnglesAlpha;

        private float[] _angles;

        private Vector2 _nrDiffractionPoints;
        private int[] _indicesSample;
        private int[] _indicesCell;

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
            if (logger == null) SetLogger(customLogger);
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
            _nrCoordinates = sampleResolution * sampleResolution;
            
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
            var mask = new Vector2Int[_nrCoordinates];
            _maskBuffer.GetData(mask);
            _indicesSample = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].x > 0.0)
                .ToArray();
            _indicesCell = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].y > 0.0)
                .ToArray();
            _nrDiffractionPoints = new Vector2(_indicesSample.Length, _indicesCell.Length);
            logger.Log(Logger.EventType.Step, 
                $"InitializeOtherFields(): found {_nrDiffractionPoints} diffraction points (of {_nrCoordinates}).");
            
            logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): done.");
        }
        
        private void ComputeIndicatorMask()
        {
            logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): started.");
            SetStatusMessage($"Step 2/{(writeFactors ? 4 : 3)}: Computing indicator mask...");

            SetShaderConstants();
            
            var maskHandle = shader.FindKernel("get_indicator");
            _inputBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            _maskBuffer = new ComputeBuffer(coordinates.Length, sizeof(uint)*2);

            _inputBuffer.SetData(coordinates);
            _maskBuffer.SetData(new Vector2Int[_nrCoordinates]);
            
            shader.SetBuffer(maskHandle, "coordinates", _inputBuffer);
            shader.SetBuffer(maskHandle, "indicator_mask", _maskBuffer);

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
            SetShaderConstants();
            logger.Log(Logger.EventType.Step, "Set shader parameters.");
            
            
            // get kernel handles.
            var handlePart1 = shader.FindKernel("get_dists_part1");
            var handlePart2 = shader.FindKernel("get_dists_part2");
            var handleAbsorptions = shader.FindKernel("get_absorptions");
            logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");
            
            
            // make buffers.
            var outputBufferCellPart1 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferSamplePart1 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferCellPart2 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferSamplePart2 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*3);
            logger.Log(Logger.EventType.Data, "Created buffers.");
            
            
            // set buffers for part1 kernel.
            shader.SetBuffer(handlePart1, "coordinates", _inputBuffer);
            shader.SetBuffer(handlePart1, "distances_sample_part1", outputBufferSamplePart1);
            shader.SetBuffer(handlePart1, "distances_cell_part1", outputBufferCellPart1);
            logger.Log(Logger.EventType.ShaderInteraction, "Wrote data to buffers.");
            
            _inputBuffer.SetData(coordinates);
            
            // compute part1 distances.
            logger.Log(Logger.EventType.ShaderInteraction, "part1 distances kernel dispatch.");
            shader.Dispatch(handlePart1, threadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, "part1 distances kernel return.");
            
            // set buffers for part2 kernel.
            shader.SetBuffer(handlePart2, "coordinates", _inputBuffer);
            shader.SetBuffer(handlePart2, "distances_sample_part2", outputBufferSamplePart2);
            shader.SetBuffer(handlePart2, "distances_cell_part2", outputBufferCellPart2);

            // set shared buffers for absorption factor kernel.
            shader.SetBuffer(handleAbsorptions, "coordinates", _inputBuffer);
            shader.SetBuffer(handleAbsorptions, "distances_sample_part1", outputBufferSamplePart1);
            shader.SetBuffer(handleAbsorptions, "distances_cell_part1", outputBufferCellPart1);
            shader.SetBuffer(handleAbsorptions, "absorptions", absorptionsBuffer);
            shader.SetBuffer(handleAbsorptions, "indicator_mask", _maskBuffer);

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
                
                // compute part2 distances.
                logger.Log(Logger.EventType.ShaderInteraction, "part2 distances kernel dispatch.");
                shader.Dispatch(handlePart2, threadGroupsX, 1, 1);
                logger.Log(Logger.EventType.ShaderInteraction, "part2 distances kernel return.");

                // set iterative buffers for absorption factors kernel.
                shader.SetBuffer(handleAbsorptions, "distances_sample_part2", outputBufferSamplePart2);
                shader.SetBuffer(handleAbsorptions, "distances_cell_part2", outputBufferCellPart2);

                for (int i = 0; i < _nrAnglesAlpha; i++)
                {
                    var startInnerLoop = sw.Elapsed;

                    var v = GetDistanceVector(i, j);
                    var stretchFactor = GetStretchFactor(v);
                    
                    shader.SetFloat("stretch_factor", stretchFactor);
                    shader.Dispatch(handleAbsorptions, threadGroupsX, 1, 1);
                    absorptionsBuffer.GetData(absorptionsTemp);

                    _absorptionFactors[i, j] = GetAbsorptionFactor(absorptionsTemp);
                    
                    avgInnerLoop += sw.Elapsed - startInnerLoop;
                }
            }

            avgInnerLoop = TimeSpan.FromTicks(avgInnerLoop.Ticks/_nrCoordinates);
            totalOuterLoop = sw.Elapsed - totalOuterLoop;

            logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");
            logger.Log(Logger.EventType.Performance, 
                $"Absorption factor calculation took {totalOuterLoop}"
                + $", {avgInnerLoop} on avg. for each inner loop (Column)"
                + ".");
            
            // release buffers.
            _inputBuffer.Release();
            _maskBuffer.Release();
            outputBufferCellPart1.Release();
            outputBufferSamplePart1.Release();
            outputBufferCellPart2.Release();
            outputBufferSamplePart2.Release();
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
        
        private void SetShaderConstants()
        {
            shader.SetFloats("mu", mu.cell, mu.sample);
            shader.SetFloats("r", r.cell, r.sample);
            shader.SetFloats("r2", rSq.cell, rSq.sample);
            shader.SetFloats("ray_dim", properties.ray.dimensions.x/2, properties.ray.dimensions.y/2);
        }

        #endregion

        #region Helper methods

        private Vector3 GetAbsorptionFactor(Vector3[] absorptions)
        {
            return new Vector3(
                _indicesSample.AsParallel().Select(i => absorptions[i].x).Average(),
                _indicesCell.AsParallel().Select(i => absorptions[i].y).Average(),
                _indicesCell.AsParallel().Select(i => absorptions[i].z).Average()
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