using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using model;
using model.properties;
using tests;
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
        private Preset _preset;
        private static Dictionary<AbsorptionProperties.Mode, ComputeShader> _shaderMapping;
        private Logger _logger;
        private PerformanceReport _report;
        private double[] _angles;
        
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
            if (!IsComplete()) return null;
            switch (_preset.properties.absorption.mode)
            {
                case AbsorptionProperties.Mode.Point:
                    adapter = new PointModeAdapter(_shader, _preset, _writeFactors, _logger, _angles, _report);
                    break;
                case AbsorptionProperties.Mode.Area:
                    adapter = new PlaneModeAdapter(_shader, _preset, _writeFactors, _logger, _report);
                    break;
                case AbsorptionProperties.Mode.Integrated:
                    adapter = new IntegratedModeAdapter(_shader, _preset, _writeFactors, _logger, _angles, _report);
                    break;
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
                if (_preset == null)
                    throw new NullReferenceException("Please specify properties.");
                if (_margin == null)
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
        
        public ShaderAdapterBuilder SetPerformanceReport(PerformanceReport report)
        {
            _report = report;
            return this;
        }

        public ShaderAdapterBuilder SetProperties(Preset preset)
        {
            _preset = preset;
            return this;
        }

        public ShaderAdapterBuilder SetSegmentMargin(float margin)
        {
            _margin = margin;
            return this;
        }
        
        public ShaderAdapterBuilder SetAngles(double[] angles)
        {
            _angles = angles;
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
            var mode = _preset.properties.absorption.mode;
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