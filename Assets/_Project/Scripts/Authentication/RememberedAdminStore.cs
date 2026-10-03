using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace MeraBrand.Expo.Authentication
{
    internal static class RememberedAdminStore
    {
        public static bool Supported
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return true;
#else
                return false;
#endif
            }
        }

        private static string Target => "MeraBrand/SupabaseAdmin/" + Application.identifier;

        public static bool TryRead(out string email, out string token)
        {
            email = token = string.Empty;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!CredRead(Target, 1, 0, out IntPtr pointer)) return false;
            try
            {
                Credential credential = Marshal.PtrToStructure<Credential>(pointer);
                if (credential.blobSize == 0 || credential.blobSize > 2560 || credential.blob == IntPtr.Zero)
                    return false;
                byte[] bytes = new byte[credential.blobSize];
                Marshal.Copy(credential.blob, bytes, 0, bytes.Length);
                email = credential.username ?? string.Empty;
                token = Encoding.UTF8.GetString(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                return !string.IsNullOrEmpty(token);
            }
            finally { CredFree(pointer); }
#else
            return false;
#endif
        }

        public static bool Save(string email, string token)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            byte[] bytes = Encoding.UTF8.GetBytes(token);
            if (bytes.Length == 0 || bytes.Length > 2560) return false;
            IntPtr buffer = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                var credential = new Credential
                {
                    type = 1, target = Target, username = email,
                    blobSize = (uint)bytes.Length, blob = buffer, persist = 2
                };
                return CredWrite(ref credential, 0);
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                Marshal.FreeHGlobal(buffer);
            }
#else
            return false;
#endif
        }

        public static void Delete()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            CredDelete(Target, 1, 0);
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public uint flags, type;
            public string target, comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME lastWritten;
            public uint blobSize;
            public IntPtr blob;
            public uint persist, attributeCount;
            public IntPtr attributes;
            public string targetAlias, username;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite(ref Credential credential, uint flags);
        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredDelete(string target, uint type, uint flags);
        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr credential);
#endif
    }
}
