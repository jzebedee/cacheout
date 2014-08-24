using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Molly
{
    public class Detour<T> : IMemoryEdit where T : class
    {
        static Detour()
        {
            if (!typeof(T).IsSubclassOf(typeof(Delegate)))
                throw new InvalidOperationException(typeof(T).Name + " is not a delegate");
        }

        readonly IntPtr _ptrOriginal;
        readonly byte[] _origCall, _detourCall;

        volatile bool _attached;

        public T Target { get; private set; }

        public Detour(IntPtr oldAddress, IntPtr newAddress)
        {
            _ptrOriginal = oldAddress;

            var db = BitConverter.GetBytes((uint)newAddress);

            _detourCall = new byte[6] { 0x68 /*push imm*/, db[0], db[1], db[2], db[3], 0xC3 /*ret*/ };
            _origCall = Memory.Read(_ptrOriginal, 6);

            Target = Marshal.GetDelegateForFunctionPointer(_ptrOriginal, typeof(T)) as T;
        }
        public Detour(IntPtr oldAddress, T detourMethod)
            : this(oldAddress, Marshal.GetFunctionPointerForDelegate<T>(detourMethod))
        {
        }
        ~Detour()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            Detach();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool Attach()
        {
            if (_attached)
                return false;

            Memory.Write(_ptrOriginal, _detourCall);
            _attached = true;

            return true;
        }
        public bool Detach()
        {
            if (!_attached)
                return false;

            Memory.Write(_ptrOriginal, _origCall);
            _attached = false;

            return true;
        }
    }
}
