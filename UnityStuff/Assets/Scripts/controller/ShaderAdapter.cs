using System;
using model;
using UnityEngine;
using util;
using Logger = util.Logger;

namespace controller
{
    public abstract class ShaderAdapter
    {
        protected Logger logger;
        
        private protected readonly ComputeShader shader;
        private protected readonly Model model;
        private protected int threadGroupsX;
    
        private protected Vector2[] coordinates;
        private protected int segmentResolution;
        private readonly float _margin;

        private protected bool writeFactorsFlag;

        protected ShaderAdapter(ComputeShader shader, Model model, float margin, bool writeFactorsFlag, Logger logger = null)
        {
            this.shader = shader;
            this.model = model;
            _margin = margin;
            this.writeFactorsFlag = writeFactorsFlag;
            if (logger != null) SetLogger(logger);
            
            InitSharedFields();
        }

        protected ShaderAdapter(ComputeShader shader, Model model, Logger logger = null)
        {
            this.shader = shader;
            this.model = model;
            _margin = 0.2f;
            writeFactorsFlag = false;
            if (logger != null) SetLogger(logger);

            InitSharedFields();
        }

        private void InitSharedFields()
        {
            segmentResolution = model.GetSegmentResolution();
            coordinates = MathTools.LinSpace2D(
                -model.GetRCell()*(1+_margin), model.GetRCell()*(1+_margin), segmentResolution);
            threadGroupsX =  (int) Math.Min(Math.Pow(2, 16) - 1, Math.Pow(segmentResolution, 2));
        }
        
        public void Execute()
        {
            InitSharedFields();
            Compute();
            Cleanup();
            if (writeFactorsFlag) Write();
        }
        
        protected abstract void Compute();

        protected abstract void Write();

        protected void SetLogger(Logger newLogger)
        {
            logger = newLogger;
        }

        protected virtual void Cleanup() {}
    }
}