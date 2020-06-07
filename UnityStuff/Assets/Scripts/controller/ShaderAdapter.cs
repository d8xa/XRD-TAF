using FoPra.model;
using FoPra.util;
using UnityEngine;

namespace controller
{
    public abstract class ShaderAdapter
    {
        private protected readonly ComputeShader Shader;
        private protected readonly Model Model;
    
        private protected Vector2[] _coordinates;
        private protected int _segmentResolution;
        private readonly float _margin;
    
        private protected bool _writeDistances;
        private protected bool _writeFactors;

        protected ShaderAdapter(ComputeShader shader, Model model, float margin, bool writeDistances, bool writeFactors)
        {
            Shader = shader;
            Model = model;
            _margin = margin;
            _writeDistances = writeDistances;
            _writeFactors = writeFactors;
            
            InitSharedFields();
        }

        protected ShaderAdapter(ComputeShader shader, Model model)
        {
            Shader = shader;
            Model = model;
            _margin = 0.2f;
            _writeDistances = false;
            _writeFactors = false;
            
            InitSharedFields();
        }

        void InitSharedFields()
        {
            _segmentResolution = Model.get_segment_resolution();
            _coordinates = MathTools.LinSpace2D(
                -Model.get_r_cell()*(1+_margin), Model.get_r_cell()*(1+_margin), _segmentResolution);
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