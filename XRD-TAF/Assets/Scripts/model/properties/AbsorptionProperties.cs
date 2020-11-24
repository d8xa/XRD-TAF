using System.Runtime.Serialization;
using UnityEngine;

namespace model.properties
{
    [DataContract]
    public class AbsorptionProperties {
        [DataMember] public Mode mode;
        public AbsorptionTarget absorptionTarget;
        
        public static AbsorptionProperties Initialize()
        {
            return new AbsorptionProperties {mode = Mode.Point, absorptionTarget = AbsorptionTarget.All};
        }

        public enum AbsorptionTarget {
            All, Cell, Sample, CellAndSample
        }
        
        [DataContract]
        public enum Mode {
            Point = 0, 
            Area = 1, 
            Integrated = 2, 
            Testing = 3, 
            Undefined = 4
        }
        
        public override string ToString() => JsonUtility.ToJson(this);
    }
}