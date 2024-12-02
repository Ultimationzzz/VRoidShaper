namespace VRoidShaper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("VRoid BlendShape Merger by Ultimation");


            if (args.Length == 0)
            {
                Console.WriteLine("Usage: VRoidShaper.exe <input vrm file> <optional reference vrm>");
                return;
            }

            var referenceVrm = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reference.vrm");
            if (args.Length > 1)
            {
                referenceVrm = args[1];
            } 
            if (!File.Exists(referenceVrm))
            {
                Console.WriteLine("Invalid reference vrm file");
                return;
            }
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Invalid model vrm file");
                return;
            }

            using (var modelFactory = new VrmModel(referenceVrm, args[0]))
            {
                if (!modelFactory.Prepare())
                {
                    Console.WriteLine("Please check your vrm model file and input reference file.");
                    return;
                }

                if (!modelFactory.IsValid())
                {
                    Console.WriteLine("The reference model and your vrm contain a different amount of vertices. Make sure you have unchecked 'Delete Transparent Meshes' in VRoid.");
                    return;
                }

                Console.WriteLine("Would you like to add ARKit Blendshapes?");
                if (Console.ReadKey(false).Key == ConsoleKey.Y)
                {
                    modelFactory.AddBlendShapes();
                }
                Console.WriteLine("Would you like to add Blendshape Clip proxies?");
                if (Console.ReadKey(false).Key == ConsoleKey.Y)
                {
                    modelFactory.AddShapeProxies();
                }


                var outDir = Path.GetDirectoryName(args[0]);
                var outFile = Path.Combine(outDir, Path.GetFileNameWithoutExtension(args[0]) + "_ARKit.vrm");
                modelFactory.Save(outFile);
                Console.WriteLine($"BlendShapes added successfully, created {Path.GetFileName(outFile)}");
            }
        }
    }
}
