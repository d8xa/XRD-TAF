using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;

public class DataSaver : MonoBehaviour{

    //public Settings settings;

    public SettingsFields settingsFields;
    public DetektorSettingsFields detSettingsFields;
    //Application.dataPath nur zum testen, spaeter durch eigenen Pfad ersetzen?
    public static string savePath;
    public InputField loadFileName;
    public Button loadButton;
    public Button saveButton;

    private void Awake() {
        //playerPrefs fuer letztes File
        savePath = Application.dataPath + "/Settings/";

    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            settingSaver();
        }

        if (Input.GetKeyDown(KeyCode.L)) {
            settingLoader();
        }

        if (Input.GetKeyDown(KeyCode.K)) {
            syncSettingData();
        }
        
    }

    private void syncSettingData() {
        //settings.detektorSettings = detSettings.detektorSettings;
    }

    public void settingSaver() {
        string saveDataSet = JsonUtility.ToJson(settingsFields.settings);
        File.WriteAllText(savePath + settingsFields.settings.aufbauBezeichnung + "_set.txt", saveDataSet);
        
        string saveDataDet = JsonUtility.ToJson(detSettingsFields.detektorSettings);
        File.WriteAllText(savePath + settingsFields.settings.aufbauBezeichnung + "_det.txt", saveDataDet);

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
    }
    
    //Daten bei Neustart handlen



}

