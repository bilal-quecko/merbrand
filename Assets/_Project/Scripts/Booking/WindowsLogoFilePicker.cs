#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;

namespace MeraBrand.Expo.Booking
{
    internal static class WindowsLogoFilePicker
    {
        // The Unicode OPENFILENAME structure includes the reserved fields used by modern Windows.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OpenFileName
        {
            public int structSize;
            public IntPtr owner;
            public IntPtr instance;
            public string filter;
            public IntPtr customFilter;
            public int maxCustomFilter;
            public int filterIndex;
            public IntPtr file;
            public int maxFile;
            public IntPtr fileTitle;
            public int maxFileTitle;
            public string initialDirectory;
            public string title;
            public int flags;
            public ushort fileOffset;
            public ushort extensionOffset;
            public string defaultExtension;
            public IntPtr customData;
            public IntPtr hook;
            public string templateName;
            public IntPtr reserved;
            public int reservedValue;
            public int flagsEx;
        }

        [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileName(ref OpenFileName dialog);

        [DllImport("comdlg32.dll")]
        private static extern uint CommDlgExtendedError();

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        public static string Open(string initialDirectory)
        {
            const int capacity = 32768;
            IntPtr buffer = Marshal.AllocHGlobal(capacity * sizeof(char));
            try
            {
                Marshal.WriteInt16(buffer, 0);
                var dialog = new OpenFileName
                {
                    structSize = Marshal.SizeOf(typeof(OpenFileName)),
                    owner = GetActiveWindow(),
                    filter = "Logo images (*.png;*.jpg;*.jpeg)\0*.png;*.jpg;*.jpeg\0\0",
                    filterIndex = 1,
                    file = buffer,
                    maxFile = capacity,
                    initialDirectory = initialDirectory,
                    title = "Select Exhibitor Logo",
                    // Explorer dialog, existing file/path, and no process working-directory changes.
                    flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008
                };
                if (GetOpenFileName(ref dialog))
                    return Marshal.PtrToStringUni(buffer);

                uint error = CommDlgExtendedError();
                if (error != 0)
                    throw new InvalidOperationException($"Windows file picker failed (0x{error:X}).");
                return string.Empty;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }
}
#endif
