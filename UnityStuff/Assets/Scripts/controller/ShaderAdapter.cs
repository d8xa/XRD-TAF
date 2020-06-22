using System;
using FoPra.model;
using model;
using UnityEngine;
using util;
using Logger = util.Logger;

namespace controller
{
    public abstract class ShaderAdapter
    {
        protected Logger logger;
        
        private protected readonly ComputeShader Shader;
        private protected readonly Model Model;
        private protected int ThreadGroupsX;
    
        private protected Vector2[] Coordinates;
        private protected int SegmentResolution;
        private readonly float _margin;

        private protected bool WriteFactorsFlag;

        protected ShaderAdapter(ComputeShader shader, Model model, float margin, bool writeFactorsFlag)
        {
            Shader = shader;
            Model = model;
            _margin = margin;
            WriteFactorsFlag = writeFactorsFlag;
            
            InitSharedFields();
        }

        protected ShaderAdapter(ComputeShader shader, Model model)
        {
            Shader = shader;
            Model = model;
            _margin = 0.2f;
            WriteFactorsFlag = false;
            
            InitSharedFields();
        }

        private void InitSharedFields()
        {
            SegmentResolution = Model.GetSegmentResolution();
            Coordinates = MathTools.LinSpace2D(
                -Model.GetRCell()*(1+_margin), Model.GetRCell()*(1+_margin), SegmentResolution);
            ThreadGroupsX =  (int) Math.Min(Math.Pow(2, 16) - 1, Math.Pow(SegmentResolution, 2));
        }
        
        public void Execute()
        {
            InitSharedFields();
            Compute();
            Cleanup();
            if (WriteFactorsFlag) Write();
        }
        
        protected abstract void Compute();

        protected abstract void Write();

        protected void SetLogger(Logger logger)
        {
            this.logger = logger;
        }

        protected virtual void Cleanup() {}
    }
}