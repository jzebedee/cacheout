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
                        var loader = new ProcessLoader(targetPath);
                        loader.Launch(true);
                        Injector.Inject(loader.Process, PAYLOAD_DLL, EXPORT_NAME);
                        loader.Resume();
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
