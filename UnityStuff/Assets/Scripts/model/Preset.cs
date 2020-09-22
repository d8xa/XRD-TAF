using model.properties;

namespace model
{
    //[System.Serializable]
    public class Preset
    {
        public Metadata metadata;
        public Properties properties;

        public Preset(Metadata metadata, Properties properties)
        {
            this.metadata = metadata;
            this.properties = properties;
        }
    }
    
    /// <summary>
    /// Contains all metadata of the preset.
    /// </summary>
    [System.Serializable]
    public class Metadata
    {
        public string pathInputData;
        public string pathOutputData;
        public string saveName;
        public string description;

        public Metadata(string pathInputData, string saveName, string description)
        {
            this.pathInputData = pathInputData;
            this.saveName = saveName;
            this.description = description;
        }
    }

    /// <summary>
    /// Contains all properties of the experiment set-up.
    /// </summary>
    [System.Serializable]
    public class Properties
    {
        private AbsorptionProperties absorptionProperties;
        private AngleProperties angleProperties;
        private DetectorProperties detectorProperties;
        private RayProperties rayProperties;
        private SampleProperties sampleProperties;

        public Properties(AbsorptionProperties absorptionProperties, AngleProperties angleProperties, 
            DetectorProperties detectorProperties, RayProperties rayProperties, SampleProperties sampleProperties)
        {
            this.absorptionProperties = absorptionProperties;
            this.sampleProperties = sampleProperties;
            this.rayProperties = rayProperties;
            this.detectorProperties = detectorProperties;
            this.angleProperties = angleProperties;
        }

        # region Property accessors
        
        public AbsorptionProperties absorption
        {
            get => absorptionProperties;
            set => absorptionProperties = value;
        }

        public AngleProperties angle
        {
            get => angleProperties;
            set => angleProperties = value;
        }
        
        public DetectorProperties detector
        {
            get => detectorProperties;
            set => detectorProperties = value;
        }
        
        public RayProperties ray
        {
            get => rayProperties;
            set => rayProperties = value;
        }
        
        public SampleProperties sample
        {
            get => sampleProperties;
            set => sampleProperties = value;
        }
        
        #endregion
    }
}