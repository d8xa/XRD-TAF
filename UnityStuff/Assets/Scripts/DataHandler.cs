using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using FoPra.model;
using FoPra.tests;
using UnityEngine.UI;

public class DataHandler : MonoBehaviour{

    //public Settings settings;

    public SettingsFields settingsFields;
    public DetektorSettingsFields detSettingsFields;
    public SampleSettingsFields sampleSettingsFields;
    //TODO
    public RaySettingsFields raySettingsFields;
    //Application.dataPath nur zum testen, spaeter durch eigenen Pfad ersetzen?
    public static string savePath;
    public InputField loadFileName;
    public Button loadButton;
    public Button saveButton;
    public Button submitButton;
    public ComputeShader computeShader;

    public LogicHandler logicHandler;

    private void Awake() {
        //playerPrefs fuer letztes File
        //TODO Pfadseperator einfuegen!!!1!!!!1elf! 
        savePath = Application.dataPath + "/Settings/";
        

    }

    public void fillInBlanks() {
        if (File.Exists(savePath + "Default_set.txt")) {
            settingsFields.fillInDefaults(JsonUtility.FromJson<Settings>(File.ReadAllText(savePath + "Default_set.txt")));
        }
        if (File.Exists(savePath + "Default_det.txt")) {
            detSettingsFields.fillInDefaults(JsonUtility.FromJson<DetektorSettings>(File.ReadAllText(savePath + "Default_det.txt")));
        }
        if (File.Exists(savePath + "Default_sam.txt")) {
            sampleSettingsFields.fillInDefaults(JsonUtility.FromJson<SampleSettings>(File.ReadAllText(savePath + "Default_sam.txt")));
        }
    }

    public void submitToComputing() {
        fillInBlanks();
        TestSuite.test_Distances2D(computeShader);
    }

    public void settingSaver() {
        string saveDataSet = JsonUtility.ToJson(settingsFields.settings);
        File.WriteAllText(savePath + settingsFields.settings.aufbauBezeichnung + "_set.txt", saveDataSet);
        
        string saveDataDet = JsonUtility.ToJson(detSettingsFields.detektorSettings);
        File.WriteAllText(savePath + settingsFields.settings.aufbauBezeichnung + "_det.txt", saveDataDet);
        
        string saveDataSam = JsonUtility.ToJson(sampleSettingsFields.sampleSettings);
        File.WriteAllText(savePath + settingsFields.settings.aufbauBezeichnung + "_sam.txt", saveDataSam);

    }

    public void settingLoader() {
        if (File.Exists(savePath + loadFileName.text + "_set.txt")) {
            string loadedDataSet = File.ReadAllText(savePath + loadFileName.text + "_set.txt");
            settingsFields.settings = JsonUtility.FromJson<Settings>(loadedDataSet);
            settingsFields.aktualisiere(false);
        }
        
        if (File.Exists(savePath + loadFileName.text + "_det.txt")) {
            string loadedDataDet = File.ReadAllText(savePath + loadFileName.text + "_det.txt");
            detSettingsFields.detektorSettings = JsonUtility.FromJson<DetektorSettings>(loadedDataDet);
            detSettingsFields.aktualisiere(false);
        }
        
        if (File.Exists(savePath + loadFileName.text + "_sam.txt")) {
            string loadedDataSam = File.ReadAllText(savePath + loadFileName.text + "_sam.txt");
            sampleSettingsFields.sampleSettings = JsonUtility.FromJson<SampleSettings>(loadedDataSam);
            sampleSettingsFields.aktualisiere(false);
        }
    }
    
    //Daten bei Neustart handlen
    
}



