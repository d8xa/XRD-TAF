using System.Collections.Generic;
using UnityEngine;
using System.IO;
using controller;
using model;
using ui;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Logger = util.Logger;

public class DataHandler : MonoBehaviour{

    //public Settings settings;

    public SettingsFields settingsFields;
    private static string _savePath;
    public InputField loadFileName;
    public Button loadButton;
    public Button saveButton;
    public Button submitButton;
    public ComputeShader pointModeShader;
    public ComputeShader planeModeShader;
    public ComputeShader integratedModeShader;

    [FormerlySerializedAs("logicHandler")] 
    public ShaderAdapter shaderAdapter;

    private readonly ShaderAdapterBuilder _builder = ShaderAdapterBuilder.New();

    private void Awake() {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 30;
        
        _builder
            .AddShader(Model.Mode.Point, pointModeShader)
            .AddShader(Model.Mode.Area, planeModeShader)
            .AddShader(Model.Mode.Integrated, integratedModeShader)
            .SetSegmentMargin(0.2f);
        
        UpdatePath();
    }

    private void UpdatePath()
    {
        _savePath = Path.Combine(Path.GetFullPath(Application.dataPath), "Settings");
    }

    private void FillInBlanks()
    {
        UpdatePath();
        if (File.Exists(Path.Combine(_savePath, "Default_set.txt"))) {
            settingsFields.FillInDefaults(
                JsonUtility.FromJson<Settings>(File.ReadAllText(Path.Combine(_savePath, "Default_set.txt"))));
        }
        if (File.Exists(Path.Combine(_savePath, "Default_det.txt"))) {
            settingsFields.FillInDefaults(
                JsonUtility.FromJson<DetectorSettings>(File.ReadAllText(Path.Combine(_savePath, "Default_det.txt"))));
        }
        if (File.Exists(Path.Combine(_savePath, "Default_sam.txt"))) {
            settingsFields.FillInDefaults(
                JsonUtility.FromJson<SampleSettings>(File.ReadAllText(Path.Combine(_savePath, "Default_sam.txt"))));
        }
    }

    public void SubmitToComputing()
    {
        FillInBlanks();
        
        /*
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
        
        */
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

        var logger = new Logger()
            .SetPrintLevel(Logger.LogLevel.Custom)
            .SetPrintFilter(new List<Logger.EventType> {Logger.EventType.Inspect});
        
        shaderAdapter = _builder
            .SetLogger(logger)
            .SetMode(settingsFields.settings.mode)
            .SetModel(settingsFields.MakeModel())
            .AutoSetShader()
            //.WriteFactors()
            .Build();
            
        shaderAdapter.Execute();
        //*/
    }

    public void SaveSettings() {
        string saveDataSet = JsonUtility.ToJson(settingsFields.settings);
        File.WriteAllText(Path.Combine(_savePath, settingsFields.settings.saveName + "_set.txt"), saveDataSet);
        
        string saveDataDet = JsonUtility.ToJson(settingsFields.detectorSettings);
        File.WriteAllText(Path.Combine(_savePath, settingsFields.settings.saveName + "_det.txt"), saveDataDet);
        
        string saveDataSam = JsonUtility.ToJson(settingsFields.sampleSettings);
        File.WriteAllText(Path.Combine(_savePath, settingsFields.settings.saveName + "_sam.txt"), saveDataSam);
    }

    public void LoadSettings() {
        if (File.Exists(Path.Combine(_savePath, loadFileName.text + "_set.txt"))) {
            string loadedDataSet = File.ReadAllText(Path.Combine(_savePath, loadFileName.text + "_set.txt"));
            settingsFields.settings = JsonUtility.FromJson<Settings>(loadedDataSet);
            settingsFields.ModeChanged();
            settingsFields.DataChanged();
        }
        
        if (File.Exists(Path.Combine(_savePath, loadFileName.text + "_det.txt"))) {
            string loadedDataDet = File.ReadAllText(Path.Combine(_savePath, loadFileName.text + "_det.txt"));
            settingsFields.detectorSettings = JsonUtility.FromJson<DetectorSettings>(loadedDataDet);
            settingsFields.DataChanged();
        }

        if (File.Exists(Path.Combine(_savePath, loadFileName.text + "_sam.txt"))) {
            string loadedDataSam = File.ReadAllText(Path.Combine(_savePath, loadFileName.text + "_sam.txt"));
            settingsFields.sampleSettings = JsonUtility.FromJson<SampleSettings>(loadedDataSam);
            settingsFields.DataChanged();
        }
    }
    
    //Daten bei Neustart handlen
    
}



