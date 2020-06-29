using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class DetectorSettingsFields : MonoBehaviour
{

//    public InputField fieldPixelSize;
//    public InputField fieldResolutionX;
//    public InputField fieldResolutionY;
//    public InputField fieldDstToSample;
//    public InputField fieldOffsetX;
//    public InputField fieldOffsetY;
//    public InputField fieldPathAngleFile;
//
//
//    public DetektorSettings detektorSettings;
//
//
//    public void fillInDefaults(DetektorSettings defaultDetektorSettings) {
//        if (fieldOffsetX.text.Equals("") && fieldOffsetY.text.Equals("")) {
//            detektorSettings.offSetFromDownRightEdge = defaultDetektorSettings.offSetFromDownRightEdge;
//        }
//        if (fieldPixelSize.text.Equals("")) {
//            detektorSettings.pixelsize = defaultDetektorSettings.pixelsize;
//        }
//        if (fieldResolutionX.text.Equals("") && fieldResolutionY.text.Equals("")) {
//            detektorSettings.resolution = defaultDetektorSettings.resolution;
//        }
//
//        if (fieldDstToSample.text.Equals("")) {
//            detektorSettings.dstToSample = defaultDetektorSettings.dstToSample;
//        }
//
//        if (fieldPathAngleFile.text.Equals("")) {
//            detektorSettings.pathToAngleFile = defaultDetektorSettings.pathToAngleFile;
//        }
//        aktualisiere(false);
//    }
//
//    public void aktualisiere(bool userInput) {
//        if (userInput) {
//            if (!fieldOffsetX.text.Equals("") && !fieldOffsetY.text.Equals("")) {
//                detektorSettings.offSetFromDownRightEdge.x = float.Parse(fieldOffsetX.text);
//                detektorSettings.offSetFromDownRightEdge.y = float.Parse(fieldOffsetY.text);
//            }
//            if (!fieldPixelSize.text.Equals("")) {
//                detektorSettings.pixelsize = float.Parse(fieldPixelSize.text);
//            }
//            if (!fieldResolutionX.text.Equals("") && !fieldResolutionY.text.Equals("")) {
//                detektorSettings.resolution.x = float.Parse(fieldResolutionX.text);
//                detektorSettings.resolution.y = float.Parse(fieldResolutionY.text);
//            }
//
//            if (!fieldDstToSample.text.Equals("")) {
//                detektorSettings.dstToSample = float.Parse(fieldDstToSample.text);
//            }
//
//            if (!fieldPathAngleFile.text.Equals("")) {
//                detektorSettings.pathToAngleFile = fieldPathAngleFile.text;
//            }
//        } else {
//            fieldPixelSize.text = detektorSettings.pixelsize.ToString();
//            fieldOffsetX.text = detektorSettings.offSetFromDownRightEdge.x.ToString();
//            fieldOffsetY.text = detektorSettings.offSetFromDownRightEdge.y.ToString();
//            fieldDstToSample.text = detektorSettings.dstToSample.ToString();
//            fieldResolutionX.text = detektorSettings.resolution.x.ToString();
//            fieldResolutionY.text = detektorSettings.resolution.y.ToString();
//            fieldPathAngleFile.text = detektorSettings.pathToAngleFile;
//        }
//        
//        
//    }
}
