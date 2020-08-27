using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FoPra.model;
using model;
using UnityEngine;
using UnityEngine.UI;

public class SettingsFields : MonoBehaviour {
   public static int namelessNumber;
   public InputField fieldDescriptor;
   //public InputField fielLoadName;
   public Dropdown dropdownMode;

   public Dropdown absType;
   //public Dropdown dropDownAbsType;
   public InputField fieldAccuracy;
   
   public InputField fieldPixelSize;
   public InputField fieldResolutionX;
   public InputField fieldResolutionY;
   public InputField fieldDstToSample;
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
   public DetektorSettings detektorSettings;
   public RaySettings raySettings;

   // for component-wise / group-wise selection of input fields.
   public GameObject inputGroupDetector;
   public GameObject inputGroupAngle;
   public GameObject inputGroupIntegrated;

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
      return new Model(settings, detektorSettings, sampleSettings);
   }

   public void FillInDefaults(Settings defaultSettings) {
      if (fieldDescriptor.text.Equals("")) {
         settings.aufbauBezeichnung = "Default_" + namelessNumber;
         namelessNumber++;
      }


      if (fieldAccuracy.text.Equals("")) {
         settings.computingAccuracy = defaultSettings.computingAccuracy;
      }
      DataChanged();
   }
   
   public void FillInDefaults(DetektorSettings defaultSettings) {
      if (fieldOffsetX.text.Equals("") && fieldOffsetY.text.Equals("")) {
         detektorSettings.offSetFromDownRightEdge = defaultSettings.offSetFromDownRightEdge;
      }
      if (fieldPixelSize.text.Equals("")) {
         detektorSettings.pixelsize = defaultSettings.pixelsize;
      }
      if (fieldResolutionX.text.Equals("") && fieldResolutionY.text.Equals("")) {
         detektorSettings.resolution = defaultSettings.resolution;
      }

      if (fieldDstToSample.text.Equals("")) {
         detektorSettings.distToSample = defaultSettings.distToSample;
      }

      if (fieldPathAngleFile.text.Equals("")) {
         detektorSettings.pathToAngleFile = defaultSettings.pathToAngleFile;
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


   private readonly Dictionary<int, Model.Mode> _dropdownMap = new Dictionary<int, Model.Mode>
   {
      {0, Model.Mode.Point},
      {1, Model.Mode.Area},
      {2, Model.Mode.Integrated},
      {3, Model.Mode.Testing},
   };

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
      ParseField(fieldDescriptor, ref settings.aufbauBezeichnung);
      ParseField(fieldAccuracy, ref settings.computingAccuracy, cultureInfo);
      
      // Detector parameters
      ParseField(fieldPixelSize, ref detektorSettings.pixelsize, cultureInfo);
      ParseField(fieldDstToSample, ref detektorSettings.distToSample, cultureInfo);
      
      if (!fieldOffsetX.text.Equals("") && !fieldOffsetY.text.Equals(""))
      {
         detektorSettings.offSetFromDownRightEdge.x = float.Parse(fieldOffsetX.text, cultureInfo);
         detektorSettings.offSetFromDownRightEdge.y = float.Parse(fieldOffsetY.text, cultureInfo);
      }
      
      if (!fieldResolutionX.text.Equals("") && !fieldResolutionY.text.Equals(""))
      {
         detektorSettings.resolution.x = int.Parse(fieldResolutionX.text);
         detektorSettings.resolution.y = int.Parse(fieldResolutionY.text);
      }
      
      // Sample parameters
      ParseField(fieldDiameter, ref sampleSettings.totalDiameter, cultureInfo);
      ParseField(fieldCellThickness, ref sampleSettings.cellThickness, cultureInfo);
      ParseField(fieldMuCell, ref sampleSettings.muCell, cultureInfo);
      ParseField(fieldMuSample, ref sampleSettings.muSample, cultureInfo);
      
      // Angles
      ParseField(fieldPathAngleFile, ref detektorSettings.pathToAngleFile);
      ParseField(fieldAngleStart, ref detektorSettings.angleStart, cultureInfo);
      ParseField(fieldAngleEnd, ref detektorSettings.angleEnd, cultureInfo);
      ParseField(fieldAngleSteps, ref detektorSettings.angleCount, cultureInfo);
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
      
      fieldDescriptor.text = settings.aufbauBezeichnung;
      fieldAccuracy.text = settings.computingAccuracy.ToString(cultureInfo);
         
      fieldPixelSize.text = detektorSettings.pixelsize.ToString(cultureInfo);
      fieldOffsetX.text = detektorSettings.offSetFromDownRightEdge.x.ToString(cultureInfo);
      fieldOffsetY.text = detektorSettings.offSetFromDownRightEdge.y.ToString(cultureInfo);
      fieldDstToSample.text = detektorSettings.distToSample.ToString(cultureInfo);
      fieldResolutionX.text = detektorSettings.resolution.x.ToString(cultureInfo);
      fieldResolutionY.text = detektorSettings.resolution.y.ToString(cultureInfo);
      fieldPathAngleFile.text = detektorSettings.pathToAngleFile;
         
      fieldDiameter.text = sampleSettings.totalDiameter.ToString(cultureInfo);
      fieldCellThickness.text = sampleSettings.cellThickness.ToString(cultureInfo);
      fieldMuCell.text = sampleSettings.muCell.ToString(cultureInfo);
      fieldMuSample.text = sampleSettings.muSample.ToString(cultureInfo);
   }

}

