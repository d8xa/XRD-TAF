using System;
using System.Collections.Generic;
using System.Linq;
using model;
using model.properties;
using UnityEngine;
using UnityEngine.UI;
using static util.FieldParseTools;
using Button = UnityEngine.UI.Button;

namespace ui
{
   public class MainPanel : MonoBehaviour {
      
      #region Fields

      public Button settingsButton;
      
      private static int _namelessNumber;
      public Text currentPresetName;
      public InputField fieldPresetName;
      public Dropdown dropdownMode;

      public Dropdown dropdownTarget;
      public InputField fieldGridResolution;
   
      public InputField fieldPixelSizeX;
      public InputField fieldPixelSizeY;
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

      public Preset selectedPreset;    // use if a saved preset was selected (incl. default).
      internal Preset preset;     // use for current state of properties.
      private bool presetHasChanged;
      // TODO: remember to deep-copy the preset!

      // for component-wise / group-wise selection of case-specific input fields.
      public GameObject inputGroupDetector;
      public GameObject inputGroupAngle;
      public GameObject inputGroupIntegrated;

      #endregion
      
      public void Awake()
      {
         preset = new Preset();
         SetAllInputGroups(false);
         SetListeners();
      }

      private void SetListeners()
      {
         // metadata 
         fieldPresetName.onEndEdit.AddListener(text =>
         {
            preset.metadata.saveName = ParseField(text, preset.metadata.saveName);
         });
         
         // Absorption parameters
         dropdownMode.onValueChanged.AddListener(value => UpdateModeData());
         
         // Detector parameters
         fieldDistToSample.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.detector.distToSample));

