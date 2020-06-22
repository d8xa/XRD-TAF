﻿using System;
using UnityEngine;
using System.IO;
using System.Linq;
using controller;
using model;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DataHandler : MonoBehaviour{

    //public Settings settings;

    public SettingsFields settingsFields;
//    public DetektorSettingsFields detSettingsFields;
//    public SampleSettingsFields sampleSettingsFields;
    //TODO
//    public RaySettingsFields raySettingsFields;
    //Application.dataPath nur zum testen, spaeter durch eigenen Pfad ersetzen?
    public static string savePath;
    public InputField loadFileName;
    public Button loadButton;
    public Button saveButton;
    public Button submitButton;
    public ComputeShader pointModeShader;
    public ComputeShader planeModeShader;
    public ComputeShader planeModeShaderBf;
    //public ComputeShader integratedModeShader;

    [FormerlySerializedAs("logicHandler")] 
    public ShaderAdapter shaderAdapter;
    public LogicHandler logicHandler;

    private void Awake() {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 30;
        UpdatePath();
    }

    private void UpdatePath()
    {
        savePath = Path.Combine(Path.GetFullPath(Application.dataPath), "Settings");
    }

    public void fillInBlanks()
    {
        UpdatePath();
        if (File.Exists(Path.Combine(savePath, "Default_set.txt"))) {
            settingsFields.FillInDefaults(
                JsonUtility.FromJson<Settings>(File.ReadAllText(Path.Combine(savePath, "Default_set.txt"))));
        }
        if (File.Exists(Path.Combine(savePath, "Default_det.txt"))) {
            settingsFields.FillInDefaults(
                JsonUtility.FromJson<DetektorSettings>(File.ReadAllText(Path.Combine(savePath, "Default_det.txt"))));
        }
        if (File.Exists(Path.Combine(savePath, "Default_sam.txt"))) {
            settingsFields.FillInDefaults(
                JsonUtility.FromJson<SampleSettings>(File.ReadAllText(Path.Combine(savePath, "Default_sam.txt"))));
        }
    }

    public void submitToComputing()
    {
        fillInBlanks();
        
        var alphaRatios = Enumerable.Range(0, settingsFields.detektorSettings.resolution.y)
            .Select(j => settingsFields.detektorSettings.GetRatioFromOffset(j, true))
            .Select(v => v.ToString("G"))
            .ToArray();
        var thetaAngles = Enumerable.Range(0, settingsFields.detektorSettings.resolution.x)
            .Reverse()
            .Select(j => settingsFields.detektorSettings.GetRatioFromOffset(j, false))
            .Select(v => Math.Acos(1.0/v) * 180.0 / Math.PI)
            .Select(v => v.ToString("G"))
            .ToArray();
        
        var saveDir = Path.Combine("Logs", "Absorptions3D", "Data");
        Directory.CreateDirectory(saveDir);

        var saveName = $"Ratios m={settingsFields.detektorSettings.resolution.y}.txt";
        File.WriteAllLines(Path.Combine(saveDir, saveName), alphaRatios);
        
        saveName = $"Angles n={settingsFields.detektorSettings.resolution.x}.txt";
        File.WriteAllLines(Path.Combine(saveDir, saveName), thetaAngles);
        /*
        Debug.Log("Alpha ratios: " + 
                  string.Join(", ", 
            Enumerable.Range(0, settingsFields.detektorSettings.resolution.y)
                .Select(j => settingsFields.detektorSettings.GetRatioFromOffset(j, true))
                .Select(v => v.ToString("F5"))
                .ToArray()
        ));
        
        Debug.Log("Theta angles from ratios: " + 
                  string.Join(", ", 
            Enumerable.Range(0, settingsFields.detektorSettings.resolution.x)
                .Select(j => settingsFields.detektorSettings.GetRatioFromOffset(j, false))
                .Select(v => Math.Acos(1/v) * 180.0 / Math.PI)
                .Select(v => v.ToString("F5"))
                .ToArray()
        ));
        
        Debug.Log("Theta angles native: " + 
                  string.Join(", ", 
                      Enumerable.Range(0, settingsFields.detektorSettings.resolution.x)
                          .Select(j => settingsFields.detektorSettings.GetRatioFromOffset(j, false))
                          .Select(ratio => settingsFields.detektorSettings.GetAngleFromRatio(ratio))
                          .Select(v => v.ToString("F5"))
                          .ToArray()
                  ));
        */

        ///*
        shaderAdapter = ShaderAdapterBuilder.New()
            .SetMode(settingsFields.settings.mode)
            .SetModel(settingsFields.MakeModel())
            .AddShader(Model.Mode.Point, pointModeShader)
            .AddShader(Model.Mode.Area, planeModeShader)
            .AddShader(Model.Mode.Testing, planeModeShaderBf)
            //.AddShader(Model.Mode.Integrated, integratedModeShader)
            .SetSegmentMargin(0.2f)
            .AutoSetShader()
            //.WriteFactors()
            .Build();
            
        //shaderAdapter.Execute();
        //*/
    }

    public void SaveSettings() {
        string saveDataSet = JsonUtility.ToJson(settingsFields.settings);
        File.WriteAllText(Path.Combine(savePath, settingsFields.settings.aufbauBezeichnung + "_set.txt"), saveDataSet);
        
        string saveDataDet = JsonUtility.ToJson(settingsFields.detektorSettings);
        File.WriteAllText(Path.Combine(savePath, settingsFields.settings.aufbauBezeichnung + "_det.txt"), saveDataDet);
        
        string saveDataSam = JsonUtility.ToJson(settingsFields.sampleSettings);
        File.WriteAllText(Path.Combine(savePath, settingsFields.settings.aufbauBezeichnung + "_sam.txt"), saveDataSam);
    }

    public void LoadSettings() {
        if (File.Exists(Path.Combine(savePath, loadFileName.text + "_set.txt"))) {
            string loadedDataSet = File.ReadAllText(Path.Combine(savePath, loadFileName.text + "_set.txt"));
            settingsFields.settings = JsonUtility.FromJson<Settings>(loadedDataSet);
            settingsFields.ModeChanged();
            settingsFields.DataChanged();
        }
        
        if (File.Exists(Path.Combine(savePath, loadFileName.text + "_det.txt"))) {
            string loadedDataDet = File.ReadAllText(Path.Combine(savePath, loadFileName.text + "_det.txt"));
            settingsFields.detektorSettings = JsonUtility.FromJson<DetektorSettings>(loadedDataDet);
            settingsFields.DataChanged();
        }

        if (File.Exists(Path.Combine(savePath, loadFileName.text + "_sam.txt"))) {
            string loadedDataSam = File.ReadAllText(Path.Combine(savePath, loadFileName.text + "_sam.txt"));
            settingsFields.sampleSettings = JsonUtility.FromJson<SampleSettings>(loadedDataSam);
            settingsFields.DataChanged();
        }
    }
    
    //Daten bei Neustart handlen
    
}



