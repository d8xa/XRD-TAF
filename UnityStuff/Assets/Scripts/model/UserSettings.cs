using System.Globalization;
using System.Runtime.Serialization;

namespace model
{
    [DataContract]
    public class Settings
    {
        private static Settings _instance;

        [DataMember(Name = "flags")] private Flags _flags;
        [DataMember] private DefaultValues defaultValues;

        private Settings()
        {
            _flags = new Flags();
            defaultValues = new DefaultValues();
            // TODO: try loading default. Maybe move load operation to DataHandler.
        }
        
        private static Settings GetInstance()
        {
            if (_instance == null) _instance = new Settings();
            return _instance;
        }

        public static Settings current => GetInstance();

        
        [DataContract]
        public class DefaultValues
        {
            /// <summary>
            /// The default margin by which each side of the simulated sample cross-section area will be extended.
            /// </summary>
            [DataMember] public float sampleAreaMarginDefault = 0.04f;
            public CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            
            public DefaultValues DeepCopy()
            {
                var defaults = new DefaultValues()
                {
                    sampleAreaMarginDefault = sampleAreaMarginDefault,
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
            
            // only for debugging use.
            public bool writeFactors = true;
            
            public Flags DeepCopy()
            {
                var flags = new Flags
                {
                    useRadian = useRadian,
                    writeFactors = writeFactors,
                    fillEmptyWithDefault = fillEmptyWithDefault,
                    planeModeWriteSeparateFiles = planeModeWriteSeparateFiles
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
            get => current.defaultValues;
            set => current.defaultValues = value;
        }
    }
}