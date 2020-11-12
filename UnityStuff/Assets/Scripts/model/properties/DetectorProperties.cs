using System;
using System.Runtime.Serialization;
using UnityEngine;

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

        /// <summary>
        /// Calculates the angle between offset point, sample center and current point on the detector,
        /// based on the distance between offset point and current position on detector.
        /// </summary>
        /// <param name="pixelIndex">The index of the current point on the detector..</param>
        /// <param name="vertical">Toggle between horizontal and vertical angle.</param>
        public double GetAngleFromIndex(int pixelIndex, bool vertical = false)
        {
            return Math.Atan(((pixelIndex + 0.5) * pixelSize[vertical ? 1 : 0] - offset[vertical ? 1 : 0]) /
                             distToSample);
        }
    }
}