using System.Collections;
using System.Collections.Generic;
using FoPra.model;
using UnityEngine;
using Ray = FoPra.model.Ray;

[System.Serializable]
public class Settings{

    //Kommentarfunktion
    public string aufbauBezeichnung;
    public Model.Modes mode;

    //muss eh alles gemacht werden
    //public Model.AbsorbtionType absorbtionType;
    //public DetektorSettings detektorSettings;
    //public SampleSettings sampleSettings;
    //public RaySettings raySettings;

    public string loadName;

    public string pathToInputData;
    
    //Berechungsgenauigkeit!
    
    //Integrationsbereich Winkel!
    
    

    
    
    public bool EvaluiereSettings() {
        return EvaluiereDetektorSettings() && EvaluiereSampleSettings() && 
               EvaluiereRaySettings() && EvaluiereModelSettings();
    }
    
    
    private bool EvaluiereDetektorSettings() {
        return true; //TODO

    } 
    
    private bool EvaluiereSampleSettings() {
        return true; //TODO

    } 
    
    private bool EvaluiereRaySettings() {
        return true; //TODO

    } 
    
    private bool EvaluiereModelSettings() {
        return true; //TODO
        
    } 

}
/*
//Detektor-Varbiablen
[System.Serializable]
public class DetektorSettings {
    public float pixelsize = 1;
    public Vector2 resolution = new Vector2(2048,2048);
    public Vector2 offSetFromDownRightEdge = new Vector2(3,2);
    public float dstToSample=2;
    //Pfad zu .txt
    public string pathToAngleFile="";
    
    private float[] angles;
    private bool useGivenAngles;
    private float dstToSampleNormalized;
    private float pixelSizeNormalized;
}
    
//Sample-Variablen
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
    
//Ray-Variablen
[System.Serializable]
public class RaySettings {
    public Ray.StrahlProfil strahlProfil = Ray.StrahlProfil.Rechteck;
    public float height;
    public float width;
    private Vector2 sizeNormalized;
    //    public float offsetToLeft;
    //    public float offsetToUp;
    //    private Vector2 offset;
    
}
*/
