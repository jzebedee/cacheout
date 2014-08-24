using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Molly;

namespace CachePayload.Hooks
{
    public class ZwCreateFile : IDisposable
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

        readonly Detour<dZwCreateFile> zwcfDetour;

        public ZwCreateFile(IntPtr addr)
        {
            zwcfDetour = new Detour<dZwCreateFile>(addr, HookedZwCreateFile);
            zwcfDetour.Attach();
        }
        public void Dispose()
        {
            zwcfDetour.Dispose();
        }

        IntPtr HookedZwCreateFile(
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
            bool shouldReattach = false;
            try
            {
                shouldReattach = zwcfDetour.Detach();

                return zwcfDetour.Target(out FileHandle, DesiredAccess, ObjectAttributes, out IoStatusBlock, AllocationSize, FileAttributes, ShareAccess, CreateDisposition, CreateOptions, EaBuffer, EaLength);
            }
            finally
            {
                if (shouldReattach)
                    zwcfDetour.Attach();
            }
        }
    }
}
