using System;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using util;

namespace model
{
    public class Model
    {
        [Serializable]
        public enum Mode {
            Point = 0, 
            Area = 1, 
            Integrated = 2, 
            Testing = 3, 
            Undefined = 4
        }
        public enum AbsorptionType {
            All, Cell, Sample, CellAndSample
        }
        public enum RayProfile {
            Oval, Rectangle
        }

        public readonly Settings settings;
        public readonly DetectorSettings detector;
        private readonly SampleSettings _sample;
        public RaySettings ray;

        private int _segmentResolution;
        private float _rSample;
        private float _rSampleSq;
        private float _rCell;
        private float _rCellSq;
        private float _muSample;
        private float _muCell;
        private float[] _angles;
        private float[] _cos3D;

        private void GatherMetaData()
        {
            // set case-shared variables:
            _segmentResolution = (int) _sample.gridResolution;
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
                    _angles = Parser.ImportAngles(
                        Path.Combine(Application.dataPath, "Input", detector.pathToAngleFile + ".txt"));
                    break;

                case Mode.Area:
                    _angles = Enumerable.Range(0, detector.resolution.x)
                        .Select(j => detector.GetRatioFromIndex(j, false))
                        .Select(v => detector.GetAngleFromRatio(v))
                        .ToArray();
                    _cos3D = Enumerable.Range(0, detector.resolution.y)
                        .Select(j => (float) detector.GetRatioFromIndex(j, true))
                        .Select(v => 1/v)
                        .ToArray();
                    break;

                case Mode.Integrated:
                    _angles = Parser.ImportAngles(
                        Path.Combine(Application.dataPath, "Input", detector.pathToAngleFile + ".txt"));
                    break;
                
                case Mode.Testing:
                    _angles = Enumerable.Range(0, detector.resolution.x)
                        .Select(j => detector.GetRatioFromIndex(j, false))
                        .Select(v => detector.GetAngleFromRatio(v))
                        .ToArray();
                    _cos3D = Enumerable.Range(0, detector.resolution.y)
                        .Select(j => (float) detector.GetRatioFromIndex(j, true))
                        .ToArray();
                    break;
            }
        }

        public Model(Settings settings, DetectorSettings detector, SampleSettings sample
            //, RaySettings ray
            ) {
            this.settings = settings;
            this.detector = detector;
            this._sample = sample;
            //this.ray = ray;
            GatherMetaData();
        }

        public float GetAngleAt(int index) => _angles[index];

        public int GetSegmentResolution() => _segmentResolution;
    
        public float GetRSample() => _rSample;

        public float GetRSampleSq() => _rSampleSq;

        public float GetRCell() => _rCell;

        public float GetRCellSq() => _rCellSq;

        public float GetMuSample() => _muSample;

        public float GetMuCell() => _muCell;

        public float[] GetAngles() => _angles;
        
        public float[] GetCos3D() => _cos3D;
    }
}