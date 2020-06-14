using System;
using System.Collections.Generic;
using System.IO;
using FoPra.model;
using UnityEngine;

namespace controller
{
    public class ShaderAdapterBuilder
    {
        private bool _writeDistances;
        private bool _writeFactors;
        private float _margin;
        private Model _model;
        private ComputeShader _shader;
        private Model.Mode _mode;
        private static readonly Dictionary<Model.Mode, string> ShaderMapping = new Dictionary<Model.Mode, string>
        {
            {Model.Mode.Point, "PointAbsorptionShader"}, 
            {Model.Mode.Area, "PlaneAbsorptionShader"},
            {Model.Mode.Integrated, "IntegratedAbsorptionShader"},
            {Model.Mode.Testing, "Distances2D"}
        };
        private static readonly string ShaderDir = Path.Combine("Assets", "Scripts", "controller");

        private ShaderAdapterBuilder() {}

        public static ShaderAdapterBuilder New()
        {
            return new ShaderAdapterBuilder();
        }

        public static ShaderAdapterBuilder GetDefault()
        {
            return New()
                .SetMode(Model.Mode.Point)
                .AutoSetShader()
                .WriteFactors()
                .WriteDistances();
        }

        private ComputeShader getShader(Model.Mode mode)
        {
            return (ComputeShader) Resources.Load(Path.Combine(ShaderDir, ShaderMapping[mode]));
        }

        public ShaderAdapterBuilder AutoSetShader()
        {
            if (_mode == null) 
                throw new NullReferenceException("Please specify mode.");

            _shader = getShader(_mode);
            return this;
        }

        public ShaderAdapterBuilder SetMode(Model.Mode mode)
        {
            _mode = mode;
            return this;
        }

        public ShaderAdapterBuilder SetShader(ComputeShader shader)
        {
            _shader = shader;
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

        public ShaderAdapterBuilder WriteDistances()
        {
            _writeDistances = true;
            return this;
        }

        public ShaderAdapterBuilder WriteFactors()
        {
            _writeFactors = true;
            return this;
        }

        public ShaderAdapter Build()
        {
            ShaderAdapter adapter = null;
            if (IsComplete())
            {
                switch (_mode)
                {
                    case Model.Mode.Point:
                        adapter = new PointModeAdapter(_shader, _model, _margin, _writeDistances, _writeFactors);
                        break;
                    case Model.Mode.Area:
                        throw new NotImplementedException();
                    case Model.Mode.Integrated:
                        throw new NotImplementedException();
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
                else if (_mode == null)
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