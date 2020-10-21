using System.Runtime.Serialization;
using JetBrains.Annotations;
using model.properties;

namespace model
{
    [DataContract]
    public class Preset
    {
        #region Autoproperties

        [DataMember(IsRequired = true)] 
        public Metadata metadata { get; set; }
        
        [DataMember(IsRequired = true)]
        public Properties properties { get; set; }

        #endregion

        public Preset(Metadata metadata, Properties properties)
        {
            this.metadata = metadata;
            this.properties = properties;
        }

        public Preset()
        {
            metadata = new Metadata();
            properties = new Properties();
        }
    }
    
    /// <summary>
    /// Contains all metadata of the preset.
    /// </summary>
    [DataContract]
    public class Metadata
    {
        #region Autoproperties

        [CanBeNull]
        [DataMember(IsRequired = true)]
        public string pathInputData { get; set; }
        
        [CanBeNull]
        [DataMember(IsRequired = true)]
        public string pathOutputData { get; set; }
        
        [CanBeNull]
        [DataMember(IsRequired = true)]
        public string saveName { get; set; }
        
        [CanBeNull]
        [DataMember(IsRequired = true)]
        public string description { get; set; }

        #endregion
        
        public Metadata(string pathInputData, string saveName, string description)
        {
            this.pathInputData = pathInputData;
            this.saveName = saveName;
            this.description = description;
        }

        public Metadata() { }
    }

    /// <summary>
    /// Contains all properties of the experiment set-up.
    /// </summary>
    [DataContract]
    public class Properties
    {
        #region Autoproperties
        
        [DataMember(IsRequired = true)]
        public AbsorptionProperties absorption { get; set; }

        [DataMember(IsRequired = true)]
        public AngleProperties angle { get; set; }

        [DataMember(IsRequired = true)]
        public DetectorProperties detector { get; set; }

        [DataMember(IsRequired = true)]
        public RayProperties ray { get; set; }

        [DataMember(IsRequired = true)]
        public SampleProperties sample { get; set; }

        #endregion
        
        public Properties(AbsorptionProperties absorptionProperties, AngleProperties angleProperties, 
            DetectorProperties detectorProperties, RayProperties rayProperties, SampleProperties sampleProperties)
        {
            absorption = absorptionProperties;
            sample = sampleProperties;
            ray = rayProperties;
            detector = detectorProperties;
            angle = angleProperties;
        }

        public Properties()
        {
            absorption = AbsorptionProperties.Initialize();
            sample = SampleProperties.Initialize();
            ray = RayProperties.Initialize();
            detector = DetectorProperties.Initialize();
            angle = AngleProperties.Initialize();
        }
    }
}