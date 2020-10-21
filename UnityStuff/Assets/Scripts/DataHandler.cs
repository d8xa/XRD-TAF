using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Net.Mime;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml;
using adapter;
using model;
using model.properties;
using ui;
using UnityEditor.Build.Content;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Logger = util.Logger;

public class DataHandler : MonoBehaviour{
    
    #region Panels
    
    public MainPanel mainPanel;
    public SettingsPanel settingsPanel;

    private enum Panel { Main, Settings }
    private Panel currentPanel;
    
    private void GoToPanel(Panel panel)
    {
        switch (panel)
        {
            case Panel.Main:
                settingsPanel.gameObject.SetActive(false);
                mainPanel.gameObject.SetActive(true);
                break;
            case Panel.Settings:
                mainPanel.gameObject.SetActive(false);
                settingsPanel.gameObject.SetActive(true);
                break;
        }
        currentPanel = panel;
    }
    
    #endregion
    
    private static string _saveDir;
    public InputField loadFileName;
    public Text status;
    
    public Button loadButton;
    public Button loadDefaults;
    public Button saveButton;
    public Button submitButton;
    public Button stopButton;
    
    public ComputeShader pointModeShader;
    public ComputeShader planeModeShader;
    public ComputeShader integratedModeShader;

    private ShaderAdapter _shaderAdapter;

    private readonly ShaderAdapterBuilder _builder = ShaderAdapterBuilder.New();
    
    public static readonly DataContractJsonSerializerSettings SerializerSettings = 
        new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true,
            IgnoreExtensionDataObject = true
        };
    private static readonly DataContractJsonSerializer PresetSerializer = 
        new DataContractJsonSerializer(typeof(Preset), SerializerSettings);
    private static readonly Encoding Encoding = Encoding.UTF8;
    private static readonly string presetExtension = ".json";

    private void Awake() {
        _builder
            .AddShader(AbsorptionProperties.Mode.Point, pointModeShader)
            .AddShader(AbsorptionProperties.Mode.Area, planeModeShader)
            .AddShader(AbsorptionProperties.Mode.Integrated, integratedModeShader)
            .SetSegmentMargin(Settings.defaults.sampleAreaMarginDefault);
            // TODO: calculate best/minimum margin for segment resolution later. 
        
        UpdateSaveDir();
        ButtonSetup();
    }
    
    private void ButtonSetup()
    {
        // set panel and add button listener for navigation between them.
        GoToPanel(Panel.Main);
        mainPanel.settingsButton.onClick.AddListener(() => GoToPanel(Panel.Settings));
        settingsPanel.closeButton.onClick.AddListener(() => GoToPanel(Panel.Main));
        

        // Loading/saving buttons: 
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
        loadDefaults.onClick.AddListener(() =>
        {
            loadFileName.text = "default";
            LoadPreset();
        });
        loadButton.onClick.AddListener(LoadPreset);
        saveButton.onClick.AddListener(SavePreset);

        
        // TODO: hide "Submit" button until all required settings for the selected mode are set.
        // TODO: multithreading.
        submitButton.onClick.AddListener(() =>
        {
            //stopButton.gameObject.SetActive(true);
            //status.gameObject.SetActive(true);
        });
        stopButton.gameObject.SetActive(false);
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
            mainPanel.FillFromPreset(preset);
        }
    }

    public void SubmitToComputing()
    {
        //FillInBlanks();

        var logger = new Logger()
            .SetPrintLevel(Logger.LogLevel.Custom)
            .SetPrintFilter(new List<Logger.EventType> {Logger.EventType.Inspect, Logger.EventType.Warning});
        
        _shaderAdapter = _builder
            .SetLogger(logger)
            .SetWriteFactors(Settings.flags.writeFactors)
            .SetProperties(mainPanel.preset)
            .AutoSetShader()
            .Build();
        
        _shaderAdapter.SetStatus(ref status);

        Debug.Log($"{mainPanel.preset.properties.absorption.mode}");
        
        //_shaderAdapter.Execute();
    }

    public void SavePreset()
    {
        var path = Path.Combine(_saveDir, mainPanel.preset.metadata.saveName + presetExtension);

        using (var stream = File.Open(path, FileMode.OpenOrCreate)) 
        using (var writer = JsonReaderWriterFactory
            .CreateJsonWriter(stream, Encoding, true, true, "\t"))
        {
            PresetSerializer.WriteObject(writer, mainPanel.preset);
            writer.Flush();
        }
    }

    public void LoadPreset()
    {
        var loadFileNamePrefix = loadFileName.text;
        var loadFilePath = Path.Combine(_saveDir, loadFileNamePrefix + presetExtension);

        if (File.Exists(loadFilePath))
        {
            var presetJson = File.ReadAllText(loadFilePath, Encoding);
            using (var stream = new MemoryStream(Encoding.GetBytes(presetJson)))
            {
                mainPanel.preset = (Preset) PresetSerializer.ReadObject(stream);
                mainPanel.selectedPreset = mainPanel.preset;
            }
            
            mainPanel.UpdateAllUI();
            SetCurrentPresetName(loadFileNamePrefix);
        }
        // TODO: else
    }

    private void SetCurrentPresetName(string presetName)
    {
        mainPanel.currentPresetName.text = presetName;
        mainPanel.currentPresetName.fontStyle = FontStyle.Normal;
        mainPanel.currentPresetName.color = new Color(50f/255f, 50f/255f, 50f/255f, 1);
        mainPanel.fieldPresetName.text = presetName;
    }
    
    public void SetStatusMessage(string message)
    {
        status.text = message;
    }
}