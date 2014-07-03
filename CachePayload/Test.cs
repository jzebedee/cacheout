using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DllExporter;

namespace CachePayload
{
    public static class Test
    {
        [DllImport("kernel32", SetLastError = true)]
        static extern bool Beep(uint dwFreq, uint dwDuration);

        [DllExport]
        public static void Test([MarshalAs(UnmanagedType.LPWStr)]string dummy)
        {
            for (uint i = 0x25; i < 0x50; i++)
            // for (uint i = 0x25; i < 0x7FFF; i++)
            {
                Beep(i, 50);
            }
        }
    }
}
