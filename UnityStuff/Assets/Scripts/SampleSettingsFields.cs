using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SampleSettingsFields : MonoBehaviour {
    
    public InputField fieldDiameter;
    public InputField fieldCellThickness;
    public InputField fieldMuCell;
    public InputField fieldMuSample;

    public SampleSettings sampleSettings;

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

    public void aktualisiere(bool userInput) {
        if (userInput) {
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
            fieldDiameter.text = sampleSettings.totalDiameter.ToString();
            fieldCellThickness.text = sampleSettings.cellThickness.ToString();
            fieldMuCell.text = sampleSettings.muCell.ToString();
            fieldMuSample.text = sampleSettings.muSample.ToString();

        }
    }

}
