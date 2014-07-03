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
    class Injector : IDisposable
    {
        private readonly Process _targetProc;
        private readonly Dictionary<string, IntPtr> _injectedModules = new Dictionary<string, IntPtr>();

        private IntPtr hProc;

        public Injector(Process targetProc)
        {
            this._targetProc = targetProc;
        }
        ~Injector()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (hProc != IntPtr.Zero)
            {
                foreach (var kvp in _injectedModules)
                {
                    var hModule = kvp.Value;
                    EjectInternal(hProc, hModule);
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool Inject(string payload)
        {
            if (!File.Exists(payload))
                throw new FileNotFoundException("Payload '" + payload + "' does not exist");

            var moduleName = Path.GetFileName(payload);
            if (_injectedModules.ContainsKey(moduleName))
                throw new InvalidOperationException("The same payload cannot be injected again without first ejecting");

            bool success = false;

            IntPtr hModule = IntPtr.Zero;
            try
            {
                Process.EnterDebugMode();

                if (hProc == IntPtr.Zero)
                    hProc = Imports.OpenProcess(
                        ProcessAccessFlags.QueryInformation | ProcessAccessFlags.CreateThread |
                        ProcessAccessFlags.VMOperation | ProcessAccessFlags.VMWrite |
                        ProcessAccessFlags.VMRead, false, _targetProc.Id);

                if (hProc == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                payload = Path.GetFullPath(payload);
                hModule = InjectInternal(hProc, _targetProc, payload);
            }
            finally
            {
                if (hModule != IntPtr.Zero)
                {
                    _injectedModules.Add(moduleName, hModule);
                    success = true;
                }

                Process.LeaveDebugMode();
            }

            return success;
        }

        public void Eject(string moduleName)
        {
            IntPtr hModule;
            if (!_injectedModules.TryGetValue(moduleName, out hModule))
                throw new InvalidOperationException("A module with this name was not injected");

            EjectInternal(hProc, hModule);
            _injectedModules.Remove(moduleName);
        }

        public IntPtr Call(string moduleName, string export, string param)
        {
            IntPtr hModule;
            if (!_injectedModules.TryGetValue(moduleName, out hModule))
                throw new InvalidOperationException("A module with this name was not injected");

            IntPtr pExternParam = IntPtr.Zero;
            try
            {
                pExternParam = WriteExternalString(hProc, param);

                var oHost = FindExportRVA(moduleName, export);
                return CRTWithWait(hProc, hModule + (int)oHost, pExternParam);
            }
            finally
            {
                if (pExternParam != IntPtr.Zero)
                    FreeExternal(hProc, pExternParam);
            }
        }

        private static IntPtr WriteExternalString(IntPtr hProc, string str)
        {
            var strSize = (uint)Encoding.Unicode.GetByteCount(str);

            IntPtr
                pStr = IntPtr.Zero,
                pExternStr = IntPtr.Zero;

            try
            {
                pStr = Marshal.StringToHGlobalUni(str);

                pExternStr = Imports.VirtualAllocEx(hProc, IntPtr.Zero, strSize, AllocationType.Commit, MemoryProtection.ReadWrite);

                int bytesWritten;
                Imports.WriteProcessMemory(hProc, pExternStr, pStr, strSize, out bytesWritten);

                Debug.Assert(bytesWritten == strSize);
                return pExternStr;
            }
            finally
            {
                if (pStr != IntPtr.Zero)
                    Marshal.FreeHGlobal(pStr);
            }
        }

        private static void FreeExternal(IntPtr hProc, IntPtr pFree)
        {
            if (pFree == IntPtr.Zero)
                throw new ArgumentException("pFree cannot be zero");

            Imports.VirtualFreeEx(hProc, pFree, 0, AllocationType.Release);
        }

        private static IntPtr InjectInternal(IntPtr hProc, Process targetProc, string payload)
        {
            var pExternPayloadStr = IntPtr.Zero;
            try
            {
                pExternPayloadStr = WriteExternalString(hProc, payload);

                var hKernel = Imports.GetModuleHandle("kernel32");
                var pLoadLib = Imports.GetProcAddress(hKernel, "LoadLibraryW");

                //var hNtDll = Imports.GetModuleHandle("ntdll");
                //var pZwCreateFile = Imports.GetProcAddress(hFsck, "ZwCreateFile");

                return CRTWithWait(hProc, pLoadLib, pExternPayloadStr);
            }
            finally
            {
                if (pExternPayloadStr != IntPtr.Zero)
                    FreeExternal(hProc, pExternPayloadStr);
            }
        }

        private static void EjectInternal(IntPtr hProc, IntPtr hModule)
        {
            var hKernel = Imports.GetModuleHandle("kernel32");
            var pFreeLib = Imports.GetProcAddress(hKernel, "FreeLibrary");
            CRTWithWait(hProc, pFreeLib, hModule);
        }

        private static IntPtr CRTWithWait(IntPtr hProc, IntPtr pTarget, IntPtr pParam)
        {
            var hNewThread = IntPtr.Zero;
            try
            {
                hNewThread = Imports.CreateRemoteThread(hProc, IntPtr.Zero, 0, pTarget, pParam, 0, IntPtr.Zero);
                if (Imports.WaitForSingleObject(hNewThread, (uint)ThreadWaitValue.Infinite) != (uint)ThreadWaitValue.Object0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                IntPtr exitCode;
                if (!Imports.GetExitCodeThread(hNewThread, out exitCode))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return exitCode;
            }
            finally
            {
                Imports.CloseHandle(hNewThread);
            }
        }

        private static IntPtr FindExportRVA(string payload, string export)
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

                return IntPtr.Subtract(pFunc, (int)hModule);
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
