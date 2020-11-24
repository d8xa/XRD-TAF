using System.Runtime.Serialization;
using UnityEngine;

namespace model.properties
{
    [DataContract]
    public class SampleProperties {
        [DataMember] public float totalDiameter;
        [DataMember] public float cellThickness;
        [DataMember] public float muSample;
        [DataMember] public float muCell;
        [DataMember] public int gridResolution;

        public static SampleProperties Initialize()
        {
            return new SampleProperties();
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}