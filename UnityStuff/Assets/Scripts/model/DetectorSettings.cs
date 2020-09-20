﻿using System;
using UnityEngine;
using util;

namespace model
{
    [Serializable]
    public class DetectorSettings {
        public float pixelSize;
        public Vector2Int resolution;
        public Vector2 offSetFromDownRightEdge;
        public float distToSample;
        
        // TODO: create new settings class for these parameters.
        public string pathToAngleFile = "";
        public float angleStart;
        public float angleEnd;
        public int angleCount;
        
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        public double GetRatioFromOffset(int pixelIndex, bool vertical)
        {
            return Math.Sqrt(
                Math.Pow(pixelIndex*pixelSize-offSetFromDownRightEdge[vertical ? 1 : 0], 2) + 
                Math.Pow(distToSample, 2)
            ) / distToSample;
        }

        public float GetAngleFromRatio(double ratio)
        {
            return (float) MathTools.AsDegree(Math.Acos(1 / ratio));
        }
    }
}

