using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Win32Helper;

namespace CacheOut
{
    class ProcessLoader
    {
        public string TargetPath { get; private set; }

        public Process Process { get; private set; }

        private PROCESS_INFORMATION _procInfo;

        public ProcessLoader(string path)
        {
            this.TargetPath = path;
        }

        public void Launch(bool suspended = false)
        {
            var supInfo = new STARTUPINFO();

            if (!Imports.CreateProcessW(TargetPath, null, IntPtr.Zero, IntPtr.Zero, false, suspended ? ProcessCreationFlags.CreateSuspended : ProcessCreationFlags.None, IntPtr.Zero, null, ref supInfo, out _procInfo))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            Process = Process.GetProcessById(_procInfo.dwProcessId);
        }

        public void Resume()
        {
            if (Process == null)
                throw new InvalidOperationException("Process must be set");

            if (Process.HasExited)
                throw new InvalidOperationException("Process must still be running");

            Imports.ResumeThread(_procInfo.hThread);
        }
    }
}
