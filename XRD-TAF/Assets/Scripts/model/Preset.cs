using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
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
        
        public static Preset Deserialize(string filePath)
        {
            if (!File.Exists(filePath)) 
                throw new FileNotFoundException("Could not load preset; file not found.");
            var presetJson = File.ReadAllText(filePath, Settings.DefaultValues.Encoding);
            using (var stream = new MemoryStream(Settings.DefaultValues.Encoding.GetBytes(presetJson)))
                return (Preset) Settings.DefaultValues.PresetSerializer.ReadObject(stream);
        }

        public void Serialize(string filepath)
        {
            using (var stream = File.Open(filepath, FileMode.Create)) 
            using (var writer = JsonReaderWriterFactory
                .CreateJsonWriter(stream, Settings.DefaultValues.Encoding, true, true, "\t"))
            {
                Settings.DefaultValues.PresetSerializer.WriteObject(writer, this);
                writer.Flush();
            }
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
        
        internal string FilenameFormatter(int nrAngles)
        {
            int n, m, k;

            var mode = (int) absorption.mode;
            var res = sample.gridResolution;
            
            switch (absorption.mode)
            {
                case AbsorptionProperties.Mode.Point:
                    n = nrAngles;
                    m = 1;
                    k = 1;
                    return $"[mode={mode}] [dim=({res},{n},{m},{k})] Output.txt";
                case AbsorptionProperties.Mode.Area:
                    n = detector.resolution.x;
                    m = detector.resolution.y;
                    k = 1;
                    return $"[mode={mode}] [dim=({res},{n},{m},{k})] Output.txt";
                case AbsorptionProperties.Mode.Integrated:
                    n = nrAngles;
                    m = 1;
                    k = angle.angleCount;
                    return $"[mode={mode}] [dim=({res},{n},{m},{k})] Output.txt";
            }
            
            return "[mode=?] [dims=?] Output.txt";;
        }

        internal string OutputPreamble() => OutputPreamble(new[] {0,1,2});
        internal string OutputPreamble(IEnumerable<int> whichTargets)
        {
            var capStrings =
                "CAPILLARY:"
                + $"\n\tdiameter = {sample.totalDiameter.ToString("G", CultureInfo.InvariantCulture)}"
                + $"\n\tcell thickness = {sample.cellThickness.ToString("G", CultureInfo.InvariantCulture)}"
                + $"\n\t\u03BC (cell,sample) = ({sample.muCell.ToString("G", CultureInfo.InvariantCulture)},{sample.muSample.ToString("G", CultureInfo.InvariantCulture)})"
                + $"\n\tgrid resolution = {sample.gridResolution}";
            var absStrings =
                "ABSORPTION:"
                + $"\n\tmode = {absorption.mode.ToString()}"
                + "\n\ttargets = [" + 
                string.Join(",", whichTargets
                    .Where(i => i >= 0 && i < 3)
                    .Select(i => new[] {"(s,sc)", "(c,sc)", "(c,c)"}[i]))
                + "]";
            string angleStrings = null;
            if (absorption.mode == AbsorptionProperties.Mode.Integrated) 
                angleStrings = 
                    "RING ANGLES:"
                    + $"\n\tstart = {angle.angleStart.ToString("G", CultureInfo.InvariantCulture)}°"
                    + $"\n\tend = {angle.angleEnd.ToString("G", CultureInfo.InvariantCulture)}°"
                    + $"\n\tcount = {angle.angleCount:D}";
            string detStrings = null;
            if (absorption.mode == AbsorptionProperties.Mode.Area ||
                absorption.mode == AbsorptionProperties.Mode.Integrated)
            {
                detStrings =
                    "DETECTOR:"
                    + $"\n\tpixel count (x,y) = {detector.resolution.ToString("G", CultureInfo.InvariantCulture)}"
                    + $"\n\tpixel size (x,y) = {detector.pixelSize.ToString("G", CultureInfo.InvariantCulture)}"
                    + $"\n\tcapillary offset (x,y) = {detector.offset.ToString("G", CultureInfo.InvariantCulture)}"
                    + $"\n\tcapillary distance = {detector.distToSample.ToString("G", CultureInfo.InvariantCulture)}";
            }
            var rayStrings =
                "RAY:"
                + $"\n\tprofile = {ray.profile.ToString()}"
                + $"\n\tdimensions (x,y) = {ray.dimensions.ToString("G")}"
                + $"\n\toffset (x,y) = {ray.offset.ToString("G")}";

            var conditionalStrings = 
                string.Join(angleStrings == null || detStrings == null ? "" : "\n", detStrings, angleStrings);
            var preamble = string.Join("\n", capStrings, absStrings, rayStrings, conditionalStrings);
            var width = preamble.Split('\n').Select(l => l.Length).Max();

            return preamble + "\n" + new string('-', width) + "\n";
        }
    }
}