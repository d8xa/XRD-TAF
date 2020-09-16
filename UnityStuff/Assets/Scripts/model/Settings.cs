using UnityEngine;
using UnityEngine.Serialization;

namespace model
{
    [System.Serializable]
    public class Settings {
        // TODO: add field for comments maybe

        public string saveName;
        public Model.Mode mode;
        public Model.AbsorptionType absType;
        public string pathToInputData;
        public float gridResolution;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}
