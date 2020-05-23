using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SocialPlatforms;


[System.Serializable]
public class DetektorSettings {
    public float pixelsize;
    public Vector2 resolution;
    public Vector2 offSetFromDownRightEdge;
    [FormerlySerializedAs("dstToSample")] public float distToSample;
    //Pfad zu .txt
    public string pathToAngleFile="";
    public float angleStart;
    public float angleEnd;
    public int angleAmount;

    public float getAngleFromOffset(int pixelIndex, bool vertical)
    {
        return (float) (Math.Atan(
                Math.Abs(pixelIndex*pixelsize-offSetFromDownRightEdge[vertical ? 1 : 0])/distToSample
                ) * 180.0 / Math.PI);
    }
}

