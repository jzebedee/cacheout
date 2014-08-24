using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Win32Helper;

namespace Molly
{
    public static class Memory
    {
        public unsafe static void Write(IntPtr addr, byte[] buffer)
        {
            MemoryProtection oldProtect;
            Imports.VirtualProtect(addr, buffer.Length, MemoryProtection.ExecuteReadWrite, out oldProtect);

            byte* pBuf = (byte*)addr.ToPointer();
            for (int i = 0; i < buffer.Length; i++)
                pBuf[i] = buffer[i];

            Imports.VirtualProtect(addr, buffer.Length, oldProtect, out oldProtect);
        }

        public unsafe static byte[] Read(IntPtr addr, int count)
        {
            var buf = new byte[count];

            byte* pBuf = (byte*)addr.ToPointer();
            for (int i = 0; i < count; i++)
                buf[i] = pBuf[i];

            return buf;
        }
    }
}
