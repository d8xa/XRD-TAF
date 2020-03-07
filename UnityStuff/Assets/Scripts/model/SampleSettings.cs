using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SampleSettings {
    public float totalDiameter;
    public float cellThickness;
    public float muSample;
    public float muCell;

    private float probeDiameterNormalized;
    private float cellThicknessNormalized;
    private float totalDiameterNormalized;
}