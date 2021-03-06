﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Reflection;
using adapter;
using model;
using model.properties;
using tests;
using ui;
using UnityEngine.UI;
using util;
using Button = UnityEngine.UI.Button;
using Debug = UnityEngine.Debug;
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
            .SetSegmentMargin(Settings.defaults.samplePaddingDefault);
            // TODO: calculate best/minimum margin for segment resolution later. 
        
        UpdatePresetDir();
        ButtonSetup();
    }
    
    private void ButtonSetup()
    {
        // set panel and add button listener for navigation between them.
        GoToPanel(Panel.Main);
        mainPanel.settingsButton.onClick.AddListener(() => GoToPanel(Panel.Settings));
        settingsPanel.closeButton.onClick.AddListener(() => GoToPanel(Panel.Main));

        // not yet supported.
        mainPanel.dropdownRayProfile.interactable = false;
        mainPanel.fieldRayDimensionsY.interactable = false;
        mainPanel.fieldRayOffsetY.interactable = false;

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
        
        runABTestsButton.gameObject.SetActive(Settings.Flags.IsDebugBuild);
        runABTestsButton.onClick.AddListener(RunABTests);
        
        //runBenchmarkButton.gameObject.SetActive(Settings.Flags.IsDebugBuild);
        runBenchmarkButton.onClick.AddListener(() =>
        {
            loadFileName.text = "benchmark";
            LoadPreset();
            var inputInfos = LoadBenchmarkConfig();
            new Benchmark(this, mainPanel.preset, inputInfos)
                .Run(write: true)
                .Save();
        });

        
        // TODO: hide "Submit" button until all required settings for the selected mode are set.
        // TODO: multithreading.
        submitButton.onClick.AddListener(SubmitToComputing);
        stopButton.gameObject.SetActive(false);
    }

    private static List<PerformanceReport.InputInfo> LoadBenchmarkConfig()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), Settings.DefaultValues.BenchmarkFolderName,
            Settings.DefaultValues.BenchmarkConfigFileName);
        
        var head = Parser.ReadTableHead(path, sep: "\t").ToList();
        
        var inputInfoFields = typeof(PerformanceReport.InputInfo)
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        var propertyNames = inputInfoFields.Select(f => f.Name).ToList();
        
        // check if csv contains columns for all members of InputInfo.
        if (propertyNames.Except(head).Any()) 
            throw new InvalidOperationException(
                $"Must supply values for all members of {nameof(PerformanceReport.InputInfo)}.");

        var indexMapping = Enumerable.Range(0, head.Count)
            .Select(i => head.IndexOf(propertyNames[i]))
            .ToList();

        var table = Parser.ReadTable(path, false, true, sep: "\t");
        var config = new List<PerformanceReport.InputInfo>();
        
        for (int i = 0; i < table.GetLength(0); i++)
        {
            object current = new PerformanceReport.InputInfo();
            for (int j = 0; j < indexMapping.Count; j++)
            {
                inputInfoFields[j].SetValue(current, (int) table[i, indexMapping[j]]);
            }
            config.Add((PerformanceReport.InputInfo) current);
        }

        return config;
    }

    #region Testing
    
    private void CompareTestData(Logger logger, List<Preset> presets, List<AbsorptionProperties.Mode> modes)
    {
        // read datasets, compare B to A, generate report.
        foreach (var preset in presets)
        {
            var nrAngles = Parser.ImportAngles(Path.Combine(Directory.GetCurrentDirectory(), 
                Settings.DefaultValues.InputFolderName, preset.properties.angle.pathToAngleFile + ".txt")).Length;
            
            foreach (var mode in modes)
            {
                var folderBottom = preset.metadata.saveName ?? "";
                var outputDir = Path.Combine(Directory.GetCurrentDirectory(), 
                    Settings.DefaultValues.OutputFolderName);
                var dirA = Path.Combine(outputDir, "A", folderBottom);
                var dirB = Path.Combine(outputDir, "B", folderBottom);

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
        var clippingBackup = Settings.flags.clipAngles;
        Settings.flags.clipAngles = false;

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
        
        Settings.flags.clipAngles = clippingBackup;
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
            .Select(s => Path.Combine(_saveDir, s + Settings.DefaultValues.SerializedExtension))
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

    public void Compute(Preset preset, Logger logger, PerformanceReport report, bool write = false,
        double[] angles = null)
    {
        const string method = nameof(Compute);
        logger.Log(Logger.EventType.Info, $"{Scope(method)}: Shader adapter built." +
                                          $" preset set to = {mainPanel.preset}");
        _shaderAdapter = _builder
            .SetLogger(logger)
            .SetWriteFactors(write)
            .SetPerformanceReport(report)
            .SetProperties(preset)
            .SetAngles(angles)
            .AutoSetShader()
            .Build();
        _shaderAdapter.SetStatus(ref status);
        _shaderAdapter.Execute();
    }

    public PerformanceReport GetPerformanceReport()
    {
        if (_shaderAdapter == null)
            throw new InvalidOperationException("Can't get report. No shader adapter set.");
        return _shaderAdapter.GetReport();
    }
    

    private static void UpdatePresetDir()
    {
        _saveDir = Path.Combine(Directory.GetCurrentDirectory(), Settings.DefaultValues.PresetFolderName);
    }

    public void SubmitToComputing()
    {
        const string method = nameof(SubmitToComputing);
        
        //FillInBlanks();    // TODO
        var startTime = DateTime.Now;
        var sw = new Stopwatch();
        sw.Start();
        
        var logger = new Logger()
            .SetPrintLevel(Logger.LogLevel.Custom)
            .SetPrintFilter(new List<Logger.EventType> {Logger.EventType.Inspect, Logger.EventType.Warning});

        logger.Log(Logger.EventType.Info, $"{Scope(method)}: Logger initialized.");
        Compute(mainPanel.preset, logger, null, Settings.Flags.WriteFactors);
        if (Settings.flags.writeLogs)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), Settings.DefaultValues.LogFolderName,
                $"{DateTime.Now:yyyy-MM-ddTHH.mm.ss} Log.txt");
            logger.WriteToFile(path);
        }
        logger.Log(Logger.EventType.Info, $"{Scope(method)}: Shader adapter executed.");
        
        sw.Stop();
        SetStatusMessage($"Last job executed at {startTime:T}.\nTime elapsed: {sw.Elapsed:g}.");
    }

    public void SavePreset()
    {
        UpdatePresetDir();
        Directory.CreateDirectory(_saveDir);
        var path = Path.Combine(_saveDir, mainPanel.preset.metadata.saveName + Settings.DefaultValues.SerializedExtension);

        mainPanel.preset?.Serialize(path);
    }

    public void LoadPreset()
    {
        LoadPreset(loadFileName.text);
    }

    private void LoadPreset(string filename)
    {
        UpdatePresetDir();
        var loadFilePath = Path.Combine(_saveDir, filename + Settings.DefaultValues.SerializedExtension);

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