using System;
using System.Runtime.InteropServices;

namespace SolidEdgeConvert
{
    // Windows Shell's published IUnknown ABI. Unused slots remain in their
    // native order; do not remove them or calls will enter the wrong vtable slot.
    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItem
    {
        void BindToHandler(IntPtr context, ref Guid handler, ref Guid iid, out IntPtr result);
        void GetParent(out IShellItem parent);
        void GetDisplayName(uint kind, out IntPtr name);
        void GetAttributes(uint mask, out uint attributes);
        void Compare(IShellItem other, uint hint, out int order);
    }

    [ComImport, Guid("B63EA76D-1F85-456F-A19C-48159EFA858B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItemArray
    {
        void BindToHandler(IntPtr context, ref Guid handler, ref Guid iid, out IntPtr result);
        void GetPropertyStore(int flags, ref Guid iid, out IntPtr result);
        void GetPropertyDescriptionList(IntPtr key, ref Guid iid, out IntPtr result);
        void GetAttributes(uint flags, uint mask, out uint attributes);
        void GetCount(out uint count);
        void GetItemAt(uint index, out IShellItem item);
        void EnumItems(out IntPtr enumerator);
    }

    [ComVisible(true), Guid("7F9185B0-CB92-43C5-80A9-92277A4F7B54"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExecuteCommand
    {
        void SetKeyState(uint keys);
        void SetParameters([MarshalAs(UnmanagedType.LPWStr)] string parameters);
        void SetPosition(ShellPoint point);
        void SetShowWindow(int show);
        void SetNoShowUI([MarshalAs(UnmanagedType.Bool)] bool noUI);
        void SetDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
        void Execute();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShellPoint { public int X, Y; }

    [ComVisible(true), Guid("1C9CD5BB-98E9-4491-A60F-31AACC72B83C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IObjectWithSelection
    {
        void SetSelection(IShellItemArray selection);
        void GetSelection(ref Guid iid, out IntPtr selection);
    }

    internal static class ShellSelection
    {
        internal static string[] Read(IShellItemArray selection)
        {
            if (selection == null) throw new ArgumentNullException("selection");
            uint count;
            selection.GetCount(out count);
            string[] paths = new string[count];
            for (uint i = 0; i < count; i++)
            {
                IShellItem item = null;
                IntPtr name = IntPtr.Zero;
                try
                {
                    selection.GetItemAt(i, out item);
                    item.GetDisplayName(0x80058000, out name); // SIGDN_FILESYSPATH
                    paths[i] = Marshal.PtrToStringUni(name);
                }
                finally
                {
                    if (name != IntPtr.Zero) Marshal.FreeCoTaskMem(name);
                    if (item != null) Marshal.ReleaseComObject(item);
                }
            }
            return BatchConversion.UniquePaths(paths);
        }

        // Recreate on demand for GetSelection rather than retaining Explorer's
        // COM objects throughout a potentially long CAD conversion.
        internal static IShellItemArray Create(string[] paths)
        {
            IntPtr[] ids = new IntPtr[paths.Length];
            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    uint attributes;
                    Marshal.ThrowExceptionForHR(SHParseDisplayName(paths[i], IntPtr.Zero, out ids[i], 0, out attributes));
                }
                IShellItemArray array;
                Marshal.ThrowExceptionForHR(SHCreateShellItemArrayFromIDLists((uint)ids.Length, ids, out array));
                return array;
            }
            finally { foreach (IntPtr id in ids) if (id != IntPtr.Zero) Marshal.FreeCoTaskMem(id); }
        }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHParseDisplayName(string name, IntPtr context, out IntPtr id, uint mask, out uint attributes);
        [DllImport("shell32.dll")]
        private static extern int SHCreateShellItemArrayFromIDLists(uint count, [In] IntPtr[] ids, out IShellItemArray array);
    }
}
