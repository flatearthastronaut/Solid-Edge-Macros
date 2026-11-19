using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

[assembly: InternalsVisibleTo("DocumentOpenTests")]

namespace SketchToModels
{
    /// <summary>
    /// Compare complete Windows file identities without assuming that Solid Edge
    /// returns the same path spelling passed to Documents.Open. A mapped drive,
    /// UNC share or short filename can describe the same saved part. Never use
    /// just the filename: another folder may contain a different part of that name.
    /// </summary>
    public static class DocumentPaths
    {
        public static bool SameFile(string expected, string actual)
        {
            return SameFile(expected, actual, UniversalName, FinalName);
        }

        // Resolver injection lets regression tests cover network mappings without
        // installing a drive mapping or depending on a particular company server.
        // Production always uses the native Windows resolvers below.
        internal static bool SameFile(string expected, string actual,
            Func<string, string> universalName, Func<string, string> finalName)
        {
            string left = Normalize(expected), right = Normalize(actual);
            if (left == null || right == null) return false;
            if (Equal(left, right)) return true; // Normal case: no filesystem/COM I/O.

            // WNet resolves the current logon's drive mapping, even when a CAD
            // file is locked. An unavailable mapping is not evidence of equality.
            left = Normalize(universalName(left)) ?? left;
            right = Normalize(universalName(right)) ?? right;
            if (Equal(left, right)) return true;

            // Resolve remaining aliases (for example, 8.3 names or junctions) by
            // asking Windows about an existing file handle. Failure leaves the
            // full original path in place; it never relaxes the identity check.
            left = Normalize(finalName(left)) ?? left;
            right = Normalize(finalName(right)) ?? right;
            return Equal(left, right);
        }

        static bool Equal(string left, string right)
        {
            return String.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        static string Normalize(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return null;
            path = path.Replace('/', '\\');
            // GetFinalPathNameByHandle returns extended-length path prefixes.
            if (path.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
                path = @"\\" + path.Substring(8);
            else if (path.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
                path = path.Substring(4);
            // Reject relative/drive-relative names and device paths. FullName
            // must identify a file independently of this executable's directory.
            bool drive = path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':' && path[2] == '\\';
            bool unc = path.StartsWith(@"\\") && !path.StartsWith(@"\\.\");
            if ((!drive && !unc) || path.IndexOfAny(new[] { '\0', '\r', '\n', '*', '?' }) >= 0) return null;
            try { return Path.GetFullPath(path); }
            catch (ArgumentException) { return null; }
            catch (NotSupportedException) { return null; }
            catch (IOException) { return null; }
            catch (System.Security.SecurityException) { return null; }
        }

        static string UniversalName(string path)
        {
            if (path.Length < 3 || path[1] != ':') return null;
            int bytes = 1024;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                IntPtr buffer = Marshal.AllocHGlobal(bytes);
                try
                {
                    // UNIVERSAL_NAME_INFO_LEVEL (1) returns one Unicode string
                    // pointer inside this buffer. Copy it before freeing memory.
                    int result = WNetGetUniversalName(path, 1, buffer, ref bytes);
                    if (result == 0) return Marshal.PtrToStringUni(Marshal.ReadIntPtr(buffer));
                    if (result != 234 || bytes <= 0 || bytes > 131072) return null; // ERROR_MORE_DATA
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            return null;
        }

        static string FinalName(string path)
        {
            // Metadata-only access (0), full sharing (7), OPEN_EXISTING (3).
            // Do not read/write CAD contents or compete with Solid Edge's lock.
            // SafeFileHandle closes on every exit, including exceptions.
            using (SafeFileHandle handle = CreateFile(path, 0, 7, IntPtr.Zero, 3, 0, IntPtr.Zero))
            {
                if (handle.IsInvalid) return null;
                int capacity = 512;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    var buffer = new StringBuilder(capacity);
                    uint length = GetFinalPathNameByHandle(handle, buffer, (uint)capacity, 0);
                    if (length == 0) return null;
                    if (length < capacity) return buffer.ToString();
                    if (length >= 32768) return null;
                    capacity = (int)length + 1;
                }
            }
            return null;
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode, EntryPoint = "WNetGetUniversalNameW", ExactSpelling = true)]
        static extern int WNetGetUniversalName(string path, int level, IntPtr buffer, ref int size);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateFileW", ExactSpelling = true, SetLastError = true)]
        static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security,
            uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetFinalPathNameByHandleW", ExactSpelling = true, SetLastError = true)]
        static extern uint GetFinalPathNameByHandle(SafeFileHandle handle, StringBuilder path, uint size, uint flags);
    }
}
