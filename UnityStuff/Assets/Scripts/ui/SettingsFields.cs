using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using model;
using UnityEngine;
using UnityEngine.UI;

namespace ui
{
   public class SettingsFields : MonoBehaviour {
      
      #region Fields

      private static int _namelessNumber;
      public Text currentPresetName;
      public InputField fieldPresetName;
      public Dropdown dropdownMode;

      public Dropdown absType;
      public InputField fieldGridResolution;
   
      public InputField fieldPixelSize;
      public InputField fieldResolutionX;
      public InputField fieldResolutionY;
      public InputField fieldDistToSample;
      public InputField fieldOffsetX;
      public InputField fieldOffsetY;
      public InputField fieldPathAngleFile;
   
      public InputField fieldDiameter;
      public InputField fieldCellThickness;
      public InputField fieldMuCell;
      public InputField fieldMuSample;

      public InputField fieldAngleStart;
      public InputField fieldAngleEnd;
      public InputField fieldAngleSteps;

   
      public Settings settings;
      public SampleSettings sampleSettings;
      public DetectorSettings detectorSettings;
      public RaySettings raySettings;

      // for component-wise / group-wise selection of input fields.
      public GameObject inputGroupDetector;
      public GameObject inputGroupAngle;
      public GameObject inputGroupIntegrated;
      
      private readonly Dictionary<int, Model.Mode> _dropdownMap = new Dictionary<int, Model.Mode>
      {
         {0, Model.Mode.Point},
         {1, Model.Mode.Area},
         {2, Model.Mode.Integrated},
         {3, Model.Mode.Testing},
      };

      private readonly CultureInfo _cultureInfo = CultureInfo.InvariantCulture;

      #endregion
      
      public void Awake()
      {
         SetAllInputGroups(false);
         SetListeners();
      }