         fieldOffsetX.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.detector.offset.x));
         fieldOffsetY.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.detector.offset.y));
         
         fieldResolutionX.onEndEdit.AddListener(text => SetComponent(text, ref preset.properties.detector.resolution, 0));
         fieldResolutionY.onEndEdit.AddListener(text => SetComponent(text, ref preset.properties.detector.resolution, 1));

         fieldPixelSizeX.onEndEdit.AddListener(text => SetComponent(text, ref preset.properties.detector.pixelSize, 0));
         fieldPixelSizeY.onEndEdit.AddListener(text => SetComponent(text, ref preset.properties.detector.pixelSize, 1));

         // Sample parameters
         fieldGridResolution.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.sample.gridResolution));
         fieldDiameter.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.sample.totalDiameter));
         fieldCellThickness.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.sample.cellThickness));
         fieldMuCell.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.sample.muCell));
         fieldMuSample.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.sample.muSample));

         // Angle parameters
         fieldPathAngleFile.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.angle.pathToAngleFile));
         fieldAngleStart.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.angle.angleStart));
         fieldAngleEnd.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.angle.angleEnd));
         fieldAngleSteps.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.angle.angleCount));
      }

      private void SetAllInputGroups(bool value)
      {
         inputGroupAngle.gameObject.SetActive(value);
         inputGroupDetector.gameObject.SetActive(value);
         inputGroupIntegrated.gameObject.SetActive(value);
      }
      
            
      void SetComponent(string text, ref Vector2Int variable, int position)
      {
         if (IsValue(text)) variable[position] = int.Parse(text, Settings.defaults.cultureInfo);
      }
         
      void SetComponent(string text, ref Vector2 variable, int position)
      {
         if (IsValue(text)) variable[position] = int.Parse(text, Settings.defaults.cultureInfo);
      }
      

      #region Load methods

      public void FillFromPreset(Preset source)
      {
         FillFromPreset(source.metadata);   // not necessary for 
         FillFromPreset(source.properties.absorption);
         FillFromPreset(source.properties.angle);
         FillFromPreset(source.properties.detector);
         FillFromPreset(source.properties.ray);
         FillFromPreset(source.properties.sample);
      }

      private void FillFromPreset(Metadata source)
      {
         if (!IsValue(fieldPresetName.text)) preset.metadata.saveName = source.saveName + _namelessNumber++;
         
         RefreshMetadataUI();
      }

      private void FillFromPreset(AbsorptionProperties source)
      {
         if (!Enum.IsDefined(typeof(AbsorptionProperties.Mode), preset.properties.absorption.mode))
            preset.properties.absorption.mode = source.mode;
         if (!Enum.IsDefined(typeof(AbsorptionProperties.Mode), preset.properties.absorption.absorptionTarget))
            preset.properties.absorption.absorptionTarget = source.absorptionTarget;

         RefreshAbsorptionPropertiesUI();
      }

      private void FillFromPreset(AngleProperties source)
      {
         if (!IsValue(fieldAngleStart.text)) preset.properties.angle.angleStart = source.angleStart;
         if (!IsValue(fieldAngleEnd.text)) preset.properties.angle.angleEnd = source.angleEnd;
         if (!IsValue(fieldAngleSteps.text)) preset.properties.angle.angleCount = source.angleCount;
         
         RefreshAnglePropertiesUI();
      }

      private void FillFromPreset(RayProperties source)
      {
         preset.properties.ray.profile = source.profile;
         preset.properties.ray.dimensions = source.dimensions; // TODO: add input field "ray dimensions".
         preset.properties.ray.intensity = source.intensity;   // TODO: add input field "ray intensity".
         preset.properties.ray.offset = source.offset;         // TODO: add input field "ray offset".
         
         //RefreshRayPropertiesUI();
      }

      private void FillFromPreset(DetectorProperties source)
      {
         if (!IsValue(fieldOffsetX.text)) preset.properties.detector.offset.x = source.offset.x;
         if (!IsValue(fieldOffsetY.text)) preset.properties.detector.offset.y = source.offset.y;
         if (!IsValue(fieldPixelSizeX.text)) preset.properties.detector.pixelSize.x = source.pixelSize.x;
         if (!IsValue(fieldPixelSizeY.text)) preset.properties.detector.pixelSize.y = source.pixelSize.y;
         if (!IsValue(fieldResolutionX.text)) preset.properties.detector.resolution.x = source.resolution.x;
         if (!IsValue(fieldResolutionY.text)) preset.properties.detector.resolution = source.resolution;
         if (!IsValue(fieldDistToSample.text)) preset.properties.detector.distToSample = source.distToSample;

         RefreshDetectorPropertiesUI();
      }

      private void FillFromPreset(SampleProperties source)
      {
         if (!IsValue(fieldGridResolution.text)) preset.properties.sample.gridResolution = source.gridResolution;
         if (!IsValue(fieldDiameter.text)) preset.properties.sample.totalDiameter = source.totalDiameter;
         if (!IsValue(fieldCellThickness.text)) preset.properties.sample.cellThickness = source.cellThickness;
         if (!IsValue(fieldMuCell.text)) preset.properties.sample.muCell = source.muCell;
         if (!IsValue(fieldMuSample.text)) preset.properties.sample.muSample = source.muSample;
         
         RefreshSamplePropertiesUI();
      }

      #endregion
      
      #region Data update methods

      public void UpdateModeData()
      {
         preset.properties.absorption.mode = (AbsorptionProperties.Mode) dropdownMode.value;
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
      
         switch (preset.properties.absorption.mode)
         {
            case AbsorptionProperties.Mode.Point:
               inputGroupAngle.gameObject.SetActive(true);
               break;
         
            case AbsorptionProperties.Mode.Area:
               inputGroupDetector.gameObject.SetActive(true);
               break;
         
            case AbsorptionProperties.Mode.Integrated:
               SetAllInputGroups(true);
               break;
         
            case AbsorptionProperties.Mode.Testing:
               SetAllInputGroups(true);
               break;
         
            default:
               throw new InvalidOperationException();
         }
      }
      
      /// <summary>Sets the dropdown value in the UI according to the supplied mode.</summary>
      private void SetDropdownTo(AbsorptionProperties.Mode mode)
      {
         dropdownMode.value = (int) mode;

         /*if (!_dropdownModeMap.ContainsValue(mode)) throw new InvalidOperationException();
         dropdownMode.value = _dropdownModeMap.FirstOrDefault(e => e.Value==mode).Key;*/
      }
      
      private void SetDropdownTo(AbsorptionProperties.AbsorptionTarget target)
      {
         dropdownTarget.value = (int) target;
      }

      public void UpdateAllUI()
      {
         RefreshModeUI();
         RefreshMetadataUI();
         RefreshAbsorptionPropertiesUI();
         RefreshSamplePropertiesUI();
         RefreshRayPropertiesUI();
         RefreshDetectorPropertiesUI();
         RefreshAnglePropertiesUI();
      }

      public void RefreshModeUI() {
         SetDropdownTo(preset.properties.absorption.mode);
         ShowRelevantInputFields();
      }

      public void RefreshMetadataUI()
      {
         fieldPresetName.text = preset.metadata.saveName;
      }

      public void RefreshAbsorptionPropertiesUI()
      {
         SetDropdownTo(preset.properties.absorption.mode);
         SetDropdownTo(preset.properties.absorption.absorptionTarget);
      }
      
      public void RefreshSamplePropertiesUI()
      {
         fieldGridResolution.text = preset.properties.sample.gridResolution.ToString(Settings.defaults.cultureInfo);
         fieldDiameter.text = preset.properties.sample.totalDiameter.ToString(Settings.defaults.cultureInfo);
         fieldCellThickness.text = preset.properties.sample.cellThickness.ToString(Settings.defaults.cultureInfo);
         fieldMuCell.text = preset.properties.sample.muCell.ToString(Settings.defaults.cultureInfo);
         fieldMuSample.text = preset.properties.sample.muSample.ToString(Settings.defaults.cultureInfo);
      }
      
      public void RefreshRayPropertiesUI()
      {
         // TODO: implement as soon as ray properties are available.
      }

      public void RefreshDetectorPropertiesUI()
      {
         fieldPixelSizeX.text = preset.properties.detector.pixelSize.x.ToString(Settings.defaults.cultureInfo);
         fieldPixelSizeY.text = preset.properties.detector.pixelSize.y.ToString(Settings.defaults.cultureInfo);
         fieldOffsetX.text = preset.properties.detector.offset.x.ToString(Settings.defaults.cultureInfo);
         fieldOffsetY.text = preset.properties.detector.offset.y.ToString(Settings.defaults.cultureInfo);
         fieldDistToSample.text = preset.properties.detector.distToSample.ToString(Settings.defaults.cultureInfo);
         fieldResolutionX.text = preset.properties.detector.resolution.x.ToString(Settings.defaults.cultureInfo);
         fieldResolutionY.text = preset.properties.detector.resolution.y.ToString(Settings.defaults.cultureInfo);
      }
      
      public void RefreshAnglePropertiesUI()
      {
         fieldPathAngleFile.text = preset.properties.angle.pathToAngleFile;
         fieldAngleStart.text = preset.properties.angle.angleStart.ToString(Settings.defaults.cultureInfo);
         fieldAngleEnd.text = preset.properties.angle.angleEnd.ToString(Settings.defaults.cultureInfo);
         fieldAngleSteps.text = preset.properties.angle.angleCount.ToString(Settings.defaults.cultureInfo);
      }

      #endregion
   }
}