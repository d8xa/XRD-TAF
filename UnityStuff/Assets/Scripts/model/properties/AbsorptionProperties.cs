using UnityEngine;

namespace model.properties
{
    [System.Serializable]
    public class AbsorptionProperties {
        public Mode mode;
        public AbsorptionTarget absorptionTarget;

        public enum AbsorptionTarget {
            All, Cell, Sample, CellAndSample
        }
        
        [System.Serializable]
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