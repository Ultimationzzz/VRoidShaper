using SharpGLTF.Schema2;

namespace VRoidShaper;

public class BlendShape
{
    public MeshPrimitive Primitive { get; set; }
    public int PrimitiveIdx { get; set; }
    public int ShapeIdx { get; set; }
    public string ShapeName { get; set; }
}