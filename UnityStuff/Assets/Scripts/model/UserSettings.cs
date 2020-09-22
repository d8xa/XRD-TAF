namespace model
{
    public class Settings
    {
        private static Settings _instance;
        
        private Flags _flags;
        private DefaultValues defaultValues;

        private Settings()
        {
            _flags = new Flags();
            defaultValues = new DefaultValues();
        }
        
        private static Settings GetInstance()
        {
            if (_instance == null) _instance = new Settings();
            return _instance;
        }

        public static Settings current => _instance;

        public class DefaultValues
        {
            /// <summary>
            /// The default margin by which each side of the simulated sample cross-section area will be extended.
            /// </summary>
            public float sampleAreaMarginDefault = 0.04f;
        }

        public class Flags
        {
            /// <summary>
            /// If enabled, one file is written per selected absorption target.
            /// If disabled, vector datatype will be used to write all absorption values in one file.
            /// </summary>
            public bool planeModeWriteSeparateFiles;    

            /// <summary>
            /// If enabled, any empty fields in the preset will be filled with values from the default preset.
            /// </summary>
            public bool fillEmptyWithDefault = true;
            
            // only for debugging use.
            public bool writeFactors = true;
        }

        public static Flags flags => GetInstance()._flags;
        public static DefaultValues defaults => GetInstance().defaultValues;

    }

    
}