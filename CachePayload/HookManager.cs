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
        [DllExport]
        public static void HookIAT(IntPtr pDetour)
        {

        }
    }
}
