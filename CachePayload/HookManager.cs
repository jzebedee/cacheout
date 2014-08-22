using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DllExporter;
using Win32Helper;

namespace CachePayload
{
    public static class HookManager
    {
        //NTSTATUS ZwCreateFile(
        //  _Out_     PHANDLE FileHandle,
        //  _In_      ACCESS_MASK DesiredAccess,
        //  _In_      POBJECT_ATTRIBUTES ObjectAttributes,
        //  _Out_     PIO_STATUS_BLOCK IoStatusBlock,
        //  _In_opt_  PLARGE_INTEGER AllocationSize,
        //  _In_      ULONG FileAttributes,
        //  _In_      ULONG ShareAccess,
        //  _In_      ULONG CreateDisposition,
        //  _In_      ULONG CreateOptions,
        //  _In_opt_  PVOID EaBuffer,
        //  _In_      ULONG EaLength
        //);
        public delegate IntPtr dZwCreateFile(
            out IntPtr FileHandle,
            uint /*ACCESS_MASK*/ DesiredAccess,
            IntPtr /*POBJECT_ATTRIBUTES*/ ObjectAttributes,
            out IntPtr /*PIO_STATUS_BLOCK*/ IoStatusBlock,
            IntPtr /*long*/ AllocationSize,
            uint FileAttributes,
            uint ShareAccess,
            uint CreateDisposition,
            uint CreateOptions,
            IntPtr EaBuffer,
            uint EaLength);

        [DllExport]
        public static void HookIAT([MarshalAs(UnmanagedType.LPWStr)]string dummy)
        {
            var hNtDll = Imports.GetModuleHandle("ntdll");
            pZwCreateFile = Imports.GetProcAddress(hNtDll, "ZwCreateFile");

            var pDetourZwCreateFile = Marshal.GetFunctionPointerForDelegate(ZwCreateFile);

            var db = BitConverter.GetBytes((uint)pDetourZwCreateFile);
            hook = new byte[6] { 0x68, db[0], db[1], db[2], db[3], 0xC3 };

            orig = Memory.Read(pZwCreateFile, 6);

            //set the delegate to the original ZwCreateFile
            ZwCreateFile = (dZwCreateFile)Marshal.GetDelegateForFunctionPointer(pZwCreateFile, typeof(dZwCreateFile));
            Memory.Write(pZwCreateFile, hook);
        }

        public static IntPtr HookedZwCreateFile(
            out IntPtr FileHandle,
            uint /*ACCESS_MASK*/ DesiredAccess,
            IntPtr /*POBJECT_ATTRIBUTES*/ ObjectAttributes,
            out IntPtr /*PIO_STATUS_BLOCK*/ IoStatusBlock,
            IntPtr /*long*/ AllocationSize,
            uint FileAttributes,
            uint ShareAccess,
            uint CreateDisposition,
            uint CreateOptions,
            IntPtr EaBuffer,
            uint EaLength)
        {
            try
            {
                Memory.Write(pZwCreateFile, orig);
                return ZwCreateFile(out FileHandle, DesiredAccess, ObjectAttributes, out IoStatusBlock, AllocationSize, FileAttributes, ShareAccess, CreateDisposition, CreateOptions, EaBuffer, EaLength);
            }
            finally
            {
                Memory.Write(pZwCreateFile, hook);
            }
        }

        private static byte[] hook, orig;

        public static IntPtr pZwCreateFile;
        public static dZwCreateFile ZwCreateFile = HookedZwCreateFile;
    }
}
