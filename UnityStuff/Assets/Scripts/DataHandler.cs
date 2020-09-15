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
    
    public SettingsFields settingsFields;
    private static string _savePath;
    public InputField loadFileName;
    public Text status;
    
    public Button loadButton;
    public Button saveButton;
    public Button submitButton;
    public Button stopButton;
    
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
        Setup();
        
    }
    
    private void Setup()
    {
        // only enable buttons when filename is not empty.
        // TODO: add further logic to only allow saving if input fields are non-empty and some value has changed.
        saveButton.interactable = false;
        loadButton.interactable = false;
        loadFileName.onValueChanged.AddListener(str =>
        {
            var value = !string.IsNullOrEmpty(str);
            saveButton.interactable = value;
            loadButton.interactable = value;
        });


        stopButton.gameObject.SetActive(false);
        // TODO: listener and multithreading.

        // TODO: hide "Submit" button until all required settings for the selected mode are set.
        // TODO: Add "default values" button to decouple default values from "Submit" button. 
        
        submitButton.onClick.AddListener(() =>
        {
            stopButton.gameObject.SetActive(true);
            status.gameObject.SetActive(true);
        });
    }

    private void UpdatePath()
    {
        _savePath = Path.Combine(Path.GetFullPath(Application.dataPath), "Settings");
    }

    private void FillInBlanks()
    {
        UpdatePath();
        if (File.Exists(Path.Combine(_savePath, "Default_set.txt"))) {
            settingsFields.FillFromPreset(
                JsonUtility.FromJson<Settings>(File.ReadAllText(Path.Combine(_savePath, "Default_set.txt"))));
        }
        if (File.Exists(Path.Combine(_savePath, "Default_det.txt"))) {
            settingsFields.FillFromPreset(
                JsonUtility.FromJson<DetectorSettings>(File.ReadAllText(Path.Combine(_savePath, "Default_det.txt"))));
        }
        if (File.Exists(Path.Combine(_savePath, "Default_sam.txt"))) {
            settingsFields.FillFromPreset(
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
        string settingsJson = JsonUtility.ToJson(settingsFields.settings);
        File.WriteAllText(Path.Combine(_savePath, settingsFields.settings.saveName + "_set.txt"), settingsJson);
        
        string detectorJson = JsonUtility.ToJson(settingsFields.detectorSettings);
        File.WriteAllText(Path.Combine(_savePath, settingsFields.settings.saveName + "_det.txt"), detectorJson);
        
        string sampleJson = JsonUtility.ToJson(settingsFields.sampleSettings);
        File.WriteAllText(Path.Combine(_savePath, settingsFields.settings.saveName + "_sam.txt"), sampleJson);
    }

    public void LoadSettings()
    {
        var loadFileNamePrefix = loadFileName.text;
        
        if (File.Exists(Path.Combine(_savePath, loadFileNamePrefix + "_set.txt"))) {
            string settingsJson = File.ReadAllText(Path.Combine(_savePath, loadFileNamePrefix + "_set.txt"));
            settingsFields.settings = JsonUtility.FromJson<Settings>(settingsJson);
            settingsFields.UpdateModeUI();
            settingsFields.UpdateGeneralSettingsUI();
            SetCurrentPresetName(loadFileName.text);
        }

        if (File.Exists(Path.Combine(_savePath, loadFileNamePrefix + "_det.txt"))) {
            string detectorJson = File.ReadAllText(Path.Combine(_savePath, loadFileNamePrefix + "_det.txt"));
            settingsFields.detectorSettings = JsonUtility.FromJson<DetectorSettings>(detectorJson);
            settingsFields.UpdateDetectorSettingsUI();
            SetCurrentPresetName(loadFileName.text);
        }

        if (File.Exists(Path.Combine(_savePath, loadFileNamePrefix + "_sam.txt"))) {
            string sampleJson = File.ReadAllText(Path.Combine(_savePath, loadFileNamePrefix + "_sam.txt"));
            settingsFields.sampleSettings = JsonUtility.FromJson<SampleSettings>(sampleJson);
            settingsFields.UpdateSampleSettingsUI();
            SetCurrentPresetName(loadFileName.text);
        }
    }

    private void SetCurrentPresetName(string presetName)
    {
        //settingsFields.currentPresetName.gameObject.SetActive(true);
        settingsFields.currentPresetName.text = presetName;
        settingsFields.currentPresetName.fontStyle = FontStyle.Normal;
        settingsFields.currentPresetName.color = new Color(50f/255f, 50f/255f, 50f/255f, 1);
        settingsFields.fieldPresetName.text = presetName;
    }
}



