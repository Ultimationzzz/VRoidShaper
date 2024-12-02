using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Newtonsoft.Json;
using SharpGLTF.IO;
using SharpGLTF.Schema2;

namespace VRoidShaper;


public class VrmNode:JsonSerializable
{
    public Dictionary<string, string> Properties { get; set; } = [];

    public Dictionary<string, JsonNode> Props { get; set; } = new Dictionary<string, JsonNode>();
    public string ExporterVersion { get; set; }
    public string SpecVersion { get; set; }
    public VrmNode(ModelRoot node)
    {

    }
    public void SetBlendShapeMaster(VrmBlendShapeMaster shapes)
    {
        var res = JsonNode.Parse(JsonConvert.SerializeObject(shapes, Formatting.None));
        Props["blendShapeMaster"] = res;
    }
    public VrmBlendShapeMaster GetBlendShapeList()
    {
        using (var strm = new MemoryStream())
        {
            using (var wtr = new Utf8JsonWriter(strm))
            {
                Props["blendShapeMaster"].WriteTo(wtr);
                wtr.Flush();
                strm.Position = 0;
                var raw = Encoding.UTF8.GetString(strm.ToArray());
                return JsonConvert.DeserializeObject<VrmBlendShapeMaster>(raw);
            }
        }
    }

   
    private string GetRawValue(JsonNode node)
    {
        return JsonConvert.SerializeObject(node, Formatting.None, new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        foreach (var p in Props)
        {
            writer.WritePropertyName(p.Key);
            p.Value.WriteTo(writer);
        }
    }

    protected override void DeserializeProperty(string jsonPropertyName, ref Utf8JsonReader reader)
    {
        reader.Read();
        Props[jsonPropertyName] = JsonNode.Parse(ref reader);
    }
}