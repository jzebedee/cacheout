using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using CacheOut.Win32;

namespace CacheOut
{
    static class Injector
    {
        static bool Inject(Process targetProc, string payload, string export)
        {
            if (!File.Exists(payload))
                throw new FileNotFoundException("Payload '" + payload + "' does not exist");

            bool success = false;

            IntPtr
                pLibBase = IntPtr.Zero,
                hProc = IntPtr.Zero;
            try
            {
                Process.EnterDebugMode();

                hProc = Imports.OpenProcess(
                    ProcessAccessFlags.QueryInformation | ProcessAccessFlags.CreateThread |
                    ProcessAccessFlags.VMOperation | ProcessAccessFlags.VMWrite |
                    ProcessAccessFlags.VMRead, false, targetProc.Id);
                if (hProc == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                pLibBase = InjectInternal(hProc, targetProc, payload, export);
            }
            finally
            {
                if (pLibBase != IntPtr.Zero && hProc != IntPtr.Zero)
                {
                    Eject(hProc, pLibBase);
                    success = true;
                }

                Process.LeaveDebugMode();
            }

            return success;
        }

        private static IntPtr InjectInternal(IntPtr hTarget, Process targetProc, string payload, string export)
        {
            var payloadName = Path.GetFileName(payload);
            var libPathSize = (uint)Encoding.Unicode.GetByteCount(payload);

            IntPtr
                pLibPath = Marshal.StringToHGlobalUni(payload),
                pExternLibPath = IntPtr.Zero;
            try
            {
                var hKernel = Imports.GetModuleHandle("kernel32");
                var pLoadLib = Imports.GetProcAddress(hKernel, "LoadLibraryW");

                pExternLibPath = Imports.VirtualAllocEx(hTarget, IntPtr.Zero, libPathSize, AllocationType.Commit, MemoryProtection.ReadWrite);

                int bytesWritten;
                Imports.WriteProcessMemory(hTarget, pExternLibPath, pLibPath, libPathSize, out bytesWritten);

                IntPtr
                    pLibBase,
                    hMod = CRTWithWait(hTarget, pLoadLib, pExternLibPath);
                if (hMod == IntPtr.Zero)
                    pLibBase = (from ProcessModule module in targetProc.Modules
                                where module.ModuleName.Equals(payloadName)
                                select module)
                    .Single().BaseAddress;
                else
                    pLibBase = hMod;

                var oHost = FindExportRVA(payload, export).ToInt32();
                CRTWithWait(hTarget, pLibBase + oHost, pExternLibPath);

                return pLibBase;
            }
            finally
            {
                Marshal.FreeHGlobal(pLibPath);
                Imports.VirtualFreeEx(hTarget, pExternLibPath, 0, AllocationType.Release);
            }
        }

        static void Eject(IntPtr hTarget, IntPtr pLibBase)
        {
            var hKernel = Imports.GetModuleHandle("kernel32");
            var pFreeLib = Imports.GetProcAddress(hKernel, "FreeLibrary");
            CRTWithWait(hTarget, pFreeLib, pLibBase);
        }

        static IntPtr CRTWithWait(IntPtr handle, IntPtr pTarget, IntPtr pParam)
        {
            var hThread = IntPtr.Zero;
            try
            {
                hThread = Imports.CreateRemoteThread(handle, IntPtr.Zero, 0, pTarget, pParam, 0, IntPtr.Zero);
                if (Imports.WaitForSingleObject(hThread, (uint)ThreadWaitValue.Infinite) != (uint)ThreadWaitValue.Object0)
                    return IntPtr.Zero;
                //throw new Win32Exception(Marshal.GetLastWin32Error());

                IntPtr hLibModule;
                if (!Imports.GetExitCodeThread(hThread, out hLibModule))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return hLibModule;
            }
            finally
            {
                Imports.CloseHandle(hThread);
            }
        }

        static IntPtr FindExportRVA(string payload, string export)
        {
            var hModule = IntPtr.Zero;
            try
            {
                hModule = Imports.LoadLibraryEx(payload, IntPtr.Zero, LoadLibraryExFlags.DontResolveDllReferences);
                if (hModule == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var pFunc = Imports.GetProcAddress(hModule, export);
                if (pFunc == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return pFunc - hModule.ToInt32();
            }
            finally
            {
                try
                {
                    Imports.CloseHandle(hModule);
                }
                catch (SEHException)
                {
                    //expected. http://stackoverflow.com/questions/9867334/why-is-the-handling-of-exceptions-from-closehandle-different-between-net-4-and
                }
            }
        }
    }
}
