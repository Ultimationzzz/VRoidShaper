using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;


namespace VRoidShaper
{
    public class VrmModel(string reference, string model) : IDisposable
    {
        public string ReferenceFile { get; set; } = reference;
        public string ModelFile { get; set; } = model;

        public ModelRoot Reference { get; set; }
        public ModelRoot Model { get; set; }
        public bool Prepare()
        {
            if (File.Exists(ReferenceFile) && File.Exists(ModelFile))
            {
                try
                {
                    Reference=ModelRoot.Load(ReferenceFile);
                    Model = ModelRoot.Load(ModelFile);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return true;
        }
        static bool HasShape(string name, MeshPrimitive p)
        {
            if (p.Extras == null || p.Extras["targetNames"] == null)
                return false;
            var names = p.Extras["targetNames"].AsArray().Select(x => x.GetValue<string>());
            return names.Contains(name);
        }

        static List<BlendShape> GetBlendShapes(Mesh face)
        {
            var res = new List<BlendShape>();
            for (int i2 = 0; i2 < face.Primitives.Count; i2++)
            {
                var p = face.Primitives[i2];
                if (p.Extras?["targetNames"] != null)
                {
                    var names = p.Extras["targetNames"].AsArray().Select(x => x.GetValue<string>()).ToList();
                    for (int i = 0; i < p.MorphTargetsCount; i++)
                    {
                        res.Add(new BlendShape()
                        {
                            Primitive = p,
                            PrimitiveIdx = i2,
                            ShapeIdx = i,
                            ShapeName = names[i]
                        });
                    }
                }
            }

            return res;
        }

        public bool IsValid()
        {
            if (Model == null || Reference == null)
                return false;
            var face1 = Model.LogicalMeshes.FirstOrDefault(x => x.Name == "Face");
            var face2 = Reference.LogicalMeshes.FirstOrDefault(x => x.Name == "Face");
            if (face1 == null || face2 == null) return false;
            if (face1.Primitives.Count != face2.Primitives.Count) return false;
            for (int i = 0; i < face2.Primitives.Count; i++)
            {
                if (!face1.Primitives[i].VertexAccessors.ContainsKey("POSITION"))
                    return false;
                if (!face2.Primitives[i].VertexAccessors.ContainsKey("POSITION"))
                    return false;
                var count = face1.Primitives[i].VertexAccessors["POSITION"].Count;
                if (count != face2.Primitives[i].VertexAccessors["POSITION"].Count)
                {
                    return false;
                }
            }

            return true;
        }
        public void AddBlendShapes()
        {
            var modelFace = Model.LogicalMeshes.First(x => x.Name == "Face");
            var refFace = Reference.LogicalMeshes.First(x => x.Name == "Face");

            var blendShapes = GetBlendShapes(refFace);
            foreach (var bs in blendShapes)
            {
                if (!HasShape(bs.ShapeName, modelFace.Primitives[bs.PrimitiveIdx]))
                {
                    var newIdx = modelFace.Primitives[bs.PrimitiveIdx].MorphTargetsCount;
                    var morph = bs.Primitive.GetMorphTargetAccessors(bs.ShapeIdx);
                    if(morph == null) continue;
                    var newMorphs = new Dictionary<string, Accessor>();
                    foreach (var d in morph)
                    {
                        if (d.Value.Format.Dimensions != DimensionType.VEC3)
                            continue;
                        var info = new MemoryAccessInfo(d.Key, 0, d.Value.Count,
                            0, d.Value.Dimensions);
                        MemoryAccessor mem = new MemoryAccessor(info);
                        mem.Update(d.Value.SourceBufferView.Content, info);
                        var newAccessor = Model.CreateAccessor(d.Key);
                        newAccessor.SetVertexData(mem);
                        newMorphs.Add(d.Key, newAccessor);
                    }
                    modelFace.Primitives[bs.PrimitiveIdx].SetMorphTargetAccessors(newIdx, newMorphs);
                    modelFace.Primitives[bs.PrimitiveIdx]?.Extras["targetNames"]?.AsArray().Add(bs.ShapeName);
                    foreach (var n in Model.LogicalMeshes)
                    {
                        n.Extras?["targetNames"]?.AsArray().Add(bs.ShapeName);
                    }
                    Console.WriteLine($"Adding BlendShape: {bs.ShapeName}");
                }
            }

        }
        

        public void Dispose()
        {
        }

        public void Save(string outFile)
        {
            Model.SaveGLB(outFile, new WriteSettings()
            {
                ImageWriting = ResourceWriteMode.Default,
                MergeBuffers = false,
            });
        }
    }
}
