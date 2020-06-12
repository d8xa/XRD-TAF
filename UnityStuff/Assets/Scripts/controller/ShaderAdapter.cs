using System;
using FoPra.model;
using FoPra.util;
using UnityEngine;

namespace controller
{
    public abstract class ShaderAdapter
    {
        private protected readonly ComputeShader Shader;
        private protected readonly Model Model;
        private protected int ThreadGroupsX;
    
        private protected Vector2[] Coordinates;
        private protected int SegmentResolution;
        private readonly float _margin;
    
        private protected bool WriteDistances;
        private protected bool WriteFactors;

        protected ShaderAdapter(ComputeShader shader, Model model, float margin, bool writeDistances, bool writeFactors)
        {
            Shader = shader;
            Model = model;
            _margin = margin;
            WriteDistances = writeDistances;
            WriteFactors = writeFactors;
            
            InitSharedFields();
        }

        protected ShaderAdapter(ComputeShader shader, Model model)
        {
            Shader = shader;
            Model = model;
            _margin = 0.2f;
            WriteDistances = false;
            WriteFactors = false;
            
            InitSharedFields();
        }

        void InitSharedFields()
        {
            SegmentResolution = Model.get_segment_resolution();
            Coordinates = MathTools.LinSpace2D(
                -Model.get_r_cell()*(1+_margin), Model.get_r_cell()*(1+_margin), SegmentResolution);
            ThreadGroupsX =  (int) Math.Min(Math.Pow(2, 16) - 1, Math.Pow(SegmentResolution, 2));
        }
        
        public void Execute()
        {
            InitSharedFields();
            Compute();
            // TODO: add Write() delegation logic.
        }
        
        protected abstract void Compute();

        protected abstract void Write();
    }
}