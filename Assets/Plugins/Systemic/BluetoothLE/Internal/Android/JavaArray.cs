using System;
using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal.Android
{
    /// <summary>
    /// Helper class for marshaling .NET arrays to and from Java arrays.
    /// </summary>
    internal static class JavaUtils
    {
        public static sbyte[] ToSignedArray(byte[] data)
        {
            sbyte[] signedArray = null;
            if (data != null)
            {
                signedArray = new sbyte[data.Length];
                Buffer.BlockCopy(data, 0, signedArray, 0, data.Length);
            }
            return signedArray;
        }

        public static byte[] ToDotNetArray(AndroidJavaObject javaArray)
        {
            byte[] data = null;
            var rawArray = javaArray?.GetRawObject();
            if (rawArray != IntPtr.Zero)
            {
                var signedArray = AndroidJNI.FromSByteArray(rawArray.Value);
                data = new byte[signedArray.Length];
                Buffer.BlockCopy(signedArray, 0, data, 0, signedArray.Length);
            }
            return data;
        }
    }
}
