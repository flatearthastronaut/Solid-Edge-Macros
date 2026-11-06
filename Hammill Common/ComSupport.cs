using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Hammill.Automation
{
    // Each getter/Item acquisition belongs to one scope. Indexed traversal avoids
    // the hidden COM enumerators created by foreach over Solid Edge collections.
    internal sealed class ComScope : IDisposable
    {
        private readonly List<object> references = new List<object>();
        public dynamic Own(object value) { references.Add(value); return value; }
        public static void Release(object value)
        {
            if (value == null || !Marshal.IsComObject(value)) return;
            try { Marshal.ReleaseComObject(value); }
            catch (InvalidComObjectException) { }
            catch (COMException) { }
        }
        public void Dispose()
        { for (int i = references.Count - 1; i >= 0; i--) Release(references[i]); references.Clear(); }
    }

    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig] int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info);
        [PreserveSig] int RetryRejectedCall(IntPtr callee, int ticks, int rejectType);
        [PreserveSig] int MessagePending(IntPtr callee, int ticks, int pendingType);
    }
    // Adapted from the bundled SDK Variables/OleMessageFilter pattern. All CAD
    // automation stays on the STA UI thread; retries are bounded to ten seconds.
    internal sealed class OleMessageFilter : IOleMessageFilter, IDisposable
    {
        private IOleMessageFilter previous;
        public OleMessageFilter() { Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this, out previous)); }
        public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) { return 0; }
        public int RetryRejectedCall(IntPtr callee, int ticks, int rejectType) { return rejectType == 2 && ticks < 10000 ? 250 : -1; }
        public int MessagePending(IntPtr callee, int ticks, int pendingType) { return 2; }
        public void Dispose()
        { IOleMessageFilter removed; Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(previous, out removed)); previous = null; GC.KeepAlive(this); }
        [DllImport("ole32.dll")] private static extern int CoRegisterMessageFilter(IOleMessageFilter filter, out IOleMessageFilter previous);
    }
}
