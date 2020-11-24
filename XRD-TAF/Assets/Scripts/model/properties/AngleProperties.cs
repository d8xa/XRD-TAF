using System.Runtime.Serialization;

namespace model.properties
{
    [DataContract]
    public class AngleProperties
    {
        [DataMember] public string pathToAngleFile;
        [DataMember] public float angleStart;
        [DataMember] public float angleEnd;
        [DataMember] public int angleCount;

        public static AngleProperties Initialize()
        {
            return new AngleProperties();
        }
    }
}