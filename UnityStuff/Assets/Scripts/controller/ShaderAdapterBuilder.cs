using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using model;
using UnityEngine;
using Logger = util.Logger;

namespace controller
{
    public class ShaderAdapterBuilder
    {
        private bool _writeFactors;
        private float? _margin;
        [CanBeNull] private Model _model;
        [CanBeNull] private ComputeShader _shader;
        private Model.Mode _mode = Model.Mode.Undefined;
        private static Dictionary<Model.Mode, ComputeShader> _shaderMapping;
        private Logger _logger;

        //private static readonly string ShaderDir = Path.Combine("Assets", "Scripts", "controller");

        private ShaderAdapterBuilder() {}

        public static ShaderAdapterBuilder New()
        {
            return new ShaderAdapterBuilder();
        }

        public ShaderAdapterBuilder SetMode(Model.Mode mode)
        {
            _mode = mode;
            return this;
        }

        public ShaderAdapterBuilder SetLogger(Logger logger)
        {
            _logger = logger;
            return this;
        }

        public ShaderAdapterBuilder AutoSetShader()
        {
            if (_mode == Model.Mode.Undefined)
                throw new InvalidOperationException("Mode must be set.");
            if (_shaderMapping == null)
                throw new InvalidOperationException("Shader must be set.");

            if (!_shaderMapping.ContainsKey(_mode)) 
                throw new KeyNotFoundException("Shader not found.");
            
            _shader = _shaderMapping[_mode];
            return this;
        }

        public ShaderAdapterBuilder SelectShader(Model.Mode mode)
        {
            if (!_shaderMapping.ContainsKey(mode))
                throw new KeyNotFoundException();
            if (mode == Model.Mode.Undefined)
                throw new InvalidOperationException("Mode must be set.");
            
            _shader = _shaderMapping[mode];
            return this;
        }

        public ShaderAdapterBuilder AddShader(Model.Mode mode, ComputeShader shader)
        {
            if (_shaderMapping == null) 
                _shaderMapping = new Dictionary<Model.Mode, ComputeShader>();
            if (!_shaderMapping.ContainsKey(mode)) 
                _shaderMapping.Add(mode, shader);
            return this;
        }

        public ShaderAdapterBuilder SetModel(Model model)
        {
            _model = model;
            return this;
        }

        public ShaderAdapterBuilder SetSegmentMargin(float margin)
        {
            _margin = margin;
            return this;
        }

        public ShaderAdapterBuilder WriteFactors()
        {
            _writeFactors = true;
            return this;
        }

        private float GetDefaultMargin() => 0.2f;

        public ShaderAdapter Build()
        {
            ShaderAdapter adapter = null;
            if (IsComplete())
            {
                switch (_mode)
                {
                    case Model.Mode.Point:
                        adapter = new PointModeAdapter(_shader, _model, _margin ?? GetDefaultMargin(), _writeFactors);
                        break;
                    case Model.Mode.Area:
                        adapter = new PlaneModeAdapter(_shader, _model, _margin ?? GetDefaultMargin(), _writeFactors);
                        break;
                    case Model.Mode.Integrated:
                        adapter = new IntegratedModeAdapter(_shader, _model, _margin ?? GetDefaultMargin(), _writeFactors, _logger);
                        break;
                    case Model.Mode.Testing:
                        adapter = new PlaneModeAdapter(_shader, _model, _margin ?? GetDefaultMargin(), _writeFactors);
                        break;
                }
            }
            return adapter;
        }

        private bool IsComplete()
        {
            try
            {
                if (_shader == null)
                    throw new NullReferenceException("Please specify a shader.");
                else if (_model == null)
                    throw new NullReferenceException("Please specify a model.");
                else if (_mode == Model.Mode.Undefined)
                    throw new NullReferenceException("Please specify a mode.");
                else if (_margin == null)
                    throw new NullReferenceException("");
                return true;
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
        }
    }
}