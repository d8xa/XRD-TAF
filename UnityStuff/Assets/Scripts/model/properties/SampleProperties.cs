using UnityEngine;

namespace model.properties
{
    [System.Serializable]
    public class SampleProperties {
        public float totalDiameter;
        public float cellThickness;
        public float muSample;
        public float muCell;
        public int gridResolution;

        private float probeDiameterNormalized;
        private float cellThicknessNormalized;
        private float totalDiameterNormalized;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
}