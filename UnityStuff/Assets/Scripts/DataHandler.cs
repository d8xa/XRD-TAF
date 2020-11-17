using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using adapter;
using model;
using model.properties;
using ui;
using UnityEngine.UI;
using util;
using Button = UnityEngine.UI.Button;
using Logger = util.Logger;

public class DataHandler : MonoBehaviour
{
    
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
    public Button runBenchmarkButton;
    public Button runABTestsButton;

    public ComputeShader pointModeShader;
    public ComputeShader planeModeShader;
    public ComputeShader integratedModeShader;

    private ShaderAdapter _shaderAdapter;

    private readonly ShaderAdapterBuilder _builder = ShaderAdapterBuilder.New();
    

    private string Scope(string method)
    {
        return GetType().Name + (string.IsNullOrWhiteSpace(method) ? "" : $".{method}()");
    }

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
        
        runABTestsButton.gameObject.SetActive(Settings.flags.isDebugBuild);
        runABTestsButton.onClick.AddListener(RunABTests);

        
        // TODO: hide "Submit" button until all required settings for the selected mode are set.
        // TODO: multithreading.
        submitButton.onClick.AddListener(SubmitToComputing);
        stopButton.gameObject.SetActive(false);
    }

    #region Testing
    
    
    private void CompareTestData(Logger logger, List<Preset> presets, List<AbsorptionProperties.Mode> modes)
    {
        // read datasets, compare B to A, generate report.
        foreach (var preset in presets)
        {
            var nrAngles = Parser.ImportAngles(Path.Combine(Directory.GetCurrentDirectory(), "Input",
                preset.properties.angle.pathToAngleFile + ".txt")).Length;
            
            foreach (var mode in modes)
            {
                var folderBottom = preset.metadata.saveName ?? "";
                var dirA = Path.Combine(Directory.GetCurrentDirectory(), "Output", "A", folderBottom);
                var dirB = Path.Combine(Directory.GetCurrentDirectory(), "Output", "B", folderBottom);

                List<Vector3> a;
                List<Vector3> b;

                preset.properties.absorption.mode = mode;
                preset.metadata.pathOutputData = "A";
                var fileNameA = preset.properties.FilenameFormatter(nrAngles);
                preset.metadata.pathOutputData = "B";
                var fileNameB = preset.properties.FilenameFormatter(nrAngles);

                if (mode == AbsorptionProperties.Mode.Area)
                {
                    var a2D = Parser.ReadArray(Path.Combine(dirA, fileNameA), false, false, reverse: true);
                    var b2D = Parser.ReadArray(Path.Combine(dirB, fileNameB), false, false, reverse: true);

                    a = Enumerable.Range(0, a2D.GetLength(0))
                        .Select(i => Enumerable.Range(0, a2D.GetLength(1))
                            .Select(j => a2D[i, j]))
                        .SelectMany(v => v)
                        .ToList();
                    b = Enumerable.Range(0, b2D.GetLength(0))
                        .Select(i => Enumerable.Range(0, b2D.GetLength(1))
                            .Select(j => b2D[i, j]))
                        .SelectMany(v => v)
                        .ToList();
                }
                else
                {
                    a = Parser.ReadTableVector(Path.Combine(dirA, fileNameA), true, true).ToList();
                    b = Parser.ReadTableVector(Path.Combine(dirB, fileNameB), true, true).ToList();
                }

                Vector3 Subtract(Vector3 v1, Vector3 v2)
                {
                    return new Vector3(
                        float.IsInfinity(v1.x) && float.IsInfinity(v2.x) ? 0f : v1.x - v2.x,
                        float.IsInfinity(v1.y) && float.IsInfinity(v2.y) ? 0f : v1.y - v2.y,
                        float.IsInfinity(v1.z) && float.IsInfinity(v2.z) ? 0f : v1.z - v2.z
                    );
                }

                var diff = Enumerable.Range(0, a.Count)
                    .Select(i => Subtract(a[i], b[i]))
                    .ToList();
                var min = new Vector3(
                    diff.AsParallel().Where(v => v != Vector3.positiveInfinity).Select(v => v.x).Min(),
                    diff.AsParallel().Where(v => v != Vector3.positiveInfinity).Select(v => v.y).Min(),
                    diff.AsParallel().Where(v => v != Vector3.positiveInfinity).Select(v => v.z).Min()
                );
                var max = new Vector3(
                    diff.AsParallel().Where(v => v != Vector3.positiveInfinity).Select(v => v.x).Max(),
                    diff.AsParallel().Where(v => v != Vector3.positiveInfinity).Select(v => v.y).Max(),
                    diff.AsParallel().Where(v => v != Vector3.positiveInfinity).Select(v => v.z).Max()
                );
                var mean = diff.AsParallel()
                    .Where(v => v != Vector3.positiveInfinity)
                    .Aggregate((sum,v) => sum + v) / 
                           diff.Count(v => v != Vector3.positiveInfinity);
                var var = diff.AsParallel()
                    .Where(v => v != Vector3.positiveInfinity)
                    .Select(v => v - mean)
                    .Select(v => Vector3.Scale(v, v))
                    .Aggregate((variance, v) => variance + v) / 
                          diff.Count(v => v != Vector3.positiveInfinity);
                    
                logger.Log(Logger.EventType.Test, 
                    $"{Scope(nameof(CompareTestData))}: " +
                    $"Test result (preset=\"{preset.metadata.saveName}\", mode={mode}): " +
                    $"min={min:G}, max={max:G}, mean={mean:G}, var={var:G}"
                    );
            }
        }
    }

    private void GenerateTestData(Logger logger, List<Preset> presets, List<AbsorptionProperties.Mode> modes)
    {
        const string method = nameof(GenerateTestData);
        
        // Backup and modify flag.
        var clippingBackup = Settings.flags.useClipping;
        Settings.flags.useClipping = false;

        // generate and save test dataset.
        var step = 0;
        foreach (var mode in modes)
        {
            foreach (var preset in presets)
            {
                preset.properties.absorption.mode = mode;
                preset.metadata.pathOutputData = "B";
                _shaderAdapter = _builder
                        .SetLogger(logger)
                        .SetWriteFactors(true)
                        .SetProperties(preset)
                        .AutoSetShader()
                        .Build()
                    ;
                _shaderAdapter.SetStatus(ref status);

                ++step;
                logger.Log(Logger.EventType.Test, 
                    $"{Scope(method)}: " + 
                    $"Shader adapter built. ({step}/{presets.Count*modes.Count})" + 
                    $" preset set to = {preset.metadata.saveName}");
                _shaderAdapter.Execute();
                logger.Log(Logger.EventType.Test,
                    $"{Scope(method)}: Shader adapter executed. ({step}/{presets.Count * modes.Count})");
                _shaderAdapter.SetStatusMessage(
                    $"{Scope(method)}: Step {step}/6: preset {preset.metadata.saveName}, {mode}");
            }
        }
        
        Settings.flags.useClipping = clippingBackup;
    }

    private void RunABTests()
    {
        const string method = nameof(RunABTests);
        Debug.Log($"{Scope(method)}: started.");
        
        var logger = new Logger()
            .SetPrintFilter(new List<Logger.EventType> {Logger.EventType.Warning, Logger.EventType.Test})
            .SetWriteFilter(new List<Logger.EventType> {Logger.EventType.Warning, Logger.EventType.Test});

        var logPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "A-B Test log.txt");
        logger.Log(Logger.EventType.Info, $"{Scope(method)}: Logger initialized.");

        // read presets
        var testPresetNames = new List<string>
        {
            "ref", 
            "ref ray"
        };
        var testPresets = testPresetNames
            .Select(s => Path.Combine(_saveDir, s + Settings.DefaultValues.PresetExtension))
            .Select(ReadPreset)
            .ToList();
        
        var modeList = new List<AbsorptionProperties.Mode>
            {
                AbsorptionProperties.Mode.Point, 
                AbsorptionProperties.Mode.Area, 
                AbsorptionProperties.Mode.Integrated
            };
        
        GenerateTestData(logger, testPresets, modeList);
        CompareTestData(logger, testPresets, modeList);
        
        SetStatusMessage("A/B-Test completed.");
        logger.AppendToFile(logPath);
    }
    
    #endregion
    

    private void UpdateSaveDir()
    {
        _saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Settings");
    }

    public void SubmitToComputing()
    {
        const string method = nameof(SubmitToComputing);
        
        //FillInBlanks();    // TODO

        var logger = new Logger()
            .SetPrintLevel(Logger.LogLevel.Custom)
            .SetPrintFilter(new List<Logger.EventType> {Logger.EventType.Inspect, Logger.EventType.Warning});
        

        logger.Log(Logger.EventType.Info, $"{Scope(method)}: Logger initialized.");
        
        _shaderAdapter = _builder
            .SetLogger(logger)
            .SetWriteFactors(Settings.flags.writeFactors)
            .SetProperties(mainPanel.preset)
            .AutoSetShader()
            .Build();
        
        logger.Log(Logger.EventType.Info, $"{Scope(method)}: Shader adapter built." +
                                             $" preset set to = {mainPanel.preset}");
        
        _shaderAdapter.SetStatus(ref status);
        
        _shaderAdapter.Execute();
        logger.Log(Logger.EventType.Info, $"{Scope(method)}: Shader adapter executed.");
    }

    public void SavePreset()
    {
        UpdateSaveDir();
        Directory.CreateDirectory(_saveDir);
        var path = Path.Combine(_saveDir, mainPanel.preset.metadata.saveName + Settings.DefaultValues.PresetExtension);

        mainPanel.preset?.Serialize(path);
    }

    public void LoadPreset()
    {
        LoadPreset(loadFileName.text);
    }

    private void LoadPreset(string filename)
    {
        UpdateSaveDir();
        var loadFilePath = Path.Combine(_saveDir, filename + Settings.DefaultValues.PresetExtension);

        try
        {
            mainPanel.preset = Preset.Deserialize(loadFilePath);
            mainPanel.selectedPreset = mainPanel.preset;
            mainPanel.UpdateAllUI();
            SetCurrentPresetName(filename);
        }
        catch (FileNotFoundException e)
        {
            // TODO
            Console.WriteLine(e);
            throw;
        }
    }

    private static Preset ReadPreset(string filePath)
    {
        return Preset.Deserialize(filePath);
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