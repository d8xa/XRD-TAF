using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FoPra.model;
using UnityEngine;
using util;

namespace model
{
    public class Model
    {
        [Serializable]
        public enum Mode {
            Point, Area, Integrated, Testing, Undefined
        }
        public enum AbsorptionType {
            All, Cell, Sample, CellAndSample
        }
        public enum RayProfile {
            Oval, Rectangle
        }

        public readonly Settings settings;
        public readonly DetektorSettings detector;
        private readonly SampleSettings _sample;
        public RaySettings ray;

        private int _segmentResolution;
        private float _rSample;
        private float _rSampleSq;
        private float _rCell;
        private float _rCellSq;
        private float _muSample;
        private float _muCell;
        private float[] _angles2D;
        private float[] _anglesIntegrated;    // TODO: check if both angle lists are necessary, else use one list.
        private float[] _cos3D;

        private void calculate_meta_data()
        {
            // set case-shared variables:
            _segmentResolution = (int) settings.computingAccuracy; // TODO: rename text field label in GUI.
            _rSample = _sample.totalDiameter / 2 - _sample.cellThickness;
            _rSampleSq = (float) Math.Pow(_rSample, 2);
            _rCell = _sample.totalDiameter / 2;
            _rCellSq = (float) Math.Pow(_rCell, 2);
            _muSample = _sample.muSample;
            _muCell = _sample.muCell;
            
            // set case-specific variables:
            switch (settings.mode)
            {
                case Mode.Point:
                    _angles2D = ImportAngles();
                    break;

                case Mode.Area:
                    _angles2D = Enumerable.Range(0, (int) detector.resolution.x)
                        .Select(j => detector.GetAngleFromOffset(j, false))
                        .ToArray();
                    _cos3D = Enumerable.Range(0, (int) detector.resolution.y)
                        .Select(j => detector.GetRatioFromOffset(j, true))
                        .ToArray();
                    break;

                case Mode.Integrated:
                    _anglesIntegrated = MathTools.LinSpace1D(
                        detector.angleStart,
                        detector.angleEnd,
                        detector.angleAmount);
                    break;
            }
        }

        public Model(Settings settings, DetektorSettings detector, SampleSettings sample/*, RaySettings ray*/) {
            this.settings = settings;
            this.detector = detector;
            this._sample = sample;
            //this.ray = ray;
            calculate_meta_data();
        }

        private float[] ImportAngles()
        {
            var text = "";
            var path = Path.Combine(Application.dataPath, "Input", detector.pathToAngleFile + ".txt");
            if (File.Exists(path))
            {
                using (var reader = new StreamReader(path))
                    text = reader.ReadToEnd();
            }
      
            return text
                .Trim(' ')
                .Split('\n')
                .Where(s => s.Length > 0)
                .Select(s => float.Parse(s, CultureInfo.InvariantCulture))
                .ToArray();
        }

        public int GetSegmentResolution() => _segmentResolution;
    
        public float GetRSample() => _rSample;

        public float GetRSampleSq() => _rSampleSq;

        public float GetRCell() => _rCell;

        public float GetRCellSq() => _rCellSq;

        public float GetMuSample() => _muSample;

        public float GetMuCell() => _muCell;

        public float[] GetAngles2D() => _angles2D;

        public float[] GetAnglesIntegrated() => _anglesIntegrated;

        public float[] GetCos3D() => _cos3D;
    }
}