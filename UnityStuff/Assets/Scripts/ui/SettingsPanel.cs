using System;
using System.IO;
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

        private bool _hasUnsavedChanges;

        public InputField fieldSampleAreaMargin;
        public Toggle toggleWriteSeparateFiles;
        public Toggle toggleFillEmpty;
        public Toggle toggleRadian;
        public Toggle toggleClipAngles;
        public Toggle toggleWriteLogs;
        
        // copy of Settings members to store unsaved changes.
        private Settings.Flags _flags;
        private Settings.DefaultValues _defaults;
        

        private void Awake()
        {
            try
            {
                LoadFromFile();
                LoadFromSettings();
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                LoadFromSettings();
                SaveChanges();
            }
            RefreshUI();

            InitializeListeners();
        }
        
        private void InitializeListeners()
        {
            saveChanges.onClick.AddListener(SaveChanges);

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
                    if (value != null && IsMarginValid(value)) _defaults.samplePaddingDefault = (float) value / 100f;
                }

                fieldSampleAreaMargin.image.color = GetValidationColor(true); // reset to valid state.
                fieldSampleAreaMargin.text = (_defaults.samplePaddingDefault*100f).ToString(_defaults.cultureInfo);

                CheckForChanges();
            });
            
            toggleRadian.onValueChanged.AddListener(value =>
            {
                _flags.useRadian = value;
                CheckForChanges();
            });
            
            toggleFillEmpty.onValueChanged.AddListener(value =>
            {
                _flags.fillEmptyWithDefault = value;
                CheckForChanges();
            });
            
            toggleWriteSeparateFiles.onValueChanged.AddListener(value =>
            {
                _flags.planeModeWriteSeparateFiles = value;
                CheckForChanges();
            });
            
            toggleWriteLogs.onValueChanged.AddListener(value =>
            {
                _flags.writeLogs = value;
                CheckForChanges();
            });
            
            toggleClipAngles.onValueChanged.AddListener(value =>
            {
                _flags.clipAngles = value;
                CheckForChanges();
            });
        }

        private bool IsMarginValid(float? value) => 0 <= value && value <= 100;

        private Color GetValidationColor(bool valid) => valid ? Color.white : Color.red;
        
        private void ShowChangesIndicator(bool value)
        {
            _hasUnsavedChanges = value; 
            saveChanges.gameObject.SetActive(value);
        }

        private void RefreshUI()
        {
            fieldSampleAreaMargin.text = (_defaults.samplePaddingDefault*100).ToString(_defaults.cultureInfo);
            toggleWriteSeparateFiles.isOn = _flags.planeModeWriteSeparateFiles;
            toggleFillEmpty.isOn = _flags.fillEmptyWithDefault;
            toggleRadian.isOn = _flags.useRadian;
            toggleClipAngles.isOn = _flags.clipAngles;
            toggleWriteLogs.isOn = _flags.writeLogs;
        }

        private void CheckForChanges()
        {
            if (Math.Abs(_defaults.samplePaddingDefault - Settings.defaults.samplePaddingDefault) > 1E-3)
                _hasUnsavedChanges = true;
            else if (_flags.useRadian != Settings.flags.useRadian) _hasUnsavedChanges = true;
            else if (_flags.fillEmptyWithDefault != Settings.flags.fillEmptyWithDefault) _hasUnsavedChanges = true;
            else if (_flags.planeModeWriteSeparateFiles != Settings.flags.planeModeWriteSeparateFiles) 
                _hasUnsavedChanges = true;
            else if (_flags.clipAngles != Settings.flags.clipAngles) _hasUnsavedChanges = true;
            else if (_flags.writeLogs != Settings.flags.writeLogs) _hasUnsavedChanges = true;
            else _hasUnsavedChanges = false;
            
            saveChanges.gameObject.SetActive(_hasUnsavedChanges);
        }

        private void SaveChanges()
        {
            Settings.CopyUserSettings(_flags, Settings.flags);
            Settings.CopyUserSettings(_defaults, Settings.defaults);

            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Settings");
            Directory.CreateDirectory(saveDir);
            var path = Path.Combine(saveDir, "settings" + Settings.DefaultValues.SerializedExtension);
            Settings.current.Serialize(path);
            
            ShowChangesIndicator(false);
        }
        
        private static void LoadFromFile()
        {
            var saveDir = Path.Combine(Directory.GetCurrentDirectory(), "Settings");
            var path = Path.Combine(saveDir, "settings" + Settings.DefaultValues.SerializedExtension);
            
            if (!File.Exists(path)) 
                throw new FileNotFoundException("Could not load preset; file not found.");
            var presetJson = File.ReadAllText(path, Settings.DefaultValues.Encoding);
            using (var stream = new MemoryStream(Settings.DefaultValues.Encoding.GetBytes(presetJson)))
            {
                var settings = (Settings) Settings.DefaultValues.SettingsSerializer.ReadObject(stream);
                Settings.CopyUserSettings(settings, Settings.current);
            }
        }
        
        /// <summary>
        /// Copies all settings to class variables.
        /// </summary>
        private void LoadFromSettings()
        {
            _flags = Settings.flags.DeepCopy();
            _defaults = Settings.defaults.DeepCopy();
        }
    }
}