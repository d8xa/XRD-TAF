using UnityEngine;

namespace model
{
    [System.Serializable]
    public class SampleSettings {
        public float totalDiameter;
        public float cellThickness;
        public float muSample;
        public float muCell;

        private float probeDiameterNormalized;
        private float cellThicknessNormalized;
        private float totalDiameterNormalized;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}