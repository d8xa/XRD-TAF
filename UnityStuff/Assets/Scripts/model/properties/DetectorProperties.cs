using System;
using System.Runtime.Serialization;
using UnityEngine;
using util;

namespace model.properties
{
    [DataContract]
    public class DetectorProperties {
        [DataMember] public Vector2 pixelSize;
        [DataMember] public Vector2Int resolution;
        [DataMember] public Vector2 offset;
        [DataMember] public float distToSample;
        
        public static DetectorProperties Initialize()
        {
            return new DetectorProperties();
        }
        
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        // TODO: check results of Area Mode after inverting ratio fraction.
        public double GetRatioFromIndex(int pixelIndex, bool vertical)
        {
            return distToSample / Math.Sqrt(
                Math.Pow(pixelIndex*pixelSize[vertical ? 1 : 0] - offset[vertical ? 1 : 0], 2) + 
                Math.Pow(distToSample, 2)
            );
        }
        
        /// <summary>
        /// Calculates the angle between offset point, sample center and current point on the detector,
        /// based on the distance between offset point and current position on detector.
        /// </summary>
        /// <param name="pixelIndex">The index of the current point on the detector..</param>
        /// <param name="vertical">Toggle between horizontal and vertical angle.</param>
        public double GetAngleFromIndex(int pixelIndex, bool vertical)
        {
            return Math.Atan((pixelIndex*pixelSize[vertical ? 1 : 0] - offset[vertical ? 1 : 0])/distToSample);
        }

        /// <summary>
        /// Calculates the angle between offset point, sample center and current point on the detector,
        /// based on the distance between offset point and current position on detector.
        /// </summary>
        /// <param name="length">The length of the opposite side of the angle, i.e. the length on the detector.</param>
        /// <param name="vertical">Toggle between horizontal and vertical angle.</param>
        public double GetAngleFromLength(double length, bool vertical)
        {
            return Math.Atan((length-offset[vertical ? 1 : 0])/distToSample);
        }

        public float GetAngleFromRatio(double ratio)
        {
            return (float) MathTools.AsDegree(Math.Acos(ratio));
        }
    }
}

