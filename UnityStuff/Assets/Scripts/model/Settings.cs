using UnityEngine.Serialization;

namespace model
{
    [System.Serializable]
    public class Settings {
        //TODO: add field for comments maybe

        [FormerlySerializedAs("aufbauBezeichnung")] 
        public string saveName;
        public Model.Mode mode;
        public Model.AbsorptionType absType;
        public string loadName;
        public string pathToInputData;
        [FormerlySerializedAs("computingAccuracy")] 
        public float gridResolution;
    }
}
