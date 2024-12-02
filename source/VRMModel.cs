using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json;
using SharpGLTF.IO;
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
                    ExtensionsFactory.RegisterExtension<ModelRoot,VrmNode>("VRM",new Func<ModelRoot,VrmNode>(root => new VrmNode(root)));
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

        public void AddShapeProxies()
        {
            var modelJson = Model.Extensions.OfType<VrmNode>().FirstOrDefault();
            if (modelJson != null)
            {
                var shapes = modelJson.GetBlendShapeList();


                var mesh = Model.LogicalMeshes.FirstOrDefault(x => x.Name.Contains("Face"));
                if (mesh != null)
                {
                    var names = mesh.Primitives[0].Extras["targetNames"].AsArray().Select(x => x.GetValue<string>())
                        .ToArray();
                    for (int shapeIdx = 0; shapeIdx < names.Length; shapeIdx++)
                    {
                        if (names[shapeIdx].StartsWith("--"))
                            continue;
                        var shapeName = names[shapeIdx];


                        var group = new VrmBlendShapeGroup()
                        {
                            name = shapeName,
                            binds = new List<VrmBind>(),
                            materialValues = new List<object>(),
                            presetName = shapeName
                        };
                        group.binds.Add(new VrmBind()
                        {
                            mesh = mesh.LogicalIndex,
                            index = shapeIdx,
                            weight = 100.0f
                        });
                        shapes.blendShapeGroups.Add(group);
                    }
                }
                modelJson.SetBlendShapeMaster(shapes);
            }
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
                        if (names[i].StartsWith("--"))
                            continue;

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
            var face1 = Model.LogicalNodes.FirstOrDefault(x => x.Name == "Face");
            var face2 = Reference.LogicalNodes.FirstOrDefault(x => x.Name == "Face");
            if (face1 == null || face2 == null) return false;
            var mesh1 = face1.Mesh;
            var mesh2 = face2.Mesh;
            if (mesh1.Primitives.Count != mesh2.Primitives.Count) return false;
            for (int i = 0; i < mesh2.Primitives.Count; i++)
            {
                if (!mesh1.Primitives[i].VertexAccessors.ContainsKey("POSITION"))
                    return false;
                if (!mesh2.Primitives[i].VertexAccessors.ContainsKey("POSITION"))
                    return false;
                var count = mesh1.Primitives[i].VertexAccessors["POSITION"].Count;
                if (count != mesh2.Primitives[i].VertexAccessors["POSITION"].Count)
                {
                    return false;
                }
            }

            return true;
        }
        public void AddBlendShapes()
        {
            var modelFace = Model.LogicalNodes.First(x => x.Name == "Face");
            var refFace = Reference.LogicalNodes.First(x => x.Name == "Face");

            var blendShapes = GetBlendShapes(refFace.Mesh);
            foreach (var bs in blendShapes)
            {
                if (!HasShape(bs.ShapeName, modelFace.Mesh.Primitives[bs.PrimitiveIdx]))
                {
                    var newIdx = modelFace.Mesh.Primitives[bs.PrimitiveIdx].MorphTargetsCount;
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
                    modelFace.Mesh.Primitives[bs.PrimitiveIdx].SetMorphTargetAccessors(newIdx, newMorphs);
                    modelFace.Mesh.Primitives[bs.PrimitiveIdx]?.Extras["targetNames"]?.AsArray().Add(bs.ShapeName);
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
