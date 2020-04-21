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
    public float angleStart;
    public float angleEnd;
    public int angleAmount;

}

