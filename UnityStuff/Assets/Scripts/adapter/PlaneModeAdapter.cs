using System;
using System.IO;
using System.Linq;
using model;
using UnityEngine;
using util;
using static tests.PerformanceReport.TimeInterval;
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
        
        private Vector2 _nrDiffractionPoints;
        private int[] _indicesSample;
        private int[] _indicesCell;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;

        private const string CLASS_NAME = nameof(PlaneModeAdapter);
        private static string Context(string methodName, string className = CLASS_NAME)
        {
            return $"{className}.{methodName}()";
        }

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
            logger.Log(Logger.EventType.Class, $"{nameof(PlaneModeAdapter)} created.");
            InitializeOtherFields();
        }

        #endregion

        #region Methods

        private void InitializeOtherFields()
        {
            const string method = nameof(InitializeOtherFields);
            logger.Log(Logger.EventType.InitializerMethod, $"{Context(method)}: started.");
            SetStatusMessage($"Step 1/{(writeFactors ? 4 : 3)}: Initializing...");
            
            _nrAnglesTheta = properties.detector.resolution.x;
            _nrAnglesAlpha = properties.detector.resolution.y;
            _nrCoordinates = sampleResolution * sampleResolution;
            
            angles = Enumerable.Range(0, properties.detector.resolution.x)
                .Select(j => properties.detector.GetAngleFromIndex(j)) 
                .ToArray();
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesAlpha, _nrAnglesTheta];

            // get indicator mask where each point diffracts in each case.
            //_diffractionMask = new Vector3Int[Coordinates.Length];
            ComputeIndicatorMask();
            
            // count diffracting points in each case.
            var mask = new Vector2Int[_nrCoordinates];
            stopwatch.Record(Category.Buffer, () => _maskBuffer.GetData(mask));
            _indicesSample = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].x > 0.0)
                .ToArray();
            _indicesCell = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].y > 0.0)
                .ToArray();
            _nrDiffractionPoints = new Vector2(_indicesSample.Length, _indicesCell.Length);
            logger.Log(Logger.EventType.Step, 
                $"{Context(method)}: found {_nrDiffractionPoints} diffraction points (of {_nrCoordinates}).");
            
            logger.Log(Logger.EventType.InitializerMethod, $"{Context(method)}: done.");
        }
        
        private void ComputeIndicatorMask()
        {
            const string method = nameof(ComputeIndicatorMask);
            logger.Log(Logger.EventType.Method, $"{Context(method)}: started.");
            SetStatusMessage($"Step 2/{(writeFactors ? 4 : 3)}: Computing indicator mask...");

            stopwatch.Record(Category.Buffer, SetShaderConstants);
            stopwatch.Start(Category.Shader);
            var maskHandle = shader.FindKernel("get_indicator");
            stopwatch.Stop(Category.Shader);
            
            stopwatch.Record(Category.Buffer, () =>
            {
                _inputBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
                _maskBuffer = new ComputeBuffer(coordinates.Length, sizeof(uint)*2);

                _inputBuffer.SetData(coordinates);
                _maskBuffer.SetData(new Vector2Int[_nrCoordinates]);
            
                shader.SetBuffer(maskHandle, "coordinates", _inputBuffer);
                shader.SetBuffer(maskHandle, "indicator_mask", _maskBuffer);
            });
            
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: indicator mask shader dispatch.");
            stopwatch.Record(Category.Shader, () => shader.Dispatch(maskHandle, threadGroupsX, 1, 1));
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: indicator mask shader return.");

            logger.Log(Logger.EventType.Method, $"{Context(method)}: done.");
        }

        protected override void Compute()
        {
            const string method = nameof(Compute);
            logger.Log(Logger.EventType.Method, $"{Context(method)}: started.");
            SetStatusMessage($"Step 3/{(writeFactors ? 4 : 3)}: Computing absorption factors...");
            

            // initialize parameters in shader.
            stopwatch.Record(Category.Shader, SetShaderConstants);
            logger.Log(Logger.EventType.Step, $"{Context(method)}: Set shader parameters.");
            
            stopwatch.Start(Category.Shader);
            // get kernel handles.
            var handlePart1 = shader.FindKernel("get_dists_part1");
            var handlePart2 = shader.FindKernel("get_dists_part2");
            var handleAbsorptions = shader.FindKernel("get_absorptions");
            stopwatch.Stop(Category.Shader);
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Retrieved kernel handles.");

            // make buffers.
            stopwatch.Start(Category.Buffer);
            var outputBufferCellPart1 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferSamplePart1 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferCellPart2 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferSamplePart2 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*3);
            logger.Log(Logger.EventType.Data, $"{Context(method)}: Created buffers.");
            
            // set buffers for part1 kernel.
            shader.SetBuffer(handlePart1, "coordinates", _inputBuffer);
            shader.SetBuffer(handlePart1, "distances_sample_part1", outputBufferSamplePart1);
            shader.SetBuffer(handlePart1, "distances_cell_part1", outputBufferCellPart1);
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Wrote data to buffers.");
            
            _inputBuffer.SetData(coordinates);
            
            stopwatch.Stop(Category.Buffer);

            // compute part1 distances.
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: part1 distances kernel dispatch.");
            stopwatch.Record(Category.Shader, () => shader.Dispatch(handlePart1, threadGroupsX, 1, 1));
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: part1 distances kernel return.");
            
            stopwatch.Record(Category.Buffer, () =>
            {
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
            });
            
            var absorptionsTemp = new Vector3[coordinates.Length];
            Array.Clear(absorptionsTemp, 0, absorptionsTemp.Length);
            stopwatch.Record(Category.Buffer, () => absorptionsBuffer.SetData(absorptionsTemp));

            for (int j = 0; j < _nrAnglesTheta; j++)
            {
                // set rotation parameters.
                var j1 = j;
                stopwatch.Record(Category.Buffer, () =>
                    shader.SetFloats("rot", (float) Math.Cos(Math.PI - GetThetaAt(j1)),
                        (float) Math.Sin(Math.PI - GetThetaAt(j1)))
                );

                // compute part2 distances.
                logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: part2 distances kernel dispatch.");
                stopwatch.Record(Category.Shader, () => shader.Dispatch(handlePart2, threadGroupsX, 1, 1));
                logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: part2 distances kernel return.");

                // set iterative buffers for absorption factors kernel.
                stopwatch.Record(Category.Buffer, () =>
                {
                    shader.SetBuffer(handleAbsorptions, "distances_sample_part2", outputBufferSamplePart2);
                    shader.SetBuffer(handleAbsorptions, "distances_cell_part2", outputBufferCellPart2);
                });

                for (int i = 0; i < _nrAnglesAlpha; i++)
                {
                    var v = GetDistanceVector(i, j);
                    var stretchFactor = GetStretchFactor(v);
                    
                    stopwatch.Record(Category.Buffer, () => shader.SetFloat("stretch_factor", stretchFactor));
                    stopwatch.Record(Category.Shader, () => shader.Dispatch(handleAbsorptions, threadGroupsX, 1, 1));
                    stopwatch.Record(Category.Buffer, () => absorptionsBuffer.GetData(absorptionsTemp));

                    _absorptionFactors[i, j] = GetAbsorptionFactor(absorptionsTemp);
                }
            }
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Calculated all absorptions.");

            // release buffers.
            stopwatch.Record(Category.Buffer, () =>
            {
                _inputBuffer.Release();
                _maskBuffer.Release();
                outputBufferCellPart1.Release();
                outputBufferSamplePart1.Release();
                outputBufferCellPart2.Release();
                outputBufferSamplePart2.Release();
                absorptionsBuffer.Release();
            });
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Shader buffers released.");
            
            logger.Log(Logger.EventType.Method, $"{Context(method)}: done.");
        }
        
        protected override void Write()
        {
            SetStatusMessage($"Step 3/{(writeFactors ? 4 : 3)}: Saving results to disk...");

            var saveFolderTop = FieldParseTools.IsValue(metadata.pathOutputData) ? metadata.pathOutputData : "";
            var saveFolderBottom = FieldParseTools.IsValue(metadata.saveName) ? metadata.saveName : "No preset";
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), Settings.DefaultValues.OutputFolderName,
                saveFolderTop, saveFolderBottom);
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
                    
                    stopwatch.Record(Category.IO, () =>
                    {
                        ArrayWriteTools.Write2D(savePath.Replace("[mode=1]", $"[mode=1][case={col}]"), current,
                            reverse: true);
                    });
                }
            }
            else 
                stopwatch.Record(Category.IO, 
                    () => ArrayWriteTools.Write2D(savePath, _absorptionFactors, reverse: true));
        }

        protected override void Cleanup()
        {
            stopwatch.Record(Category.Buffer, () =>
            {
                _inputBuffer.Release();
                _maskBuffer.Release();
            });
            
            SetStatusMessage("Done.");
            
            base.Cleanup();
        }
        
        private void SetShaderConstants()
        {
            shader.SetFloats("mu", mu.cell, mu.sample);
            shader.SetFloats("r", r.cell, r.sample);
            shader.SetFloats("r2", rSq.cell, rSq.sample);
            shader.SetFloats("ray_dim", properties.ray.dimensions.x/2, properties.ray.dimensions.y/2);
            shader.SetFloats("ray_offset", properties.ray.offset.x, properties.ray.offset.y);
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
            return Math.Abs(angles[index]);
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