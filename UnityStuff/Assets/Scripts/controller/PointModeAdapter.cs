﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using model;
using UnityEngine;
using util;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Logger = util.Logger;

namespace controller
{
    public class PointModeAdapter : ShaderAdapter
    {
        #region Fields
        
        private Vector3[] _absorptionFactors;
        private int _nrSegments;
        private int _nrAnglesTheta;
        
        // Mask of diffraction points
        private Vector2Int[] _diffractionMask;
        private Vector2 _nrDiffractionPoints;
        private int[] _innerIndices;
        private int[] _outerIndices;
        
        private ComputeBuffer _inputBuffer;
        
        #endregion

        #region Constructors

        public PointModeAdapter(
            ComputeShader shader,
            Model model,
            float margin,
            bool writeFactorsFlag
        ) : base(shader, model, margin, writeFactorsFlag)
        {
            SetLogger(new NullLogger());
            logger.SetPrintFilter(new List<Logger.EventType>()
            {
                Logger.EventType.Performance,
                Logger.EventType.Class,
                Logger.EventType.InitializerMethod
            });
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        public PointModeAdapter(
            ComputeShader shader, 
            Model model
        ) : base(shader, model)
        {
            SetLogger(new Logger());
            logger.Log(Logger.EventType.Class, $"{GetType().Name} created.");
            InitializeOtherFields();
        }

        #endregion

        #region Methods

        private void InitializeOtherFields()
        {
            logger.SetPrintLevel(Logger.LogLevel.All);
            logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): started.");
            _nrAnglesTheta = model.GetAngles().Length;
            _nrSegments = segmentResolution * segmentResolution;
            
            // initialize absorption array. dim n: (#thetas).
            _absorptionFactors = new Vector3[_nrAnglesTheta];

            // get indicator mask where each point diffracts in each case.
            _diffractionMask = new Vector2Int[coordinates.Length];
            ComputeIndicatorMask();
            
            _innerIndices = ParallelEnumerable.Range(0, _diffractionMask.Length)
                .Where(i => _diffractionMask[i].x > 0.0)
                .ToArray();
            _outerIndices = ParallelEnumerable.Range(0, _diffractionMask.Length)
                .Where(i => _diffractionMask[i].y > 0.0)
                .ToArray();
            
            // count diffracting points in each case.
            logger.Log(Logger.EventType.Step, 
                "InitializeOtherFields():" +
                $" found ({_innerIndices.Length}, {_outerIndices.Length}) diffraction points (of {_nrSegments}).");
            
            logger.Log(Logger.EventType.InitializerMethod, "InitializeOtherFields(): done.");
        }

        private void ComputeIndicatorMask()
        {
            logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): started.");

            // prepare required variables.
            shader.SetFloat("r_cell", model.GetRCell());
            shader.SetFloat("r_sample", model.GetRSample());
            shader.SetInts("indicatorCount", 0, 0, 0);
            var maskHandle = shader.FindKernel("getIndicatorMask");
            _inputBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var maskBuffer = new ComputeBuffer(coordinates.Length, sizeof(uint)*2);

            _inputBuffer.SetData(coordinates);
            maskBuffer.SetData(_diffractionMask);
            
            shader.SetBuffer(maskHandle, "segment", _inputBuffer);
            shader.SetBuffer(maskHandle, "indicatorMask", maskBuffer);

            logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader dispatch.");
            shader.Dispatch(maskHandle, threadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, 
                "ComputeIndicatorMask(): indicator mask shader return.");
            maskBuffer.GetData(_diffractionMask);
            
            maskBuffer.Release();

            logger.Log(Logger.EventType.Method, "ComputeIndicatorMask(): done.");
        }

