using System.Collections;
using System.Collections.Generic;
using FoPra.model;
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
   public Settings settings;

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

   public void aktualisiere(bool userInput) {
      if (userInput) {
         if (!fieldDescriptor.text.Equals("")) {
            settings.aufbauBezeichnung = fieldDescriptor.text;
         }
         
         if (dropdownMode.value == 0) {
            settings.mode = Model.Modes.Point;
         } else if(dropdownMode.value == 1) {
            settings.mode = Model.Modes.Area;
         } else if(dropdownMode.value == 2) {
            settings.mode = Model.Modes.Integrated;
         }
         
         if (absType.value == 0) {
            settings.absType = Model.AbsorbtionType.All;
         } else if(absType.value == 1) {
            settings.absType = Model.AbsorbtionType.Sample;
         } else if(absType.value == 2) {
            settings.absType = Model.AbsorbtionType.Cell;
         } else if(absType.value == 3) {
            settings.absType = Model.AbsorbtionType.CellAndSample;
         }


         if (!fieldAccuracy.text.Equals("")) {
            settings.computingAccuracy = float.Parse(fieldAccuracy.text);
         }
         
      } else {
         fieldDescriptor.text = settings.aufbauBezeichnung;
         fieldAccuracy.text = settings.computingAccuracy.ToString();
      }
   }
   
}

