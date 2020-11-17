using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using model;
using model.structs;
using UnityEngine;
using util;
using static util.MathTools;
using Logger = util.Logger;
using Vector3 = UnityEngine.Vector3;

namespace adapter
{
    public class IntegratedModeAdapter : ShaderAdapter
    {
        #region Fields
        
        private Rotation[] _rotations;
        private Vector3[] _absorptionFactors;
        private int _nrCoordinates;
        private int _nrAnglesPerRing;
        private int _nrAnglesTheta;
        private double _thetaLowerBound, _thetaUpperBound;
        private double _alphaLowerBound, _alphaUpperBound;
        
        private float[] _angles;
        
        private Vector2 _nrDiffractionPoints;
        private int[] _innerIndices;
        private int[] _outerIndices;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;

        private const string CLASS_NAME = nameof(IntegratedModeAdapter);
        private static string Context(string methodName, string className = CLASS_NAME)
        {
            return $"{className}.{methodName}()";
        }
        
        #endregion

        #region Constructors

        public IntegratedModeAdapter(ComputeShader shader, Preset preset, bool writeFactors, Logger customLogger
        ) : base(shader, preset, writeFactors, customLogger)
        {
            if (logger == null) SetLogger(customLogger);
            logger.Log(Logger.EventType.Class, $"{CLASS_NAME} created.");
            InitializeOtherFields();
        }

        #endregion

        #region Methods

        private void InitializeOtherFields()
        {
            const string method = nameof(InitializeOtherFields);
            logger.Log(Logger.EventType.InitializerMethod, $"{Context(method)}: started.");
            
            _angles = Parser.ImportAngles(
                Path.Combine(Directory.GetCurrentDirectory(), "Input", properties.angle.pathToAngleFile + ".txt"));
            if (!Settings.flags.useRadian)
                _angles = _angles.Select(AsRadian).ToArray();
            // TODO: validate. (e.g. throw and display error if any abs value >= 90°)

            // initialize dimensions.
            _nrCoordinates = sampleResolution * sampleResolution;
            _nrAnglesTheta = _angles.Length;
            _nrAnglesPerRing = properties.angle.angleCount;

            _thetaLowerBound = properties.detector.GetAngleFromIndex(0, false);
            _alphaLowerBound = properties.detector.GetAngleFromIndex(0, true);
            
            _thetaUpperBound = properties.detector.GetAngleFromIndex(properties.detector.resolution.x, false);
            _alphaUpperBound = properties.detector.GetAngleFromIndex(properties.detector.resolution.y, true);
            logger.Log(Logger.EventType.Inspect, 
                $"{Context(method)}: Bounds:\n\talpha = ({_alphaLowerBound}, {_alphaUpperBound})" +
                                                 $", theta = ({_thetaLowerBound}, {_thetaUpperBound})");

            // initialize arrays.
            _absorptionFactors = new Vector3[_nrAnglesTheta];
            _rotations = LinSpace1D(
                    properties.angle.angleStart, 
                    properties.angle.angleEnd, 
                    properties.angle.angleCount,
                    true
                )
                .Select(AsRadian)
                .Select(Rotation.FromAngle)
                .ToArray();
            logger.Log(Logger.EventType.Inspect, 
                $"{Context(method)}: rotations = [{string.Join(",", _rotations.Select(v => v.ToString()))}]");
            
            ComputeIndicatorMask();
            
            // count diffracting points in each case.
            var mask = new Vector2Int[_nrCoordinates];
            _maskBuffer.GetData(mask);
            _innerIndices = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].x > 0.0)
                .ToArray();
            _outerIndices = ParallelEnumerable.Range(0, mask.Length)
                .Where(i => mask[i].y > 0.0)
                .ToArray();
            _nrDiffractionPoints = new Vector2(_innerIndices.Length, _outerIndices.Length);
            logger.Log(Logger.EventType.Step, 
                $"I{Context(method)}: found {_nrDiffractionPoints} diffraction points (of {_nrCoordinates}).");
            
