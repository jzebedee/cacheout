using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CachePayload.Hooks;
using DllExporter;
using Molly;
using Win32Helper;

namespace CachePayload
{
    public static class HookManager
    {
        static Queue<IDisposable> _hooks = new Queue<IDisposable>();

        [DllExport]
        public static void HookFilesystem([MarshalAs(UnmanagedType.LPWStr)]string dummy)
        {
            var hNtDll = Imports.GetModuleHandle("ntdll");

            var zwcf = new ZwCreateFile(Imports.GetProcAddress(hNtDll, "ZwCreateFile"));
            _hooks.Enqueue(zwcf);
        }

        public static void Stop()
        {
            IDisposable hook;
            while ((hook = _hooks.Dequeue()) != null)
                hook.Dispose();
        }
    }
}
