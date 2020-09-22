using System.Collections.Generic;
using UnityEngine;
using System.IO;
using adapter;
using model;
using model.properties;
using ui;
using UnityEngine.UI;
using Logger = util.Logger;

public class DataHandler : MonoBehaviour{
    
    public SettingsFields settingsFields;
    private static string _saveDir;
    public InputField loadFileName;
    public Text status;
    
    public Button loadButton;
    public Button saveButton;
    public Button submitButton;
    public Button stopButton;
    
    public ComputeShader pointModeShader;
    public ComputeShader planeModeShader;
    public ComputeShader integratedModeShader;

    private ShaderAdapter _shaderAdapter;

    private readonly ShaderAdapterBuilder _builder = ShaderAdapterBuilder.New();

    private void Awake() {
        _builder
            .AddShader(AbsorptionProperties.Mode.Point, pointModeShader)
            .AddShader(AbsorptionProperties.Mode.Area, planeModeShader)
            .AddShader(AbsorptionProperties.Mode.Integrated, integratedModeShader)
            .SetSegmentMargin(Settings.defaults.sampleAreaMarginDefault);
            // TODO: calculate best/minimum margin for segment resolution later. 
        
        UpdateSaveDir();
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
            //stopButton.gameObject.SetActive(true);
            //status.gameObject.SetActive(true);
        });
    }

    private void UpdateSaveDir()
    {
        _saveDir = Path.Combine(Path.GetFullPath(Application.dataPath), "Settings");
    }

    private void FillInBlanks()
    {
        UpdateSaveDir();
        var filePath = Path.Combine(_saveDir, "Default.json");
        if (File.Exists(filePath))
        {
            var presetJson = File.ReadAllText(filePath);
            var preset = JsonUtility.FromJson<Preset>(presetJson);
            settingsFields.FillFromPreset(preset);
        }
    }

    public void SubmitToComputing()
    {
        FillInBlanks();

        var logger = new Logger()
            .SetPrintLevel(Logger.LogLevel.Custom)
            .SetPrintFilter(new List<Logger.EventType> {Logger.EventType.Inspect, Logger.EventType.Warning});
        
        _shaderAdapter = _builder
            .SetLogger(logger)
            .SetWriteFactors(Settings.flags.writeFactors)
            .SetProperties(settingsFields.preset)
            .AutoSetShader()
            .Build();
        
        _shaderAdapter.SetStatus(ref status);

        _shaderAdapter.Execute();
    }

    public void SavePreset()
    {
        var presetJson = JsonUtility.ToJson(settingsFields.preset);
        File.WriteAllText(Path.Combine(_saveDir, settingsFields.preset.metadata.saveName + ".json"), presetJson);
    }

    public void LoadPreset()
    {
        var loadFileNamePrefix = loadFileName.text;
        var loadFilePath = Path.Combine(_saveDir, loadFileNamePrefix + ".json");

        if (File.Exists(loadFilePath))
        {
            var presetJson = File.ReadAllText(loadFilePath);
            settingsFields.preset = JsonUtility.FromJson<Preset>(presetJson);
            settingsFields.selectedPreset = settingsFields.preset;
            settingsFields.UpdateAllUI();
            SetCurrentPresetName(loadFileNamePrefix);
        }
    }

    private void SetCurrentPresetName(string presetName)
    {
        settingsFields.currentPresetName.text = presetName;
        settingsFields.currentPresetName.fontStyle = FontStyle.Normal;
        settingsFields.currentPresetName.color = new Color(50f/255f, 50f/255f, 50f/255f, 1);
        settingsFields.fieldPresetName.text = presetName;
    }
    
    public void SetStatusMessage(string message)
    {
        status.text = message;
    }
}