using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using model;
using model.properties;
using UnityEngine;
using Logger = util.Logger;

namespace adapter
{
    public class ShaderAdapterBuilder
    {
        #region Fields

        private bool _writeFactors;
        private float? _margin;
        [CanBeNull] private ComputeShader _shader;
        [CanBeNull] private Properties _properties;
        private static Dictionary<AbsorptionProperties.Mode, ComputeShader> _shaderMapping;
        private Logger _logger;
        
        #endregion
        
        #region Builder

        private ShaderAdapterBuilder() {}

        public static ShaderAdapterBuilder New()
        {
            return new ShaderAdapterBuilder();
        }
        
        public ShaderAdapter Build()
        {
            ShaderAdapter adapter = null;
            if (IsComplete())
            {
                switch (_properties.absorption.mode)
                {
                    case AbsorptionProperties.Mode.Point:
                        adapter = new PointModeAdapter(_shader, _properties, _writeFactors, _logger);
                        break;
                    case AbsorptionProperties.Mode.Area:
                        adapter = new PlaneModeAdapter(_shader, _properties, _writeFactors, _logger);
                        break;
                    case AbsorptionProperties.Mode.Integrated:
                        adapter = new IntegratedModeAdapter(_shader, _properties, _writeFactors, _logger);
                        break;
                }
            }
            return adapter;
        }

        // TODO: validate preset here.
        private bool IsComplete()
        {
            try
            {
                if (_shader == null)
                    throw new NullReferenceException("Please specify a shader.");
                else if (_properties == null)
                    throw new NullReferenceException("Please specify properties.");
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

        #endregion
        
        #region Setter methods

        public ShaderAdapterBuilder SetLogger(Logger logger)
        {
            _logger = logger;
            return this;
        }

        public ShaderAdapterBuilder SetProperties(Preset preset)
        {
            _properties = preset.properties;
            return this;
        }

        public ShaderAdapterBuilder SetSegmentMargin(float margin)
        {
            _margin = margin;
            return this;
        }

        public ShaderAdapterBuilder SetWriteFactors(bool enabled)
        {
            _writeFactors = enabled;
            return this;
        }

        #endregion
        
        public ShaderAdapterBuilder AutoSetShader()
        {
            var mode = _properties.absorption.mode;
            if (mode == AbsorptionProperties.Mode.Undefined)
                throw new InvalidOperationException("Mode must be set.");
            if (_shaderMapping == null)
                throw new InvalidOperationException("Shader must be set.");

            if (!_shaderMapping.ContainsKey(mode)) 
                throw new KeyNotFoundException("Shader not found.");
            
            _shader = _shaderMapping[mode];
            return this;
        }

        public ShaderAdapterBuilder SelectShader(AbsorptionProperties.Mode mode)
        {
            if (!_shaderMapping.ContainsKey(mode))
                throw new KeyNotFoundException();
            if (mode == AbsorptionProperties.Mode.Undefined)
                throw new InvalidOperationException("Mode must be set.");
            
            _shader = _shaderMapping[mode];
            return this;
        }

        public ShaderAdapterBuilder AddShader(AbsorptionProperties.Mode mode, ComputeShader shader)
        {
            if (_shaderMapping == null) 
                _shaderMapping = new Dictionary<AbsorptionProperties.Mode, ComputeShader>();
            if (!_shaderMapping.ContainsKey(mode)) 
                _shaderMapping.Add(mode, shader);
            return this;
        }
    }
}