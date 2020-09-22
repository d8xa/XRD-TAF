using System;
using UnityEngine;
using util;

namespace model.properties
{
    [Serializable]
    public class DetectorProperties {
        public float pixelSize;
        public Vector2Int resolution;
        public Vector2 offSetFromBottomRight;
        public float distToSample;
        
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        public double GetRatioFromOffset(int pixelIndex, bool vertical)
        {
            return Math.Sqrt(
                Math.Pow(pixelIndex*pixelSize-offSetFromBottomRight[vertical ? 1 : 0], 2) + 
                Math.Pow(distToSample, 2)
            ) / distToSample;
        }

        public float GetAngleFromRatio(double ratio)
        {
            return (float) MathTools.AsDegree(Math.Acos(1 / ratio));
        }
    }
}

