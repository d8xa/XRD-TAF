using System.Collections.Generic;
using UnityEngine;
using System.IO;
using controller;
using FoPra.model;
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
            settingsFields.fillInDefaults(
                JsonUtility.FromJson<Settings>(File.ReadAllText(Path.Combine(savePath, "Default_set.txt"))));
        }
        if (File.Exists(Path.Combine(savePath, "Default_det.txt"))) {
            settingsFields.fillInDefaults(
                JsonUtility.FromJson<DetektorSettings>(File.ReadAllText(Path.Combine(savePath, "Default_det.txt"))));
        }
        if (File.Exists(Path.Combine(savePath, "Default_sam.txt"))) {
            settingsFields.fillInDefaults(
                JsonUtility.FromJson<SampleSettings>(File.ReadAllText(Path.Combine(savePath, "Default_sam.txt"))));
        }
    }

    public void submitToComputing()
    {
        fillInBlanks();

        shaderAdapter = ShaderAdapterBuilder.New()
            .SetMode(settingsFields.settings.mode)
            .SetModel(settingsFields.MakeModel())
            .AddShader(Model.Mode.Point, pointModeShader)
            .AddShader(Model.Mode.Area, planeModeShader)
            //.AddShader(Model.Mode.Integrated, integratedModeShader)
            .SetSegmentMargin(0.2f)
            .AutoSetShader()
            .WriteFactors()
            .Build();
            
        shaderAdapter.Execute();
        
        

        
        /*
        shaderAdapter = new PointModeAdapter(
            computeShader,
            new Model(
                settingsFields.settings, 
                settingsFields.detektorSettings, 
                settingsFields.sampleSettings
            ), 
            0.02f,
            false,
            true);
        
        
        //TestSuite.test_Distances2D(computeShader);
        // TODO: delegate to ShaderAdapter
        shaderAdapter.Execute();
        */
        /*
        var sw = new Stopwatch();
        logicHandler = new LogicHandler(
            new Model(
                settingsFields.settings, 
                settingsFields.detektorSettings, 
                settingsFields.sampleSettings
            ), computeShader);

        sw.Start();
        logicHandler.run_shader();
        sw.Stop();
        Debug.Log($"run_shader took {sw.Elapsed}");
        //*/
    }

    public void settingSaver() {
        string saveDataSet = JsonUtility.ToJson(settingsFields.settings);
        File.WriteAllText(Path.Combine(savePath, settingsFields.settings.aufbauBezeichnung + "_set.txt"), saveDataSet);
        
        string saveDataDet = JsonUtility.ToJson(settingsFields.detektorSettings);
        File.WriteAllText(Path.Combine(savePath, settingsFields.settings.aufbauBezeichnung + "_det.txt"), saveDataDet);
        
        string saveDataSam = JsonUtility.ToJson(settingsFields.sampleSettings);
        File.WriteAllText(Path.Combine(savePath, settingsFields.settings.aufbauBezeichnung + "_sam.txt"), saveDataSam);
    }

    public void settingLoader() {
        if (File.Exists(Path.Combine(savePath, loadFileName.text + "_set.txt"))) {
            string loadedDataSet = File.ReadAllText(Path.Combine(savePath, loadFileName.text + "_set.txt"));
            settingsFields.settings = JsonUtility.FromJson<Settings>(loadedDataSet);
            settingsFields.aktualisiereModus(true);
            settingsFields.aktualisiere(false);
        }
        
        if (File.Exists(Path.Combine(savePath, loadFileName.text + "_det.txt"))) {
            string loadedDataDet = File.ReadAllText(Path.Combine(savePath, loadFileName.text + "_det.txt"));
            settingsFields.detektorSettings = JsonUtility.FromJson<DetektorSettings>(loadedDataDet);
            settingsFields.aktualisiere(false);
        }

        if (File.Exists(Path.Combine(savePath, loadFileName.text + "_sam.txt"))) {
            string loadedDataSam = File.ReadAllText(Path.Combine(savePath, loadFileName.text + "_sam.txt"));
            settingsFields.sampleSettings = JsonUtility.FromJson<SampleSettings>(loadedDataSam);
            settingsFields.aktualisiere(false);
        }
    }
    
    //Daten bei Neustart handlen
    
}