      private void SetListeners()
      {
         // metadata
         fieldPresetName.onEndEdit.AddListener(text => ParseField(text, ref settings.saveName));
         
         // Detector parameters
         fieldPixelSize.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.pixelSize));
         fieldDistToSample.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.distToSample));
         
         fieldOffsetX.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.offSetFromDownRightEdge.x));
         fieldOffsetY.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.offSetFromDownRightEdge.y));

         void SetComponent(string text, ref Vector2Int variable, int position)
         {
            if (IsValue(text)) variable[position] = int.Parse(text, _cultureInfo);
         }

         fieldResolutionX.onEndEdit.AddListener(text => SetComponent(text, ref detectorSettings.resolution, 0));
         fieldResolutionY.onEndEdit.AddListener(text => SetComponent(text, ref detectorSettings.resolution, 1));

         // Sample parameters
         fieldGridResolution.onEndEdit.AddListener(text => ParseField(text, ref settings.gridResolution));
         fieldDiameter.onEndEdit.AddListener(text => ParseField(text, ref sampleSettings.totalDiameter));
         fieldCellThickness.onEndEdit.AddListener(text => ParseField(text, ref sampleSettings.cellThickness));
         fieldMuCell.onEndEdit.AddListener(text => ParseField(text, ref sampleSettings.muCell));
         fieldMuSample.onEndEdit.AddListener(text => ParseField(text, ref sampleSettings.muSample));

         // Angle parameters
         fieldPathAngleFile.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.pathToAngleFile));
         fieldAngleStart.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.angleStart));
         fieldAngleEnd.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.angleEnd));
         fieldAngleSteps.onEndEdit.AddListener(text => ParseField(text, ref detectorSettings.angleCount));
      }

      private void SetAllInputGroups(bool value)
      {
         inputGroupAngle.gameObject.SetActive(value);
         inputGroupDetector.gameObject.SetActive(value);
         inputGroupIntegrated.gameObject.SetActive(value);
      }

      public Model MakeModel()
      {
         return new Model(settings, detectorSettings, sampleSettings);
      }

      public void FillFromPreset(Settings preset)
      {
         if (!IsValue(fieldPresetName.text)) settings.saveName = "Default_" + _namelessNumber++;
         if (!IsValue(fieldGridResolution.text)) settings.gridResolution = preset.gridResolution;
         
         UpdateGeneralSettingsUI();
      }
   
      public void FillFromPreset(DetectorSettings preset)
      {
         if (!IsValue(fieldOffsetX.text)) 
            detectorSettings.offSetFromDownRightEdge.x = preset.offSetFromDownRightEdge.x;
         if (!IsValue(fieldOffsetY.text)) 
            detectorSettings.offSetFromDownRightEdge.y = preset.offSetFromDownRightEdge.y;
         if (!IsValue(fieldPixelSize.text)) detectorSettings.pixelSize = preset.pixelSize;
         if (!IsValue(fieldResolutionX.text)) detectorSettings.resolution.x = preset.resolution.x;
         if (!IsValue(fieldResolutionY.text)) detectorSettings.resolution = preset.resolution;
         if (!IsValue(fieldDistToSample.text)) detectorSettings.distToSample = preset.distToSample;
         if (!IsValue(fieldAngleStart.text)) detectorSettings.angleStart = preset.angleStart;
         if (!IsValue(fieldAngleEnd.text)) detectorSettings.angleEnd = preset.angleEnd;
         if (!IsValue(fieldAngleSteps.text)) detectorSettings.angleCount = preset.angleCount;

         UpdateDetectorSettingsUI();
      }
   
      public void FillFromPreset(SampleSettings preset)
      {
         if (!IsValue(fieldDiameter.text)) sampleSettings.totalDiameter = preset.totalDiameter;
         if (!IsValue(fieldCellThickness.text)) sampleSettings.cellThickness = preset.cellThickness;
         if (!IsValue(fieldMuCell.text)) sampleSettings.muCell = preset.muCell;
         if (!IsValue(fieldMuSample.text)) sampleSettings.muSample = preset.muSample;
         
         UpdateSampleSettingsUI();
      }

      /// <summary>Sets the internal dropdown value according to the supplied mode.</summary>
      private void SetDropdownTo(Model.Mode mode)
      {
         if (!_dropdownMap.ContainsValue(mode)) throw new InvalidOperationException();
         dropdownMode.value = _dropdownMap.FirstOrDefault(e => e.Value==mode).Key;
      }

      #region Data update methods

      public void UpdateModeData()
      {
         if (!_dropdownMap.ContainsKey(dropdownMode.value)) throw new InvalidOperationException();
         settings.mode = _dropdownMap[dropdownMode.value];
         ShowRelevantInputFields();
      }

      #endregion

      #region UI update methods

      /// <summary>
      /// Updates main menu upon change of computation mode.
      /// Mode-relevant fields are set to visible, all others to invisible.
      /// </summary>
      /// <exception cref="InvalidOperationException"></exception>
      private void ShowRelevantInputFields()
      {
         SetAllInputGroups(false);
      
         switch (settings.mode)
         {
            case Model.Mode.Point:
               inputGroupAngle.gameObject.SetActive(true);
               break;
         
            case Model.Mode.Area:
               inputGroupDetector.gameObject.SetActive(true);
               break;
         
            case Model.Mode.Integrated:
               SetAllInputGroups(true);
               break;
         
            case Model.Mode.Testing:
               SetAllInputGroups(true);
               break;
         
            default:
               throw new InvalidOperationException();
         }
      }

      public void UpdateModeUI() {
         SetDropdownTo(settings.mode);
         ShowRelevantInputFields();
      }

      public void UpdateGeneralSettingsUI()
      {
         fieldPresetName.text = settings.saveName;
         fieldGridResolution.text = settings.gridResolution.ToString(_cultureInfo);
      }

      public void UpdateIntegrationSettingsUI()
      {
         // TODO
      }

      public void UpdateDetectorSettingsUI()
      {
         fieldPixelSize.text = detectorSettings.pixelSize.ToString(_cultureInfo);
         fieldOffsetX.text = detectorSettings.offSetFromDownRightEdge.x.ToString(_cultureInfo);
         fieldOffsetY.text = detectorSettings.offSetFromDownRightEdge.y.ToString(_cultureInfo);
         fieldDistToSample.text = detectorSettings.distToSample.ToString(_cultureInfo);
         fieldResolutionX.text = detectorSettings.resolution.x.ToString(_cultureInfo);
         fieldResolutionY.text = detectorSettings.resolution.y.ToString(_cultureInfo);
         
         fieldPathAngleFile.text = detectorSettings.pathToAngleFile;
         fieldAngleStart.text = detectorSettings.angleStart.ToString(_cultureInfo);
         fieldAngleEnd.text = detectorSettings.angleEnd.ToString(_cultureInfo);
         fieldAngleSteps.text = detectorSettings.angleCount.ToString(_cultureInfo);
      }

      public void UpdateSampleSettingsUI()
      {
         fieldDiameter.text = sampleSettings.totalDiameter.ToString(_cultureInfo);
         fieldCellThickness.text = sampleSettings.cellThickness.ToString(_cultureInfo);
         fieldMuCell.text = sampleSettings.muCell.ToString(_cultureInfo);
         fieldMuSample.text = sampleSettings.muSample.ToString(_cultureInfo);
      }

      #endregion
      
      #region Parsing

      /// <summary>
      /// Not null, empty or whitespace.
      /// </summary>
      private bool IsValue(string text)
      {
         return !(string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text));
      }
      
      private void ParseField(string input, ref string target)
      {
         if (IsValue(input))
            target = input;
      }
      
      private void ParseField(string input, ref float target)
      {
         if (IsValue(input)) 
            target = float.Parse(input, _cultureInfo);
      }

      private void ParseField(string input, ref int target)
      {
         if (IsValue(input)) 
            target = int.Parse(input, _cultureInfo);
      }

      #endregion
   }
}