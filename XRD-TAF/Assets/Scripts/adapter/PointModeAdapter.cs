using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using model;
using tests;
using UnityEngine;
using util;
using static tests.PerformanceReport.TimeInterval;
using static util.MathTools;
using Vector3 = UnityEngine.Vector3;
using Logger = util.Logger;

namespace adapter
{
    public class PointModeAdapter : ShaderAdapter
    {
        #region Fields
        
        private Vector3[] _absorptionFactors;
        private int _nrCoordinates;
        private int _nrAnglesTheta;
        
        // Mask of diffraction points
        private Vector2Int[] _diffractionMask;
        private int[] _indicesSample;
        private int[] _indicesCell;
        
        private ComputeBuffer _inputBuffer;
        
        private const string CLASS_NAME = nameof(PointModeAdapter);
        private static string Context(string methodName, string className = CLASS_NAME)
        {
            return $"{className}.{methodName}()";
        }
        
        #endregion

        #region Constructors

        public PointModeAdapter(ComputeShader shader, Preset preset, bool writeFactors, Logger customLogger,
            double[] angles, PerformanceReport performanceReport) 
            : base(shader, preset, writeFactors, customLogger, angles, performanceReport)
        {
            if (logger == null) SetLogger(customLogger);
            logger.Log(Logger.EventType.Class, $"{nameof(PointModeAdapter)} created.");
            InitializeOtherFields();
        }

        #endregion

        #region Methods

        private void InitializeOtherFields()
        {
            const string method = nameof(InitializeOtherFields);
            logger.Log(Logger.EventType.InitializerMethod, $"{Context(method)}: started.");
            
            stopwatch.Record(Category.IO, () =>
            {
                if (angles != null) return;
                angles = Parser.ImportAngles(Path.Combine(Directory.GetCurrentDirectory(), 
                    Settings.DefaultValues.InputFolderName, properties.angle.pathToAngleFile + ".txt"));
                Debug.Log(string.Join(", ", angles.Select(v => v.ToString("g"))));

            });
            if (!Settings.flags.useRadian)
                angles = angles.Select(AsRadian).ToArray();
            
            _nrAnglesTheta = angles.Length;
            _nrCoordinates = sampleResolution * sampleResolution;
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesTheta];

            // get indicator mask where each point diffracts in each case.
            _diffractionMask = new Vector2Int[coordinates.Length];
            ComputeIndicatorMask();
            
            _indicesSample = ParallelEnumerable.Range(0, _diffractionMask.Length)
                .Where(i => _diffractionMask[i].x > 0.0)
                .ToArray();
            _indicesCell = ParallelEnumerable.Range(0, _diffractionMask.Length)
                .Where(i => _diffractionMask[i].y > 0.0)
                .ToArray();
            
            logger.Log(Logger.EventType.Step, 
                $"{Context(method)}:" +
                $" found ({_indicesSample.Length}, {_indicesCell.Length}) diffraction points (of {_nrCoordinates}).");
            
            logger.Log(Logger.EventType.InitializerMethod, $"{Context(method)}: done.");
        }

        private void ComputeIndicatorMask()
        {
            const string method = nameof(ComputeIndicatorMask); 
            logger.Log(Logger.EventType.Method, $"{Context(method)}: started.");

            stopwatch.Record(Category.Buffer, SetShaderConstants);

            var handleIndicator = shader.FindKernel("get_indicator");
            _inputBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var maskBuffer = new ComputeBuffer(coordinates.Length, sizeof(uint)*2);

            stopwatch.Record(Category.Buffer, () =>
            {
                _inputBuffer.SetData(coordinates);
                maskBuffer.SetData(new Vector2[coordinates.Length]);
                
                shader.SetBuffer(handleIndicator, "coordinates", _inputBuffer);
                shader.SetBuffer(handleIndicator, "indicator_mask", maskBuffer);
            });
            
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: indicator mask shader dispatch.");
            stopwatch.Record(Category.Shader, () => shader.Dispatch(handleIndicator, threadGroupsX, 1, 1));
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: indicator mask shader return.");
            stopwatch.Record(Category.Buffer, () => maskBuffer.GetData(_diffractionMask));
            
            maskBuffer.Release();

            logger.Log(Logger.EventType.Method, $"{Context(method)}: done.");
        }

        protected override void Compute()
        {
            const string method = nameof(Compute);
            logger.Log(Logger.EventType.Method, $"{Context(method)}: started.");

            stopwatch.Record(Category.Buffer, SetShaderConstants);

            // get kernel handles.
            stopwatch.Start(Category.Shader);
            var handlePart1 = shader.FindKernel("get_dists_part1");
            var handleAbsorptions = shader.FindKernel("get_absorptions");
            stopwatch.Stop(Category.Shader);
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Retrieved kernel handles.");

 
            // make buffers.
            var outputBufferCell = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferSample = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*3);
            logger.Log(Logger.EventType.Data, $"{Context(method)}: Created buffers.");
            