        protected override void Compute()
        {
            logger.Log(Logger.EventType.Method, "Compute(): started.");

            var sw = new Stopwatch();
            sw.Start();
            
            // initialize g1 distance arrays.
            var g1DistsOuter = new Vector2[_nrSegments];
            var g1DistsInner = new Vector2[_nrSegments];
            Array.Clear(g1DistsOuter, 0, _nrSegments);    // necessary ? 
            Array.Clear(g1DistsInner, 0, _nrSegments);    // necessary ? 
            logger.Log(Logger.EventType.Step, "Initialized g1 distance arrays.");

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
            var absorptionsHandle = shader.FindKernel("Absorptions");
            logger.Log(Logger.EventType.ShaderInteraction, "Retrieved kernel handles.");

 
            // make buffers.
            //var _inputBuffer = new ComputeBuffer(Coordinates.Length, sizeof(float)*2);
            //maskBuffer = new ComputeBuffer(Coordinates.Length, sizeof(uint)*3);
            var outputBufferOuter = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var outputBufferInner = new ComputeBuffer(coordinates.Length, sizeof(float)*2);
            var absorptionsBuffer = new ComputeBuffer(coordinates.Length, sizeof(float)*3);
            logger.Log(Logger.EventType.Data, "Created buffers.");
            
            _inputBuffer.SetData(coordinates);
            
            // set buffers for g1 kernel.
            shader.SetBuffer(g1Handle, "segment", _inputBuffer);
            shader.SetBuffer(g1Handle, "distancesInner", outputBufferInner);
            shader.SetBuffer(g1Handle, "distancesOuter", outputBufferOuter);
            logger.Log(Logger.EventType.ShaderInteraction, "Wrote data to buffers.");

            
            // compute g1 distances.
            logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel dispatch.");
            shader.Dispatch(g1Handle, threadGroupsX, 1, 1);
            logger.Log(Logger.EventType.ShaderInteraction, "g1 distances kernel return.");
            
                     
            var loopTs = new TimeSpan();
            var factorsTs = new TimeSpan();
            var absorptions = new Vector3[_nrSegments];
            Array.Clear(absorptions, 0, absorptions.Length);
            
            shader.SetBuffer(absorptionsHandle, "segment", _inputBuffer);

            // for each angle:
            for (int j = 0; j < _nrAnglesTheta; j++) {
                var loopStart = sw.Elapsed;

                // set coordinate buffer. remove?
                shader.SetFloat("cos", (float) Math.Cos((180 - model.GetAngles()[j]) * Math.PI / 180));
                shader.SetFloat("sin", (float) Math.Sin((180 - model.GetAngles()[j]) * Math.PI / 180));
                shader.SetBuffer(absorptionsHandle, "distancesInner", outputBufferInner);
                shader.SetBuffer(absorptionsHandle, "distancesOuter", outputBufferOuter);
                shader.SetBuffer(absorptionsHandle, "absorptions", absorptionsBuffer);
                
                shader.Dispatch(absorptionsHandle, threadGroupsX, 1, 1);
                

                absorptionsBuffer.GetData(absorptions);
                //Debug.Log(string.Join(", ", 
                  //  absorptions.Select(v => v.ToString("G"))));
                var factorStart = sw.Elapsed;
                _absorptionFactors[j] = GetAbsorptionFactor(absorptions);
                var factorStop = sw.Elapsed;
                factorsTs += factorStop - factorStart;
                
                var loopStop = sw.Elapsed;
                loopTs += loopStop - loopStart;
            }
            
            logger.Log(Logger.EventType.ShaderInteraction, "Calculated all absorptions.");
            logger.Log(Logger.EventType.Performance, 
                $"Absorption calculation took {loopTs}, " +
                $"{TimeSpan.FromTicks(loopTs.Ticks/_nrAnglesTheta)} avg. per loop, " + 
                $"{TimeSpan.FromTicks(factorsTs.Ticks/_nrAnglesTheta)} of which for absorption factor calculation.");
            
            // release buffers.
            _inputBuffer.Release();
            outputBufferOuter.Release();
            outputBufferInner.Release();
            absorptionsBuffer.Release();
            logger.Log(Logger.EventType.ShaderInteraction, "Shader buffers released.");

            sw.Stop();
            logger.Log(Logger.EventType.Method, "Compute(): done.");
        }
        
        protected override void Write()
        {
            if (writeFactorsFlag) WriteAbsorptionFactors();
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

        private void WriteAbsorptionFactors()
        {
            var path = Path.Combine("Logs", "Absorptions2D", $"Output n={segmentResolution}.txt");
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
            
            ArrayWriteTools.Write2D(path, headCol, headRow, data);
        }
        
        private void WriteAbsorptionFactorsNative()
        {
            logger.Log(Logger.EventType.Method, "WriteAbsorptionFactors(): started.");

            using (FileStream fileStream = File.Create(Path.Combine("Logs", "Absorptions2D", $"Output n={segmentResolution}.txt")))
            using (BufferedStream buffered = new BufferedStream(fileStream))
            using (StreamWriter writer = new StreamWriter(buffered))
            {
                writer.WriteLine(string.Join("\t", "2 theta","A_{s,sc}", "A_{c,sc}", "A_{c,c}"));
                for (int i = 0; i < _nrAnglesTheta; i++)
                {
                    writer.WriteLine(string.Join("\t", 
                        model.GetAngles()[i].ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].x.ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].y.ToString("G", CultureInfo.InvariantCulture),
                        _absorptionFactors[i].z.ToString("G", CultureInfo.InvariantCulture)));
                }
            }
            
            logger.Log(Logger.EventType.Method, "WriteAbsorptionFactors(): done.");
        }

        #endregion
    }
}