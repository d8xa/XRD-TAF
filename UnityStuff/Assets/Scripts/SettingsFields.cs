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

   
   public Settings settings;
   public SampleSettings sampleSettings;
   public DetektorSettings detektorSettings;
   public RaySettings raySettings;
   
   
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
      switch (settings.mode)
      {
         case Model.Mode.Point:
            fieldPixelSize.gameObject.SetActive(false);
            fieldOffsetX.gameObject.SetActive(false);
            fieldOffsetY.gameObject.SetActive(false);
            fieldResolutionX.gameObject.SetActive(false);
            fieldResolutionY.gameObject.SetActive(false);
            fieldDstToSample.gameObject.SetActive(false);
            
            fieldPathAngleFile.gameObject.SetActive(true);
            break;
         
         case Model.Mode.Area:
            fieldPathAngleFile.gameObject.SetActive(false);
            
            fieldPixelSize.gameObject.SetActive(true);
            fieldOffsetX.gameObject.SetActive(true);
            fieldOffsetY.gameObject.SetActive(true);
            fieldResolutionX.gameObject.SetActive(true);
            fieldResolutionY.gameObject.SetActive(true);
            fieldDstToSample.gameObject.SetActive(true);
            break;
         
         case Model.Mode.Integrated:
            fieldPixelSize.gameObject.SetActive(false);
            fieldOffsetX.gameObject.SetActive(false);
            fieldOffsetY.gameObject.SetActive(false);
            fieldResolutionX.gameObject.SetActive(false);
            fieldResolutionY.gameObject.SetActive(false);
            fieldDstToSample.gameObject.SetActive(false);
            fieldPathAngleFile.gameObject.SetActive(false);
            break;
         
         case Model.Mode.Testing:
            fieldPathAngleFile.gameObject.SetActive(false);
            
            fieldPixelSize.gameObject.SetActive(true);
            fieldOffsetX.gameObject.SetActive(true);
            fieldOffsetY.gameObject.SetActive(true);
            fieldResolutionX.gameObject.SetActive(true);
            fieldResolutionY.gameObject.SetActive(true);
            fieldDstToSample.gameObject.SetActive(true);
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
      
      if (!fieldDescriptor.text.Equals("")) 
         settings.aufbauBezeichnung = fieldDescriptor.text;

      if (!fieldAccuracy.text.Equals("")) 
         settings.computingAccuracy = float.Parse(fieldAccuracy.text, cultureInfo);

      if (!fieldOffsetX.text.Equals("") && !fieldOffsetY.text.Equals(""))
      {
         detektorSettings.offSetFromDownRightEdge.x = float.Parse(fieldOffsetX.text, cultureInfo);
         detektorSettings.offSetFromDownRightEdge.y = float.Parse(fieldOffsetY.text, cultureInfo);
      }

      if (!fieldPixelSize.text.Equals("")) 
         detektorSettings.pixelsize = float.Parse(fieldPixelSize.text, cultureInfo);

      if (!fieldResolutionX.text.Equals("") && !fieldResolutionY.text.Equals(""))
      {
         detektorSettings.resolution.x = int.Parse(fieldResolutionX.text);
         detektorSettings.resolution.y = int.Parse(fieldResolutionY.text);
      }

      if (!fieldDstToSample.text.Equals("")) 
         detektorSettings.distToSample = float.Parse(fieldDstToSample.text, cultureInfo);

      if (!fieldPathAngleFile.text.Equals("")) 
         detektorSettings.pathToAngleFile = fieldPathAngleFile.text;

      if (!fieldDiameter.text.Equals("")) 
         sampleSettings.totalDiameter = float.Parse(fieldDiameter.text, cultureInfo);

      if (!fieldCellThickness.text.Equals("")) 
         sampleSettings.cellThickness = float.Parse(fieldCellThickness.text, cultureInfo);

      if (!fieldMuCell.text.Equals("")) 
         sampleSettings.muCell = float.Parse(fieldMuCell.text, cultureInfo);

      if (!fieldMuSample.text.Equals("")) 
         sampleSettings.muSample = float.Parse(fieldMuSample.text, cultureInfo);
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

