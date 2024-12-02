using Newtonsoft.Json;

namespace VRoidShaper
{
    public class VrmBind
    {
        public int index { get; set; }
        public int mesh { get; set; }
        public float weight { get; set; }
    }

    public class VrmBlendShapeGroup
    {
        public List<VrmBind> binds { get; set; }
        public bool isBinary { get; set; }
        public List<object> materialValues { get; set; }
        public string name { get; set; }
        public string presetName { get; set; }

        public override string ToString()
        {
            return $"{name} | {presetName}";
        }
    }

    public class VrmBlendShapeMaster
    {
        public List<VrmBlendShapeGroup> blendShapeGroups { get; set; }
    }
}
