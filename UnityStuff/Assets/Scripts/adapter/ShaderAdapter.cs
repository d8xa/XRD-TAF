﻿using System;
using model;
using model.structs;
using UnityEngine;
using UnityEngine.UI;
using util;
using Logger = util.Logger;

namespace adapter
{
    public abstract class ShaderAdapter
    {
        #region Fields

        protected Logger logger;
        protected readonly bool writeFactors;
        
        private protected readonly ComputeShader shader;
        private protected readonly Properties properties;
        private protected readonly Metadata metadata;
        private protected int threadGroupsX;
        
        private protected Vector2[] coordinates;
        private protected int sampleResolution;
        
        private protected ProbeValuePair r;
        private protected ProbeValuePair rSq;
        private protected ProbeValuePair mu;

        private Text _status;

        public void SetStatus(ref Text text) => _status = text;

        internal void SetStatusMessage(string message)
        {
            if (_status != null) _status.text = message;
        }

        #endregion

        #region Constructors

        protected ShaderAdapter(ComputeShader shader, Preset preset, bool writeFactors, Logger logger = null)
        {
            this.shader = shader;
            properties = preset.properties;
            metadata = preset.metadata;
            this.writeFactors = writeFactors;
            if (logger != null) SetLogger(logger);
            
            InitSharedFields();
        }

        #endregion

        #region Methods

        private void InitSharedFields()
        {
            sampleResolution = properties.sample.gridResolution;
            r.cell = properties.sample.totalDiameter / 2;
            r.sample = r.cell - properties.sample.cellThickness;
            rSq = r.squared;
            mu.cell = properties.sample.muCell;
            mu.sample = properties.sample.muSample;

            var boundary = r.cell * (1 + Settings.defaults.sampleAreaMarginDefault);
            coordinates = MathTools.LinSpace2D(-boundary, boundary, sampleResolution);
            threadGroupsX =  (int) Math.Min(Math.Pow(2, 16) - 1, Math.Pow(sampleResolution, 2));
        }
        
        public void Execute()
        {
            InitSharedFields();
            Compute();
            Cleanup();
            if (writeFactors) Write();
        }
        
        protected abstract void Compute();

        protected abstract void Write();

        protected void SetLogger(Logger newLogger)
        {
            logger = newLogger;
        }

        protected virtual void Cleanup() {}

        #endregion
    }
}