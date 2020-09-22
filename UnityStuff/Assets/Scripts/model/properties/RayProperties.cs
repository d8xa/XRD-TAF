using UnityEngine;

namespace model.properties
{
    public class RayProperties {
        public Profile profile;
        public Vector2 dimensions;
        public float intensity;
        public Vector2 offsetFromProbeCenter;
    }
    
    public enum Profile {
        Oval, Rectangle
    }
}