using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using model;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ui
{
   public class SettingsFields : MonoBehaviour {
      
      #region Fields

      private static int _namelessNumber;
      public InputField fieldDescriptor;
      public Dropdown dropdownMode;

      public Dropdown absType;
      public InputField fieldAccuracy;
   
      public InputField fieldPixelSize;
      public InputField fieldResolutionX;
      public InputField fieldResolutionY;
      [FormerlySerializedAs("fieldDstToSample")] public InputField fieldDistToSample;
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
      [FormerlySerializedAs("detektorSettings")] public DetectorSettings detectorSettings;
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

      #endregion
      
      public void Awake()
      {
         SetAllInputGroups(false);
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

      public void FillInDefaults(Settings defaultSettings) {
         if (fieldDescriptor.text.Equals("")) {
            settings.saveName = "Default_" + _namelessNumber;
            _namelessNumber++;
         }


         if (fieldAccuracy.text.Equals("")) {
            settings.gridResolution = defaultSettings.gridResolution;
         }
         DataChanged();
      }
   
      public void FillInDefaults(DetectorSettings defaultSettings) {
         if (fieldOffsetX.text.Equals("") && fieldOffsetY.text.Equals("")) {
            detectorSettings.offSetFromDownRightEdge = defaultSettings.offSetFromDownRightEdge;
         }
         if (fieldPixelSize.text.Equals("")) {
            detectorSettings.pixelSize = defaultSettings.pixelSize;
         }
         if (fieldResolutionX.text.Equals("") && fieldResolutionY.text.Equals("")) {
            detectorSettings.resolution = defaultSettings.resolution;
         }

         if (fieldDistToSample.text.Equals("")) {
            detectorSettings.distToSample = defaultSettings.distToSample;
         }

         if (fieldPathAngleFile.text.Equals("")) {
            detectorSettings.pathToAngleFile = defaultSettings.pathToAngleFile;
         }
         DataChanged();
      }
   
      public void FillInDefaults(SampleSettings defaultSettings) {
         if (fieldDiameter.text.Equals("")) {
            sampleSettings.totalDiameter = defaultSettings.totalDiameter;
         }
         if (fieldCellThickness.text.Equals("")) {
            sampleSettings.cellThickness = defaultSettings.cellThickness;
         }
         if (fieldMuCell.text.Equals("")) {
            sampleSettings.muCell = defaultSettings.muCell;
         }
         if (fieldMuSample.text.Equals("")) {
            sampleSettings.muSample = defaultSettings.muSample;
         }
         DataChanged();
      }

      /// <summary>
      /// Updates main menu upon change of computation mode.
      /// Mode-relevant fields are set to visible, all other to invisible.
      /// </summary>
      /// <exception cref="InvalidOperationException"></exception>
      private void DropdownValueChanged()
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

   
      public void ModeChanged() {
         SetDropdownTo(settings.mode);
         DropdownValueChanged();
      }

      public void DropdownChanged()
      {
         if (!_dropdownMap.ContainsKey(dropdownMode.value)) throw new InvalidOperationException();
         settings.mode = _dropdownMap[dropdownMode.value];
         DropdownValueChanged();
      }

      /// <summary>Sets the internal dropdown value according to the supplied mode.</summary>
      private void SetDropdownTo(Model.Mode mode)
      {
         if (!_dropdownMap.ContainsValue(mode)) throw new InvalidOperationException();
         dropdownMode.value = _dropdownMap.FirstOrDefault(e => e.Value==mode).Key;
      }
   
      /// <summary>Updates each data field in the model with parsed input from UI input fields.</summary>
      public void InputChanged()
      {
         var cultureInfo = CultureInfo.InvariantCulture;

         // metadata
         ParseField(fieldDescriptor, ref settings.saveName);
         ParseField(fieldAccuracy, ref settings.gridResolution, cultureInfo);
      
         // Detector parameters
         ParseField(fieldPixelSize, ref detectorSettings.pixelSize, cultureInfo);
         ParseField(fieldDistToSample, ref detectorSettings.distToSample, cultureInfo);
      
         if (!fieldOffsetX.text.Equals("") && !fieldOffsetY.text.Equals(""))
         {
            detectorSettings.offSetFromDownRightEdge.x = float.Parse(fieldOffsetX.text, cultureInfo);
            detectorSettings.offSetFromDownRightEdge.y = float.Parse(fieldOffsetY.text, cultureInfo);
         }
      
         if (!fieldResolutionX.text.Equals("") && !fieldResolutionY.text.Equals(""))
         {
            detectorSettings.resolution.x = int.Parse(fieldResolutionX.text);
            detectorSettings.resolution.y = int.Parse(fieldResolutionY.text);
         }
      
         // Sample parameters
         ParseField(fieldDiameter, ref sampleSettings.totalDiameter, cultureInfo);
         ParseField(fieldCellThickness, ref sampleSettings.cellThickness, cultureInfo);
         ParseField(fieldMuCell, ref sampleSettings.muCell, cultureInfo);
         ParseField(fieldMuSample, ref sampleSettings.muSample, cultureInfo);
      
         // Angles
         ParseField(fieldPathAngleFile, ref detectorSettings.pathToAngleFile);
         ParseField(fieldAngleStart, ref detectorSettings.angleStart, cultureInfo);
         ParseField(fieldAngleEnd, ref detectorSettings.angleEnd, cultureInfo);
         ParseField(fieldAngleSteps, ref detectorSettings.angleCount, cultureInfo);
      }

      private void ParseField(InputField input, ref string target)
      {
         if (!input.text.Equals(""))
            target = input.text;
      }
   
      private void ParseField(InputField input, ref float target, CultureInfo cultureInfo = null)
      {
         if (cultureInfo == null) 
            cultureInfo = CultureInfo.InvariantCulture;
         if (!input.text.Equals("")) 
            target = float.Parse(input.text, cultureInfo);
      }

      private void ParseField(InputField input, ref int target, CultureInfo cultureInfo = null)
      {
         if (cultureInfo == null) 
            cultureInfo = CultureInfo.InvariantCulture;
         if (!input.text.Equals(""))
            target = int.Parse(input.text, cultureInfo);
      }
   
      /// <summary>
      /// Updates all input fields in the UI with the changed data from the model.
      /// </summary>
      public void DataChanged()
      {
         var cultureInfo = CultureInfo.InvariantCulture;
      
         fieldDescriptor.text = settings.saveName;
         fieldAccuracy.text = settings.gridResolution.ToString(cultureInfo);
         
         fieldPixelSize.text = detectorSettings.pixelSize.ToString(cultureInfo);
         fieldOffsetX.text = detectorSettings.offSetFromDownRightEdge.x.ToString(cultureInfo);
         fieldOffsetY.text = detectorSettings.offSetFromDownRightEdge.y.ToString(cultureInfo);
         fieldDistToSample.text = detectorSettings.distToSample.ToString(cultureInfo);
         fieldResolutionX.text = detectorSettings.resolution.x.ToString(cultureInfo);
         fieldResolutionY.text = detectorSettings.resolution.y.ToString(cultureInfo);
         fieldPathAngleFile.text = detectorSettings.pathToAngleFile;
         
         fieldDiameter.text = sampleSettings.totalDiameter.ToString(cultureInfo);
         fieldCellThickness.text = sampleSettings.cellThickness.ToString(cultureInfo);
         fieldMuCell.text = sampleSettings.muCell.ToString(cultureInfo);
         fieldMuSample.text = sampleSettings.muSample.ToString(cultureInfo);
      }
   }
}