            logger.Log(Logger.EventType.InitializerMethod, $"{Context(method)}: done.");
        }
        
        private void ComputeIndicatorMask()
        {
            const string method = nameof(ComputeIndicatorMask);
            logger.Log(Logger.EventType.Method, $"{Context(method)}: started.");

            var maskHandle = shader.FindKernel("get_indicator");
            _inputBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            _maskBuffer = new ComputeBuffer(coordinates.Length, sizeof(uint)*2);

            SetShaderConstants();
            _inputBuffer.SetData(coordinates);
            _maskBuffer.SetData(new Vector2Int[_nrCoordinates]);
            
            shader.SetBuffer(maskHandle, "coordinates", _inputBuffer);
            shader.SetBuffer(maskHandle, "indicator_mask", _maskBuffer);

            logger.Log(Logger.EventType.ShaderInteraction, 
                $"{Context(method)}: indicator mask shader dispatch.");
            shader.Dispatch(maskHandle, threadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: indicator mask shader return.");

            logger.Log(Logger.EventType.Method, $"{Context(method)}: done.");
        }

        protected override void Compute()
        {
            const string method = nameof(Compute);
            logger.Log(Logger.EventType.Method, $"{Context(method)}: started.");
            
            SetShaderConstants();
            
            // get kernel handles.
            var handlePart1 = shader.FindKernel("get_dists_part1");
            var handlePart2 = shader.FindKernel("get_dists_part2");
            var handleAbsorptions = shader.FindKernel("get_absorptions");
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Retrieved kernel handles.");
            
            // make buffers.
            var outputBufferCellPart1 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferSamplePart1 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferCellPart2 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferSamplePart2 = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*3);
            logger.Log(Logger.EventType.Data, $"{Context(method)}: Created buffers.");
            
            // set buffers for g1 kernel.
            shader.SetBuffer(handlePart1, "coordinates", _inputBuffer);
            shader.SetBuffer(handlePart1, "distances_sample_part1", outputBufferSamplePart1);
            shader.SetBuffer(handlePart1, "distances_cell_part1", outputBufferCellPart1);
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Wrote data to buffers.");
            
            _inputBuffer.SetData(coordinates);
            
            // compute g1 distances.
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: g1 distances kernel dispatch.");
            shader.Dispatch(handlePart1, threadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: g1 distances kernel return.");
            
            // set buffers for g2 kernel.
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

            // debug
            var stretchFactors = new float[_nrAnglesTheta, _nrAnglesTheta];
            var ringCoordinates = new Vector2[_nrAnglesTheta, _nrAnglesTheta];
            var ringAbsorptions = new Vector3[_nrAnglesTheta, _nrAnglesTheta];

            
            // iterative computation of average absorption values for each ring of radius theta:
            for (int j = 0; j < _nrAnglesTheta; j++)
            {
                var ringAbsorptionValues = new LinkedList<Vector3>();
                
                // ring geometry for current theta:
                var ringDistance = Math.Abs(Math.Cos(GetThetaAt(j))) * properties.detector.distToSample;
                var ringRadius = Math.Abs(Math.Sin(GetThetaAt(j))) * properties.detector.distToSample;
                var ringProjRadius = Math.Abs(Math.Tan(GetThetaAt(j))) * properties.detector.distToSample;

                // The distance of any point on the ring to capillary center.
                var hypot = Math.Sqrt(Math.Pow(ringDistance, 2) + Math.Pow(ringRadius, 2));
                
                // iterative computation of absorption values for each point on the current ring:
                for (int i = 0; i < _nrAnglesPerRing; i++)
                {
                    // tau: beam angle to current point on ring.
                    var tau = _rotations[i].cos * GetThetaAt(j);
                    var tauRadius = _rotations[i].cos * ringRadius;

                    var hypotXZ = Math.Sqrt(Math.Pow(ringDistance, 2) + Math.Pow(tauRadius, 2));
                    
                    var stretchFactor = hypot / hypotXZ;
                    stretchFactors[i, j] = (float) stretchFactor;

                    var v = GetRingCoordinate(i, ringProjRadius);
                    ringCoordinates[i, j] = v;
                    
                    if (Settings.flags.useClipping && BoundaryCheck(v))
                    {
                        ringAbsorptions[i,j] = Vector3.positiveInfinity;
                        continue;
                    }

                    // set rotation parameters.
                    shader.SetFloats("rot", (float) Math.Cos(Math.PI - tau), 
                        (float) Math.Sin(Math.PI - tau));
                
                    // compute g2 distances.
                    logger.Log(Logger.EventType.ShaderInteraction, 
                        $"{Context(method)}: g2 distances kernel dispatch.");
                    shader.Dispatch(handlePart2, threadGroupsX, 1, 1);
                    logger.Log(Logger.EventType.ShaderInteraction, 
                        $"{Context(method)}: g2 distances kernel return.");

                    // set iterative buffers for absorption factors kernel.
                    shader.SetBuffer(handleAbsorptions, "distances_sample_part2", outputBufferSamplePart2);
                    shader.SetBuffer(handleAbsorptions, "distances_cell_part2", outputBufferCellPart2);
                    
                    shader.SetFloat("stretch_factor", (float) stretchFactor);
                    shader.SetBuffer(handleAbsorptions, "absorptions", absorptionsBuffer);
                    shader.Dispatch(handleAbsorptions, threadGroupsX, 1, 1);
                    absorptionsBuffer.GetData(absorptionsTemp);

                    var af = GetAbsorptionFactor(absorptionsTemp);
                    ringAbsorptions[i, j] = af;
                    
                    ringAbsorptionValues.AddLast(af);
                }
                
                _absorptionFactors[j] = GetRingAverage(ringAbsorptionValues);
            }
            
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Calculated all absorptions.");

            // release buffers.
            _inputBuffer.Release();
            _maskBuffer.Release();
            outputBufferCellPart1.Release();
            outputBufferSamplePart1.Release();
            outputBufferCellPart2.Release();
            outputBufferSamplePart2.Release();
            absorptionsBuffer.Release();
            logger.Log(Logger.EventType.ShaderInteraction, $"{Context(method)}: Shader buffers released.");

            const string saveFileName1 = "[mode=2] ring coordinates.txt";
            const string saveFileName2 = "[mode=2] vcosinv.txt";
            const string saveFileName3 = "[mode=2] ring absorption values.txt";
            var saveFolderTop = FieldParseTools.IsValue(metadata.pathOutputData) ? metadata.pathOutputData : "";
            var saveFolderBottom = FieldParseTools.IsValue(metadata.saveName) ? metadata.saveName : "No preset";
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Output", saveFolderTop, saveFolderBottom);
            //var savePath = Path.Combine(saveDir, saveFileName);
            Directory.CreateDirectory(saveDir);
            
            ArrayWriteTools.Write2D(Path.Combine(saveDir, saveFileName1), ringCoordinates, reverse:true);
            ArrayWriteTools.Write2D(Path.Combine(saveDir, saveFileName2), stretchFactors, reverse:true);
            ArrayWriteTools.Write2D(Path.Combine(saveDir, saveFileName3), ringAbsorptions, reverse:true);

            logger.Log(Logger.EventType.Method, $"{Context(method)}: done.");
        }

        protected override void Write()
        {
            const string method = nameof(Write);
            logger.Log(Logger.EventType.Method, $"{Context(method)}: started.");
            
            var saveFolderTop = FieldParseTools.IsValue(metadata.pathOutputData) ? metadata.pathOutputData : "";
            var saveFolderBottom = FieldParseTools.IsValue(metadata.saveName) ? metadata.saveName : "No preset";
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Output", saveFolderTop, saveFolderBottom);
            var saveFileName = properties.FilenameFormatter(_nrAnglesTheta);
            var savePath = Path.Combine(saveDir, saveFileName);
            Directory.CreateDirectory(saveDir);

            var headRow = string.Join("\t", "2 theta", "A_{s,sc}", "A_{c,sc}", "A_{c,c}");
            var headCol = _angles
                .Select(v => !Settings.flags.useRadian ? AsDegree(v): v)
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
            
            ArrayWriteTools.Write2D(savePath, headCol, headRow, data);
            
            logger.Log(Logger.EventType.Method, $"{Context(method)}: done.");
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

        private Vector2 GetRingCoordinate(int i, double thetaRadius)
        {
            var x = thetaRadius * _rotations[i].cos;
            var y = thetaRadius * _rotations[i].sin;

            return new Vector2((float) x, (float) y);
        }

        private double GetThetaAt(int index)
        {
            return _angles[index];
        }
        
        private bool BoundaryCheck(Vector2 v)
        {
            var lowerBound = Vector2.zero - properties.detector.offset;
            var upperBound = properties.detector.pixelSize * properties.detector.resolution;
            
            if (v.x < lowerBound.x || v.x > upperBound.x) return true;
            if (v.y < lowerBound.y || v.y > upperBound.y) return true;
            return false;
        }

        private Vector3 GetAbsorptionFactor(Vector3[] absorptions)
        {
            return new Vector3(
                _innerIndices.AsParallel().Select(i => absorptions[i].x).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].y).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].z).Average()
            );
        }
        
        private static Vector3 GetRingAverage(ICollection<Vector3> ringValues)
        {
            if (ringValues.Count == 0) 
                return Vector3.positiveInfinity;    // untested.
            return new Vector3(
                ringValues.AsParallel().Select(v => v.x).Average(),
                ringValues.AsParallel().Select(v => v.y).Average(),
                ringValues.AsParallel().Select(v => v.z).Average()
            );
        }

        #endregion
    }
}