            stopwatch.Record(Category.Buffer, () =>
            {
                _inputBuffer.SetData(coordinates);
                
                // set buffers for part1 kernel.
                shader.SetBuffer(handlePart1, "coordinates", _inputBuffer);
                shader.SetBuffer(handlePart1, "distances_sample", outputBufferSample);
                shader.SetBuffer(handlePart1, "distances_cell", outputBufferCell);
            });
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Wrote data to buffers.");

            
            // compute part1 distances.
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: part1 distances kernel dispatch.");
            stopwatch.Record(Category.Shader, () => shader.Dispatch(handlePart1, threadGroupsX, 1, 1));
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: part1 distances kernel return.");
            
            
            var absorptions = new Vector3[_nrCoordinates];
            Array.Clear(absorptions, 0, absorptions.Length);

            stopwatch.Record(Category.Buffer, () => shader.SetBuffer(handleAbsorptions, "coordinates", _inputBuffer));

            // for each angle:
            for (int j = 0; j < _nrAnglesTheta; j++) {
                var j1 = j;
                stopwatch.Record(Category.Buffer, () =>
                {
                    shader.SetFloats("rot", (float) Math.Cos(Math.PI - GetThetaAt(j1)),
                        (float) Math.Sin(Math.PI - GetThetaAt(j1)));
                    shader.SetBuffer(handleAbsorptions, "distances_sample", outputBufferSample);
                    shader.SetBuffer(handleAbsorptions, "distances_cell", outputBufferCell);
                    shader.SetBuffer(handleAbsorptions, "absorptions", absorptionsBuffer);
                });
                stopwatch.Record(Category.Shader, () => shader.Dispatch(handleAbsorptions, threadGroupsX, 1, 1));
                stopwatch.Record(Category.Buffer, () => absorptionsBuffer.GetData(absorptions));
                
                _absorptionFactors[j] = GetAbsorptionFactor(absorptions);
            }
            
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Calculated all absorptions.");

            // release buffers.
            stopwatch.Record(Category.Buffer, () =>
            {
                _inputBuffer.Release();
                outputBufferCell.Release();
                outputBufferSample.Release();
                absorptionsBuffer.Release();
            });
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Shader buffers released.");
            
            logger.Log(Logger.EventType.Method, $"{Context(method)}: done.");
        }
        
        protected override void Write()
        {
            if (writeFactors) 
                stopwatch.Record(Category.IO, WriteAbsorptionFactors);
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

        private Vector3 GetAbsorptionFactor(IList<Vector3> absorptions)
        {
            return new Vector3(
                _indicesSample.AsParallel().Select(i => absorptions[i].x).Average(),
                _indicesCell.AsParallel().Select(i => absorptions[i].y).Average(),
                _indicesCell.AsParallel().Select(i => absorptions[i].z).Average()
            );
        }

        private void WriteAbsorptionFactors()
        {
            const string method = nameof(WriteAbsorptionFactors);
            var saveFolderTop = FieldParseTools.IsValue(metadata.pathOutputData) ? metadata.pathOutputData : "";
            var saveFolderBottom = FieldParseTools.IsValue(metadata.saveName) ? metadata.saveName : "No preset";
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), Settings.DefaultValues.OutputFolderName,
                saveFolderTop, saveFolderBottom);
            var saveFileName = properties.FilenameFormatter(_nrAnglesTheta);
            var savePath = Path.Combine(saveDir, saveFileName);
            Directory.CreateDirectory(saveDir);

            var headRow = (Settings.flags.useOutputPreamble ? properties.OutputPreamble() + "\n" : "") +
                          string.Join("\t", "2 theta", "A_{s,sc}", "A_{c,sc}", "A_{c,c}");
            var headCol = angles
                .Select(v => !Settings.flags.useRadian ? AsDegree(v): v)
                .Select(angle => angle.ToString("G", CultureInfo.InvariantCulture))
                .ToArray();
            var data = new float[_nrAnglesTheta, 3];
            for (int i = 0; i < _nrAnglesTheta; i++)
            for (int j = 0; j < 3; j++)
                data[i, j] = _absorptionFactors[i][j];

            ArrayWriteTools.Write2D(savePath, headCol, headRow, data);
            logger.Log(Logger.EventType.Step, $"{Context(method)}: done.");
        }
        
        
        private double GetThetaAt(int index)
        {
            return Math.Abs(angles[index]);
        }

        #endregion
    }
}