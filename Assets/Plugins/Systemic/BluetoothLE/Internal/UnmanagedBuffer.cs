using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Systemic.Unity.BluetoothLE.Internal
{
    /// <summary>
    /// Helper class for marshaling .NET arrays to and from unmanaged memory.
    /// </summary>
    internal static class UnmanagedBuffer
    {
        public static (IntPtr, int) AllocUnmanagedBuffer(byte[] data)
        {
            int length = data?.Length ?? 0;
            IntPtr ptr = IntPtr.Zero;
            if (length > 0)
            {
                ptr = Marshal.AllocHGlobal(length);
                if (ptr == IntPtr.Zero)
                {
                    throw new InvalidOperationException($"Failed to allocate {length} unmanaged bytes");
                }
                Marshal.Copy(data, 0, ptr, length);
            }
            return (ptr, length);
        }

        public static void FreeUnmanagedBuffer(IntPtr ptr)
        {
            Marshal.FreeHGlobal(ptr);
        }

        public static byte[] ToArray(IntPtr data, UIntPtr length)
        {
            byte[] array = null;
            if (data != IntPtr.Zero)
            {
                array = new byte[(int)length];
                Marshal.Copy(data, array, 0, array.Length);
            }
            return array;
        }
    }
}
