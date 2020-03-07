using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class DetektorSettings {
    public float pixelsize;
    public Vector2 resolution;
    public Vector2 offSetFromDownRightEdge;
    public float dstToSample;
    //Pfad zu .txt
    public string pathToAngleFile="";

    private float[] angles;
    private bool useGivenAngles;
    private float dstToSampleNormalized;
    private float pixelSizeNormalized;
}

