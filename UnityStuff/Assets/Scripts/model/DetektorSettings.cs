using System;
using UnityEngine;
using UnityEngine.Serialization;


[Serializable]
public class DetektorSettings {
    public float pixelsize;
    public Vector2Int resolution;
    public Vector2 offSetFromDownRightEdge;
    [FormerlySerializedAs("dstToSample")] public float distToSample;
    //Pfad zu .txt
    public string pathToAngleFile = "";
    public float angleStart;
    public float angleEnd;
    [FormerlySerializedAs("angleAmount")] public int angleCount;

    public double GetRatioFromOffset(int pixelIndex, bool vertical)
    {
        return Math.Sqrt(
            Math.Pow(pixelIndex*pixelsize-offSetFromDownRightEdge[vertical ? 1 : 0], 2) + 
            Math.Pow(distToSample, 2)
        ) / distToSample;
    }

    public float GetAngleFromRatio(double ratio)
    {
        return (float) (Math.Acos(1 / ratio) * 180.0 / Math.PI);
    }
}

