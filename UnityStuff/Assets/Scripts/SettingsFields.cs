using System.Collections;
using System.Collections.Generic;
using FoPra.model;
using UnityEngine;
using UnityEngine.UI;

public class SettingsFields : MonoBehaviour {
   public InputField fieldDescriptor;
   //public InputField fielLoadName;
   public Dropdown dropdownMode;
   //public Dropdown dropDownAbsType;

   public Settings settings;

   

   public void aktualisiere(bool userInput) {
      if (userInput) {
         if (!fieldDescriptor.text.Equals("")) {
            settings.aufbauBezeichnung = fieldDescriptor.text;
         }

         /*if (!fielLoadName.text.Equals("")) {
            settings.loadName = fielLoadName.text;
         }*/

         if (dropdownMode.value == 0) {
            settings.mode = Model.Modes.Point;
         } else if(dropdownMode.value == 1) {
            settings.mode = Model.Modes.Area;
         } else if(dropdownMode.value == 2) {
            settings.mode = Model.Modes.Integrated;
         }
         
      } else {
         fieldDescriptor.text = settings.aufbauBezeichnung;
      }
   }
   
}
