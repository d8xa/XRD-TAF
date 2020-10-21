using System.Runtime.Serialization;
using UnityEngine;

namespace model.properties
{
    [DataContract]
    public class RayProperties {
        [DataMember] public Profile profile;
        public Vector2 dimensions;    // TODO: support
        public float intensity;
        
        /// <summary> The horizontal and vertical offset from the center of the capillary. </summary>
        [DataMember] public Vector2 offset;
        
        public enum Profile {
            Rectangle, Oval
        }

        public static RayProperties Initialize()
        {
            return new RayProperties
            {
                profile = Profile.Rectangle,
                offset = Vector2.zero,
            };
        }
    }
    
    
}