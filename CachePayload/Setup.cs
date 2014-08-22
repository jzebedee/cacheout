using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DllExporter;

namespace CachePayload
{
    public static class Setup
    {
        public static string BasePath { get; private set; }

        static Setup()
        {
            AppDomain.CurrentDomain.AssemblyResolve += PathedAssemblyResolve;
        }

#if DEBUG
        [DllImport("kernel32", SetLastError = true)]
        static extern bool Beep(uint dwFreq, uint dwDuration);

        [DllExport]
        public static void BeepTest([MarshalAs(UnmanagedType.LPWStr)]string dummy)
        {
            for (uint i = 0x25; i < 0x100; i++)
            // for (uint i = 0x25; i < 0x7FFF; i++)
            {
                Beep(i, 50);
            }
        }

        [DllExport]
        public static void AttachDebugger([MarshalAs(UnmanagedType.LPWStr)]string dummy)
        {
            System.Diagnostics.Debugger.Launch();
        }
#endif

        [DllExport]
        public static void SetPath([MarshalAs(UnmanagedType.LPWStr)]string path)
        {
            BasePath = path;
        }

        static Assembly PathedAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = Path.ChangeExtension(args.Name.Substring(0, args.Name.IndexOf(',')), "dll");

            var basedPath = Path.Combine(BasePath, name);
            if (!File.Exists(basedPath))
                return null;

            return Assembly.LoadFrom(basedPath);
        }
    }
}
