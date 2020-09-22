using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using model;
using model.properties;
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

      public Preset selectedPreset;    // use if a saved preset was selected (incl. default).
      internal Preset preset;     // use for current state of properties.
      private bool presetHasChanged;

      // for component-wise / group-wise selection of case-specific input fields.
      public GameObject inputGroupDetector;
      public GameObject inputGroupAngle;
      public GameObject inputGroupIntegrated;

      private readonly Dictionary<int, AbsorptionProperties.Mode> _dropdownMap =
         new Dictionary<int, AbsorptionProperties.Mode>
         {
            {0, AbsorptionProperties.Mode.Point},
            {1, AbsorptionProperties.Mode.Area},
            {2, AbsorptionProperties.Mode.Integrated},
            {3, AbsorptionProperties.Mode.Testing}
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
         fieldPresetName.onEndEdit.AddListener(text => ParseField(text, ref preset.metadata.saveName));
         
         // Detector parameters
         fieldPixelSize.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.detector.pixelSize));
         fieldDistToSample.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.detector.distToSample));

         fieldOffsetX.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.detector.offSetFromBottomRight.x));
         fieldOffsetY.onEndEdit.AddListener(text => ParseField(text, ref preset.properties.detector.offSetFromBottomRight.y));

         void SetComponent(string text, ref Vector2Int variable, int position)
         {
            if (IsValue(text)) variable[position] = int.Parse(text, _cultureInfo);
         }

         fieldResolutionX.onEndEdit.AddListener(text => SetComponent(text, ref preset.properties.detector.resolution, 0));
         fieldResolutionY.onEndEdit.AddListener(text => SetComponent(text, ref preset.properties.detector.resolution, 1));

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
      

      #region Load methods

      public void FillFromPreset(Preset source)
      {
         FillFromPreset(source.metadata);
         FillFromPreset(source.properties.absorption);
         FillFromPreset(source.properties.angle);
         FillFromPreset(source.properties.detector);
         FillFromPreset(source.properties.ray);
         FillFromPreset(source.properties.sample);
      }

      private void FillFromPreset(Metadata source)
      {
         if (!IsValue(fieldPresetName.text)) preset.metadata.saveName = "Default_" + _namelessNumber++;
         
         RefreshMetadataUI();
      }

      private void FillFromPreset(AbsorptionProperties source)
      {
         throw new NotImplementedException();
         RefreshAbsorptionPropertiesUI();
      }

      private void FillFromPreset(AngleProperties source)
      {
         if (!IsValue(fieldAngleStart.text)) preset.properties.angle.angleStart = source.angleStart;
         if (!IsValue(fieldAngleEnd.text)) preset.properties.angle.angleEnd = source.angleEnd;
         if (!IsValue(fieldAngleSteps.text)) preset.properties.angle.angleCount = source.angleCount;
      }

      private void FillFromPreset(RayProperties source)
      {
         throw new NotImplementedException();
         RefreshRayPropertiesUI();
      }

      private void FillFromPreset(DetectorProperties source)
      {
         if (!IsValue(fieldOffsetX.text)) 
            preset.properties.detector.offSetFromBottomRight.x = source.offSetFromBottomRight.x;
         if (!IsValue(fieldOffsetY.text)) 
            preset.properties.detector.offSetFromBottomRight.y = source.offSetFromBottomRight.y;
         if (!IsValue(fieldPixelSize.text)) preset.properties.detector.pixelSize = source.pixelSize;
         if (!IsValue(fieldResolutionX.text)) preset.properties.detector.resolution.x = source.resolution.x;
         if (!IsValue(fieldResolutionY.text)) preset.properties.detector.resolution = source.resolution;
         if (!IsValue(fieldDistToSample.text)) preset.properties.detector.distToSample = source.distToSample;

         RefreshDetectorPropertiesUI();
      }

      private void FillFromPreset(SampleProperties source)
      {
         if (!IsValue(fieldGridResolution.text))  preset.properties.sample.gridResolution = source.gridResolution;
         if (!IsValue(fieldDiameter.text))  preset.properties.sample.totalDiameter = source.totalDiameter;
         if (!IsValue(fieldCellThickness.text))  preset.properties.sample.cellThickness = source.cellThickness;
         if (!IsValue(fieldMuCell.text))  preset.properties.sample.muCell = source.muCell;
         if (!IsValue(fieldMuSample.text))  preset.properties.sample.muSample = source.muSample;
         
         RefreshSamplePropertiesUI();
      }

      #endregion

      
      
      #region Data update methods

      public void UpdateModeData()
      {
         if (!_dropdownMap.ContainsKey(dropdownMode.value)) throw new InvalidOperationException();
         preset.properties.absorption.mode = _dropdownMap[dropdownMode.value];
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
         if (!_dropdownMap.ContainsValue(mode)) throw new InvalidOperationException();
         dropdownMode.value = _dropdownMap.FirstOrDefault(e => e.Value==mode).Key;
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
      }
      
      public void RefreshSamplePropertiesUI()
      {
         fieldGridResolution.text = preset.properties.sample.gridResolution.ToString(_cultureInfo);
         fieldDiameter.text = preset.properties.sample.totalDiameter.ToString(_cultureInfo);
         fieldCellThickness.text = preset.properties.sample.cellThickness.ToString(_cultureInfo);
         fieldMuCell.text = preset.properties.sample.muCell.ToString(_cultureInfo);
         fieldMuSample.text = preset.properties.sample.muSample.ToString(_cultureInfo);
      }
      
      public void RefreshRayPropertiesUI()
      {
         // TODO: implement as soon as ray properties are available.
      }

      public void RefreshDetectorPropertiesUI()
      {
         fieldPixelSize.text = preset.properties.detector.pixelSize.ToString(_cultureInfo);
         fieldOffsetX.text = preset.properties.detector.offSetFromBottomRight.x.ToString(_cultureInfo);
         fieldOffsetY.text = preset.properties.detector.offSetFromBottomRight.y.ToString(_cultureInfo);
         fieldDistToSample.text = preset.properties.detector.distToSample.ToString(_cultureInfo);
         fieldResolutionX.text = preset.properties.detector.resolution.x.ToString(_cultureInfo);
         fieldResolutionY.text = preset.properties.detector.resolution.y.ToString(_cultureInfo);
      }
      
      public void RefreshAnglePropertiesUI()
      {
         fieldPathAngleFile.text = preset.properties.angle.pathToAngleFile;
         fieldAngleStart.text = preset.properties.angle.angleStart.ToString(_cultureInfo);
         fieldAngleEnd.text = preset.properties.angle.angleEnd.ToString(_cultureInfo);
         fieldAngleSteps.text = preset.properties.angle.angleCount.ToString(_cultureInfo);
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