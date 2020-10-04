using System;
using model;
using UnityEngine;
using UnityEngine.UI;
using static util.FieldParseTools;

namespace ui
{
    public class SettingsPanel : MonoBehaviour
    {
        public Button closeButton;
        public Button saveChanges;

        private bool hasUnsavedChanges;

        // TODO: add input members.
        public InputField fieldSampleAreaMargin;
        public Toggle toggleRadian;
        public Toggle toggleFillEmpty;
        public Toggle toggleWriteSeparateFiles;
        
        // copy of Settings members to store unsaved changes.
        private Settings.Flags flags;
        private Settings.DefaultValues defaults;

        private void Awake()
        {
            LoadFromSettings();
            RefreshUI();

            InitializeListeners();
        }

        private void InitializeListeners()
        {
            //saveChanges.onClick.AddListener((SaveChanges));

            fieldSampleAreaMargin.onValueChanged.AddListener(text =>
            {
                var valid = false;
                if (IsValue(text))
                {
                    var value = ParseFloat(text);
                    valid = value != null && IsMarginValid(value);
                }

                fieldSampleAreaMargin.image.color = GetValidationColor(valid);
            });
            
            fieldSampleAreaMargin.onEndEdit.AddListener(text =>
            {
                if (IsValue(text))
                {
                    var value = ParseFloat(text);
                    if (value != null && IsMarginValid(value)) defaults.sampleAreaMarginDefault = (float) value / 100f;
                }

                fieldSampleAreaMargin.image.color = GetValidationColor(true); // reset to valid state.
                fieldSampleAreaMargin.text = (defaults.sampleAreaMarginDefault*100f).ToString(defaults.cultureInfo);

                CheckForChanges();
            });
            
            toggleRadian.onValueChanged.AddListener(value =>
            {
                flags.useRadian = value;
                CheckForChanges();
            });
            
            toggleFillEmpty.onValueChanged.AddListener(value =>
            {
                flags.fillEmptyWithDefault = value;
                CheckForChanges();
            });
            
            toggleWriteSeparateFiles.onValueChanged.AddListener(value =>
            {
                flags.planeModeWriteSeparateFiles = value;
                CheckForChanges();
            });
            
            // TODO: continue impl.
        }

        private bool IsMarginValid(float? value) => 0 <= value && value <= 100;

        private Color GetValidationColor(bool valid) => valid ? Color.white : Color.red;

        private void LoadFromSettings()
        {
            flags = Settings.flags.DeepCopy();
            defaults = Settings.defaults.DeepCopy();
        }

        private void ShowChangesIndicator(bool value)
        {
            hasUnsavedChanges = value; 
            saveChanges.gameObject.SetActive(value);
        }

        private void RefreshUI()
        {
            fieldSampleAreaMargin.text = (defaults.sampleAreaMarginDefault*100).ToString(defaults.cultureInfo);
            toggleRadian.isOn = flags.useRadian;
            toggleFillEmpty.isOn = flags.fillEmptyWithDefault;
            toggleWriteSeparateFiles.isOn = flags.planeModeWriteSeparateFiles;
        }

        private void CheckForChanges()
        {
            if (Math.Abs(defaults.sampleAreaMarginDefault - Settings.defaults.sampleAreaMarginDefault) > 1E-3)
                hasUnsavedChanges = true;
            else if (flags.useRadian != Settings.flags.useRadian) hasUnsavedChanges = true;
            else if (flags.fillEmptyWithDefault != Settings.flags.fillEmptyWithDefault) hasUnsavedChanges = true;
            else if (flags.planeModeWriteSeparateFiles != Settings.flags.planeModeWriteSeparateFiles) 
                hasUnsavedChanges = true;
            else hasUnsavedChanges = false;
            
            saveChanges.gameObject.SetActive(hasUnsavedChanges);
        }

        private void SaveChanges()
        {
            Settings.flags = flags;
            Settings.defaults = defaults;
            // TODO: serialize here or in Settings class

            ShowChangesIndicator(false);
        }
    }
}