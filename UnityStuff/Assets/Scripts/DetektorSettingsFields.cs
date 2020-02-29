using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DetektorSettingsFields : MonoBehaviour
{

    public InputField fieldPixelSize;
    public InputField fieldResolutionX;
    public InputField fieldResolutionY;
    public InputField fieldDstToSample;
    public InputField fieldOffsetX;
    public InputField fieldOffsetY;
    public InputField fieldPathAngleFile;


    public DetektorSettings detektorSettings;



    public void aktualisiere(bool userInput) {
        if (userInput) {
            if (!fieldOffsetX.text.Equals("") && !fieldOffsetY.text.Equals("")) {
                detektorSettings.offSetFromDownRightEdge.x = float.Parse(fieldOffsetX.text);
                detektorSettings.offSetFromDownRightEdge.y = float.Parse(fieldOffsetY.text);
            }
            if (!fieldPixelSize.text.Equals("")) {
                detektorSettings.pixelsize = float.Parse(fieldPixelSize.text);
            }
            if (!fieldResolutionX.text.Equals("") && !fieldResolutionY.text.Equals("")) {
                detektorSettings.resolution.x = float.Parse(fieldResolutionX.text);
                detektorSettings.resolution.y = float.Parse(fieldResolutionY.text);
            }

            if (!fieldDstToSample.text.Equals("")) {
                detektorSettings.dstToSample = float.Parse(fieldDstToSample.text);
            }

            if (!fieldPathAngleFile.text.Equals("")) {
                detektorSettings.pathToAngleFile = fieldPathAngleFile.text;
            }
        } else {
            fieldPixelSize.text = detektorSettings.pixelsize.ToString();
            fieldOffsetX.text = detektorSettings.offSetFromDownRightEdge.x.ToString();
            fieldOffsetY.text = detektorSettings.offSetFromDownRightEdge.y.ToString();
            fieldDstToSample.text = detektorSettings.dstToSample.ToString();
            fieldResolutionX.text = detektorSettings.resolution.x.ToString();
            fieldResolutionY.text = detektorSettings.resolution.y.ToString();
            fieldPathAngleFile.text = detektorSettings.pathToAngleFile;
        }
        
        
    }
}

[System.Serializable]
public class DetektorSettings {
    public float pixelsize = 1;
    public Vector2 resolution = new Vector2(2048,2048);
    public Vector2 offSetFromDownRightEdge = new Vector2(0,0);
    public float dstToSample=0;
    //Pfad zu .txt
    public string pathToAngleFile="";
    
    private float[] angles;
    private bool useGivenAngles;
    private float dstToSampleNormalized;
    private float pixelSizeNormalized;
}
