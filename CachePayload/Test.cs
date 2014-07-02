using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DllExporter;

namespace CachePayload
{
    public static class HookManager
    {
        [DllImport("kernel32", SetLastError = true)]
        static extern bool Beep(uint dwFreq, uint dwDuration);

        [DllExport]
        public static void HookIAT([MarshalAs(UnmanagedType.LPWStr)]string path)
        {
            for (uint i = 0x25; i < 0x7FFF; i++)
            {
                Beep(i, 50);
            }
        }
    }
}
