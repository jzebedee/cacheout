using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CacheOut
{
    class Program
    {
        const string
            PAYLOAD_DLL = "CachePayload.dll",
            EXPORT_NAME = "HookIAT";

        static void Main(string[] args)
        {
            if (args.Length == 0)
                Console.WriteLine("usage: cacheout.exe <path>");
            else
            {
                var targetPath = Path.GetFullPath(args[0]);
                if (!File.Exists(targetPath))
                    Console.WriteLine("File {0} does not exist.", targetPath);
                else
                {
                    if (Path.GetExtension(targetPath).ToUpperInvariant() != ".EXE")
                        Console.WriteLine("File {0} is not an executable.", targetPath);
                    else
                    {
                        var targetName = Path.GetFileName(targetPath);
                        Console.WriteLine("Loading {0}", targetName);

                        var loader = new ProcessLoader(targetPath);

                        Console.WriteLine("Launching {0} (suspended)", targetName);
                        loader.Launch(true);

                        Console.WriteLine("Injecting {0}", PAYLOAD_DLL);
                        using (var inj = new Injector(loader.Process))
                        {
                            inj.Inject(PAYLOAD_DLL);
                            Console.WriteLine("Injected");

                            //CallExport(inj, "AttachDebugger");
                            CallExport(inj, "SetPath", AppDomain.CurrentDomain.BaseDirectory);
                            CallExport(inj, EXPORT_NAME);

                            Console.WriteLine("Press any key to resume process");
                            Console.ReadKey();
                            loader.Resume();

                            Console.ReadKey();
                            var tmp = Path.GetTempFileName();
                            CallExport(inj, "Dump", tmp);

                            using (var fstream = File.OpenRead(tmp))
                            {
                                var bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                                var x = bf.Deserialize(fstream) as System.Collections.Concurrent.ConcurrentQueue<System.Collections.ArrayList>;
                                Console.WriteLine(x);
                            }

                            Console.WriteLine("Press any key to eject");
                            Console.ReadKey();
                        }
                    }
                }
            }
        }

        private static void CallExport(Injector inj, string t, string arg = "")
        {
            Console.WriteLine("Calling export {0}", t);
            var exitCode = inj.Call(PAYLOAD_DLL, t, arg);
            Console.WriteLine("Done. Thread exit code is {0:x8}", exitCode);

            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
        }
    }
}
