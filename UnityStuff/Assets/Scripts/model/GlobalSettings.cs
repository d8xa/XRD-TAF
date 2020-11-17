using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace model
{
    [DataContract]
    public class Settings
    {
        private static Settings _instance;

        [DataMember(Name = "flags")] private Flags _flags;
        [DataMember] private DefaultValues _defaultValues;

        private Settings()
        {
            _flags = new Flags();
            _defaultValues = new DefaultValues();
        }
        
        private static Settings GetInstance()
        {
            if (_instance == null) _instance = new Settings();
            return _instance;
        }

        public static Settings current => GetInstance();
        
        public static Settings Deserialize(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Could not load preset; file not found.");
            var presetJson = File.ReadAllText(filePath, DefaultValues.Encoding);
            using (var stream = new MemoryStream(DefaultValues.Encoding.GetBytes(presetJson)))
                return (Settings) DefaultValues.SettingsSerializer.ReadObject(stream);
        }

        public void Serialize(string filepath)
        {
            using (var stream = File.Open(filepath, FileMode.Create)) 
            using (var writer = JsonReaderWriterFactory
                .CreateJsonWriter(stream, DefaultValues.Encoding, true, true, "\t"))
            {
                DefaultValues.SettingsSerializer.WriteObject(writer, this);
                writer.Flush();
            }
        }

        public static void CopyUserSettings(Flags source, Flags target)
        {
            target.planeModeWriteSeparateFiles = source.planeModeWriteSeparateFiles;
            target.fillEmptyWithDefault = source.fillEmptyWithDefault;
            target.useRadian = source.useRadian;
            target.clipAngles = source.clipAngles;
            target.writeLogs = source.writeLogs;
        }

        public static void CopyUserSettings(DefaultValues source, DefaultValues target)
        {
            target.samplePaddingDefault = source.samplePaddingDefault;
        }

        public static void CopyUserSettings(Settings source, Settings target)
        {
            CopyUserSettings(source._flags, target._flags);
            CopyUserSettings(source._defaultValues, target._defaultValues);
        }
        

        [DataContract]
        public class DefaultValues
        {
            /// <summary>
            /// The default margin by which each side of the simulated sample cross-section area will be extended.
            /// </summary>
            [DataMember] public float samplePaddingDefault = 0.04f;
            public CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            
            // Serialization settings:
            private static readonly DataContractJsonSerializerSettings SerializerSettings = 
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true,
                    IgnoreExtensionDataObject = true
                };
            public static readonly DataContractJsonSerializer PresetSerializer = 
                new DataContractJsonSerializer(typeof(Preset), SerializerSettings);
            public static readonly DataContractJsonSerializer SettingsSerializer = 
                new DataContractJsonSerializer(typeof(Settings), SerializerSettings);
            public static readonly Encoding Encoding = Encoding.UTF8;
            public const string SerializedExtension = ".json";

            public DefaultValues DeepCopy()
            {
                var defaults = new DefaultValues()
                {
                    samplePaddingDefault = samplePaddingDefault,
                    cultureInfo = cultureInfo
                };
                return defaults;
            }
        }

        [DataContract]
        public class Flags
        {
            /// <summary>
            /// If enabled, one file is written per selected absorption target.
            /// If disabled, vector datatype will be used to write all absorption values in one file.
            /// </summary>
            [DataMember] public bool planeModeWriteSeparateFiles; 

            /// <summary>
            /// If enabled, any empty fields in the preset will be filled with values from the default preset.
            /// </summary>
            [DataMember] public bool fillEmptyWithDefault = true;
            
            /// <summary>
            /// Use radian angles for input and output.
            /// </summary>
            [DataMember] public bool useRadian;
            
            /// <summary>
            /// In integrated mode, clip angles outside of the detector range.
            /// </summary>
            [DataMember] public bool clipAngles = true;
            
            /// <summary>
            /// Write logs. If enabled, useLogging is set to true. 
            /// </summary>
            [DataMember] public bool writeLogs;

            #region Internal settings

            /// <summary>
            /// Toggle to switch on or off the use of Logger objects.
            /// </summary>
            //[DataMember]
            public bool useLogging;
            
            public const bool IsDebugBuild = true;
            public const bool WriteFactors = true;

            #endregion
            
            
            public Flags DeepCopy()
            {
                var flags = new Flags
                {
                    planeModeWriteSeparateFiles = planeModeWriteSeparateFiles,
                    fillEmptyWithDefault = fillEmptyWithDefault,
                    useRadian = useRadian,
                    clipAngles = clipAngles,
                    writeLogs = writeLogs
                };
                return flags;
            }
        }

        public static Flags flags
        {
            get => current._flags;
            set => current._flags = value;
        }

        public static DefaultValues defaults
        {
            get => current._defaultValues;
            set => current._defaultValues = value;
        }
    }
}