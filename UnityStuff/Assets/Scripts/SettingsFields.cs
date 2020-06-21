using System;
using FoPra.model;
using model;
using UnityEditor;
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

   public void fillInDefaults(Settings defaultSettings) {
      if (fieldDescriptor.text.Equals("")) {
         settings.aufbauBezeichnung = "Default_" + namelessNumber;
         namelessNumber++;
      }


      if (fieldAccuracy.text.Equals("")) {
         settings.computingAccuracy = defaultSettings.computingAccuracy;
      }
      aktualisiere(false);
   }
   
   public void fillInDefaults(DetektorSettings defaultDetektorSettings) {
      if (fieldOffsetX.text.Equals("") && fieldOffsetY.text.Equals("")) {
         detektorSettings.offSetFromDownRightEdge = defaultDetektorSettings.offSetFromDownRightEdge;
      }
      if (fieldPixelSize.text.Equals("")) {
         detektorSettings.pixelsize = defaultDetektorSettings.pixelsize;
      }
      if (fieldResolutionX.text.Equals("") && fieldResolutionY.text.Equals("")) {
         detektorSettings.resolution = defaultDetektorSettings.resolution;
      }

      if (fieldDstToSample.text.Equals("")) {
         detektorSettings.distToSample = defaultDetektorSettings.distToSample;
      }

      if (fieldPathAngleFile.text.Equals("")) {
         detektorSettings.pathToAngleFile = defaultDetektorSettings.pathToAngleFile;
      }
      aktualisiere(false);
   }
   
   public void fillInDefaults(SampleSettings defaultSampleSettings) {
      if (fieldDiameter.text.Equals("")) {
         sampleSettings.totalDiameter = defaultSampleSettings.totalDiameter;
      }
      if (fieldCellThickness.text.Equals("")) {
         sampleSettings.cellThickness = defaultSampleSettings.cellThickness;
      }
      if (fieldMuCell.text.Equals("")) {
         sampleSettings.muCell = defaultSampleSettings.muCell;
      }
      if (fieldMuSample.text.Equals("")) {
         sampleSettings.muSample = defaultSampleSettings.muSample;
      }
      aktualisiere(false);
   }

   
   // TODO: refactor
   public void aktualisiereModus(bool userInput) {
      if (userInput) PrepareMode();
      else PrepareDropdown();
      
      
      SetObjects();
   }

   private void SetObjects()
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

   private void PrepareMode()
   {
      switch (dropdownMode.value)
         {
            case 0:
               settings.mode = Model.Mode.Point;
               break;
            
            case 1:
               settings.mode = Model.Mode.Area;
               break;
            
            case 2:
               settings.mode = Model.Mode.Integrated;
               break;
            
            case 3:
               settings.mode = Model.Mode.Testing;
               break;
            
            default:
               throw new InvalidOperationException();
         }
   }

   private void PrepareDropdown()
   {
      switch (settings.mode)
      {
         case Model.Mode.Point:
            dropdownMode.value = 0;
            break;
         
         case Model.Mode.Area:
            dropdownMode.value = 1;
            break;
         
         case Model.Mode.Integrated:
            dropdownMode.value = 2;
            break;
         
         case Model.Mode.Testing:
            dropdownMode.value = 3;
            break;
            
         default: 
            throw new NotImplementedException();
      }
   }
   
   // TODO: make parsing culture invariant.
   public void aktualisiere(bool userInput) {
      if (userInput) {
         if (!fieldDescriptor.text.Equals("")) {
            settings.aufbauBezeichnung = fieldDescriptor.text;
         }

         if (!fieldAccuracy.text.Equals("")) {
            settings.computingAccuracy = float.Parse(fieldAccuracy.text);
         }
         
         if (!fieldOffsetX.text.Equals("") && !fieldOffsetY.text.Equals("")) {
            detektorSettings.offSetFromDownRightEdge.x = float.Parse(fieldOffsetX.text);
            detektorSettings.offSetFromDownRightEdge.y = float.Parse(fieldOffsetY.text);
         }
         if (!fieldPixelSize.text.Equals("")) {
            detektorSettings.pixelsize = float.Parse(fieldPixelSize.text);
         }
         if (!fieldResolutionX.text.Equals("") && !fieldResolutionY.text.Equals("")) {
            detektorSettings.resolution.x = int.Parse(fieldResolutionX.text);
            detektorSettings.resolution.y = int.Parse(fieldResolutionY.text);
         }
         if (!fieldDstToSample.text.Equals("")) {
            detektorSettings.distToSample = float.Parse(fieldDstToSample.text);
         }
         if (!fieldPathAngleFile.text.Equals("")) {
            detektorSettings.pathToAngleFile = fieldPathAngleFile.text;
         }
         
         if (!fieldDiameter.text.Equals("")) {
            sampleSettings.totalDiameter = float.Parse(fieldDiameter.text);
         }
         if (!fieldCellThickness.text.Equals("")) {
            sampleSettings.cellThickness = float.Parse(fieldCellThickness.text);
         }
         if (!fieldMuCell.text.Equals("")) {
            sampleSettings.muCell = float.Parse(fieldMuCell.text);
         }
         if (!fieldMuSample.text.Equals("")) {
            sampleSettings.muSample = float.Parse(fieldMuSample.text);
         }
         
      } else {

         fieldDescriptor.text = settings.aufbauBezeichnung;
         fieldAccuracy.text = settings.computingAccuracy.ToString();
         
         fieldPixelSize.text = detektorSettings.pixelsize.ToString();
         fieldOffsetX.text = detektorSettings.offSetFromDownRightEdge.x.ToString();
         fieldOffsetY.text = detektorSettings.offSetFromDownRightEdge.y.ToString();
         fieldDstToSample.text = detektorSettings.distToSample.ToString();
         fieldResolutionX.text = detektorSettings.resolution.x.ToString();
         fieldResolutionY.text = detektorSettings.resolution.y.ToString();
         fieldPathAngleFile.text = detektorSettings.pathToAngleFile;
         
         fieldDiameter.text = sampleSettings.totalDiameter.ToString();
         fieldCellThickness.text = sampleSettings.cellThickness.ToString();
         fieldMuCell.text = sampleSettings.muCell.ToString();
         fieldMuSample.text = sampleSettings.muSample.ToString();
      }
   }

}

