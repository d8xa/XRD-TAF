using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using model;
using UnityEngine;
using util;
using static util.MathTools;
using Logger = util.Logger;
using Vector3 = UnityEngine.Vector3;

namespace controller
{
    public class IntegratedModeAdapter : ShaderAdapter
    {
        #region Fields
        
        private Rotation[] _rotations;
        private Vector3[] _absorptionFactors;
        private int _nrSegments;
        private int _nrAnglesPerRing;
        private int _nrAnglesTheta;
        private double _thetaLowerBound, _thetaUpperBound;
        private double _alphaLowerBound, _alphaUpperBound;

        private Vector2 _nrDiffractionPoints;
        private int[] _innerIndices;
        private int[] _outerIndices;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;
        
        #endregion

        #region Constructors

        public IntegratedModeAdapter(
            ComputeShader shader, 
            Model model, 
            float margin, 
            bool writeFactorsFlag,
            Logger customLogger
            ) : base(shader, model, margin, writeFactorsFlag, customLogger)
        {
            if (logger == null) SetLogger(new Logger());
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        public IntegratedModeAdapter(ComputeShader shader, Model model, Logger customLogger) : base(shader, model, customLogger)
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
            
            // initialize dimensions.
            _nrSegments = segmentResolution * segmentResolution;
            _nrAnglesTheta = model.GetAngles().Length;
            _nrAnglesPerRing = model.detector.angleCount;
            
            
            // TODO: support negative lower bounds. Find out why one bound is negative and one isn't.
            _thetaLowerBound = model.detector.GetAngleFromIndex(0, false);
            _alphaLowerBound = model.detector.GetAngleFromIndex(0, true);
            
            _thetaUpperBound = model.detector.GetAngleFromIndex(model.detector.resolution.x, false);
            _alphaUpperBound = model.detector.GetAngleFromIndex(model.detector.resolution.y, true);
            logger.Log(Logger.EventType.Inspect, $"Bounds: alpha = ({_alphaLowerBound}, {_alphaUpperBound})" +
                                                 $", theta = ({_thetaLowerBound}, {_thetaUpperBound})");

            // initialize arrays.
            _absorptionFactors = new Vector3[_nrAnglesTheta];
            _rotations = LinSpace1D(
                    model.detector.angleStart, 
                    model.detector.angleEnd, 
                    model.detector.angleCount,
                    true
                )
                .Select(AsRadian)
                .Select(Rotation.FromAngle)
                .ToArray();
            logger.Log(Logger.EventType.Inspect, 
                $"{string.Join(",", _rotations.Select(v => v.ToString()))}");
            
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

            // prepare required variables.
            shader.SetFloat("r_cell", model.GetRCell());
            shader.SetFloat("r_sample", model.GetRSample());
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
            
            // initialize parameters in shader.
            shader.SetFloats("mu", model.GetMuCell(), model.GetMuSample());
            shader.SetFloat("r_cell", model.GetRCell());
            shader.SetFloat("r_sample", model.GetRSample());
            shader.SetFloat("r_cell_sq", model.GetRCellSq());
            shader.SetFloat("r_sample_sq", model.GetRSampleSq());
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
            
            // iterative computation of average absorption values for each ring of radius theta:
            for (int j = 0; j < _nrAnglesTheta; j++)
            {
                var ringAbsorptionValues = new LinkedList<Vector3>();
                
                // ring geometry values for current theta:
                var thetaHypotLength = Math.Abs(model.detector.distToSample / Math.Cos(GetThetaAt(j))); // p (green)
                var thetaRadius = 
                    Math.Sqrt(Math.Pow(thetaHypotLength, 2) - Math.Pow(model.detector.distToSample, 2));    // r (blue)
                
                // iterative computation of absorption values for each point on the current ring:
                for (int i = 0; i < _nrAnglesPerRing; i++)
                {
                    // tau: theta angle at x-coordinate of rotated point.
                    var tau = _rotations[i].GetCos() * GetThetaAt(j);
                    
                    var vCosInv = GetVFactor(i, j, tau, thetaRadius, thetaHypotLength);
                    
                    if (IsOutside(j, i, tau, vCosInv))
                    {
                        logger.Log(Logger.EventType.Inspect, $"(i={i}, j={j}) outside.");
                        continue;
                    }

                    // set rotation parameters.
                    shader.SetFloat("rotCos", (float) Math.Cos(Math.PI - tau));
                    shader.SetFloat("rotSin", (float) Math.Sin(Math.PI - tau));
                
                    // compute g2 distances.
                    logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel dispatch.");
                    shader.Dispatch(g2Handle, threadGroupsX, 1, 1);
                    logger.Log(Logger.EventType.ShaderInteraction, "g2 distances kernel return.");

                    // set iterative buffers for absorption factors kernel.
                    shader.SetBuffer(absorptionsHandle, "g2DistancesInner", g2OutputBufferInner);
                    shader.SetBuffer(absorptionsHandle, "g2DistancesOuter", g2OutputBufferOuter);
                    
                    shader.SetFloat("vCos", (float) vCosInv);
                    shader.SetBuffer(absorptionsHandle, "absorptionFactors", absorptionsBuffer);
                    shader.Dispatch(absorptionsHandle, threadGroupsX, 1, 1);
                    absorptionsBuffer.GetData(absorptionsTemp);
                    
                    ringAbsorptionValues.AddLast(GetAbsorptionFactor(absorptionsTemp));
                    
                    // TODO: remove after validation.
                    if (IsUnrepresentable(ringAbsorptionValues.ElementAt(i)))
                        logger.Log(Logger.EventType.Inspect, $"(i={i}, j={j}): NaN or Infinity detected.");
                }

                //if (AnyIrregular(ringValues)) 
                    logger.Log(Logger.EventType.Inspect, $"(j={j})\t" + string.Join(", ", ringAbsorptionValues.Select(v => v.ToString("F5"))));
                _absorptionFactors[j] = GetRingAverage(ringAbsorptionValues);
            }
            
            logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");

            var results = string.Join(", ", 
                _absorptionFactors.Select(v => v.ToString("F5")).ToArray());
            logger.Log(Logger.EventType.Inspect, $"Absorptions factors: {results}");

            // release buffers.
            _inputBuffer.Release();
            _maskBuffer.Release();
            g1OutputBufferOuter.Release();
            g1OutputBufferInner.Release();
            g2OutputBufferOuter.Release();
            g2OutputBufferInner.Release();
            absorptionsBuffer.Release();
            logger.Log(Logger.EventType.ShaderInteraction, "Shader buffers released.");
            
            logger.Log(Logger.EventType.Method, "Compute(): done.");
        }

