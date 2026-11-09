using System;
using System.Runtime.InteropServices;

namespace HoleTableFormatter
{
    internal static class Com
    {
        // Release each reference acquired by this macro once. FinalRelease would
        // invalidate aliases held by Solid Edge or another add-in in this process.
        internal static void Release(object value)
        { if (value != null && Marshal.IsComObject(value)) Marshal.ReleaseComObject(value); }
    }

    // Follow the SDK Automation sample: all COM work stays on the STA thread,
    // with a bounded retry when Solid Edge is temporarily processing a command.
    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig] int HandleInComingCall(int kind, IntPtr caller, int tick, IntPtr info);
        [PreserveSig] int RetryRejectedCall(IntPtr callee, int tick, int reject);
        [PreserveSig] int MessagePending(IntPtr callee, int tick, int pending);
    }
    internal sealed class BusyFilter : IOleMessageFilter, IDisposable
    {
        [DllImport("ole32.dll")] private static extern int CoRegisterMessageFilter(IOleMessageFilter filter, out IOleMessageFilter previous);
        private IOleMessageFilter previous;
        internal BusyFilter() { Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this, out previous)); }
        public int HandleInComingCall(int kind, IntPtr caller, int tick, IntPtr info) { return 0; }
        public int RetryRejectedCall(IntPtr callee, int tick, int reject) { return reject == 2 && tick < 10000 ? 250 : -1; }
        public int MessagePending(IntPtr callee, int tick, int pending) { return 2; }
        public void Dispose() { IOleMessageFilter ignored; CoRegisterMessageFilter(previous, out ignored); previous = null; }
    }
}
