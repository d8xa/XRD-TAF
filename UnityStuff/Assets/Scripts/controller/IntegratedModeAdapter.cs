using System;
using System.Linq;
using model;
using UnityEngine;
using util;
using Logger = util.Logger;
using Vector3 = UnityEngine.Vector3;

namespace controller
{
    public class IntegratedModeAdapter : ShaderAdapter
    {
        private Rotation[] _rotations;
        private Vector3[] _absorptionFactors;
        private int _nrSegments;
        private int _nrAnglesPerRing;
        private int _nrAnglesTheta;
        
        private Vector2 _nrDiffractionPoints;
        private int[] _innerIndices;
        private int[] _outerIndices;

        private ComputeBuffer _inputBuffer;
        private ComputeBuffer _maskBuffer;

        public IntegratedModeAdapter(
            ComputeShader shader, 
            Model model, 
            float margin, 
            bool writeFactorsFlag
            ) : base(shader, model, margin, writeFactorsFlag)
        {
            SetLogger(new Logger());
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        public IntegratedModeAdapter(ComputeShader shader, Model model) : base(shader, model)
        {
            SetLogger(new Logger());
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        private void InitializeOtherFields()
        {
            logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): started.");
            
            // initialize dimensions.
            _nrSegments = segmentResolution * segmentResolution;
            _nrAnglesTheta = model.GetAngles().Length;
            _nrAnglesPerRing = model.detector.angleAmount;
            
            // initialize arrays.
            _absorptionFactors = new Vector3[_nrAnglesTheta];
            _rotations = MathTools.LinSpace1D(
                    model.detector.angleStart, 
                    model.detector.angleEnd, 
                    model.detector.angleAmount
                )
                .Select(Rotation.FromAngle)
                .ToArray();
            
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
            // necessary here already?
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
            
            throw new NotImplementedException();
        }

        protected override void Write()
        {
            throw new NotImplementedException();
        }
    }
}