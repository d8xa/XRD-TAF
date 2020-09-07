﻿using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace model
{
    [Serializable]
    public class DetectorSettings {
        [FormerlySerializedAs("pixelsize")] public float pixelSize;
        public Vector2Int resolution;
        public Vector2 offSetFromDownRightEdge;
        [FormerlySerializedAs("dstToSample")] public float distToSample;
        public string pathToAngleFile = "";
        public float angleStart;
        public float angleEnd;
        [FormerlySerializedAs("angleAmount")] public int angleCount;

        public double GetRatioFromOffset(int pixelIndex, bool vertical)
        {
            return Math.Sqrt(
                Math.Pow(pixelIndex*pixelSize-offSetFromDownRightEdge[vertical ? 1 : 0], 2) + 
                Math.Pow(distToSample, 2)
            ) / distToSample;
        }

        public float GetAngleFromRatio(double ratio)
        {
            return (float) (Math.Acos(1 / ratio) * 180.0 / Math.PI);
        }
    }
}