        protected override void Write()
        {
            var saveDir = Path.Combine("Logs", "AbsorptionsIntegrated");
            Directory.CreateDirectory(saveDir);
            var saveName = $"Output res={segmentResolution}, n={_nrAnglesTheta}, m={_nrAnglesPerRing}.txt";

            var headRow = string.Join("\t", "2 theta", "A_{s,sc}", "A_{c,sc}", "A_{c,c}");
            var headCol = model.GetAngles()
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
            
            ArrayWriteTools.Write2D(Path.Combine(saveDir, saveName), headCol, headRow, data);
        }
        
        #endregion

        #region Helper methods

        private void LogRingGeometry(int i, int j, double thetaHypotLength, double thetaRadius, double tau,
            double tauVerticalDiff, double tauHypotLength, double vCos, double hypotLength)
        {
            logger.Log(Logger.EventType.Inspect,
                $"(i={i}, j={j})"
                + $"\ntheta: {GetThetaAt(j)} (deg)"
                + $", thetaHypotLength: {thetaHypotLength:G}"
                + $", thetaRadius: {thetaRadius:G}"
                + $"\ntau: {AsDegree(tau):G} (deg)"
                + $", tauVerticalDiff: {tauVerticalDiff:G}"
                + $", tauHypotLength: {tauHypotLength:G}"
                + "\nrho: "
                + $"{Math.Acos(_rotations[i].GetCos())} (rad)"
                + $", {AsDegree(Math.Acos(_rotations[i].GetCos()))} (deg)"
                + $"\nalpha: {AsDegree(Math.Acos(1/vCos))} (deg)"
                + $", vCos: {vCos:G}"
                + $"\nhypotLength: {hypotLength:G}"
            );
        }

        private double GetThetaAt(int index)
        {
            return AsRadian((double) model.GetAngleAt(index));
        }

        private bool IsOutside(int j, int i, double tau, double vCosInv)
        {
            logger.Log(Logger.EventType.Inspect, 
                $"(i={i}, j={j}): tau={tau}, vCos={1/vCosInv}, v angle={AsDegree(Math.Acos(1/vCosInv))} (deg)");
            if (tau < _thetaLowerBound || tau > _thetaUpperBound) return true;
            else if (Math.Acos(1/vCosInv) < _alphaLowerBound || Math.Acos(1/vCosInv) > _alphaUpperBound) return true;
            return false;
        }

        private bool IsUnrepresentable(Vector3 value)
        {
            return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) 
                   || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z);
        }
        
        private Vector3 GetAbsorptionFactor(Vector3[] absorptions)
        {
            return new Vector3(
                _innerIndices.AsParallel().Select(i => absorptions[i].x).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].y).Average(),
                _outerIndices.AsParallel().Select(i => absorptions[i].z).Average()
            );
        }
        
        private Vector3 GetRingAverage(ICollection<Vector3> ringValues)
        {
            if (ringValues.Count == 0) 
                return Vector3.positiveInfinity;    // untested.
            return new Vector3(
                ringValues.AsParallel().Select(v => v.x).Average(),
                ringValues.AsParallel().Select(v => v.y).Average(),
                ringValues.AsParallel().Select(v => v.z).Average()
            );
        }

        /// <summary>
        /// Calculates the cosine of the (vertical) angle between the diffraction ray and its projection on the XY-plane.
        /// </summary>
        /// <returns></returns>
        private double GetVFactor(int i, int j, double tau, double thetaRadius, double thetaHypotLength)
        {
            // vertical offset of rotated point to base point. 
            var tauVerticalOffset = _rotations[i].GetSin() * thetaRadius;    // a (orange)
                        
            var tauHypotLength = model.detector.distToSample / Math.Cos(tau);    // b (pink)
            var hypotLength = Math.Sqrt(Math.Pow(tauHypotLength, 2) + Math.Pow(tauVerticalOffset, 2));    // c (light blue)
    
            // ratio of distance to 2D projection of ray from sample center to rotated point.
            var vCosInv = hypotLength / tauHypotLength;
                if (Math.Abs(tauVerticalOffset) < 1E-5) vCosInv = 1;    // experimental
    
            LogRingGeometry(i, j, thetaHypotLength, thetaRadius, tau, tauVerticalOffset, tauHypotLength, vCosInv, hypotLength);

            return vCosInv;
        }

        #endregion
    }
}